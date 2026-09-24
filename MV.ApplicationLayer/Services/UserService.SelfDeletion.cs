using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MV.DomainLayer.Constants;
using MV.DomainLayer.DTO.RequestModel;
using MV.DomainLayer.DTO.ResponseModel;
using MV.DomainLayer.Entities;
using MV.DomainLayer.Exceptions;
using MV.DomainLayer.Helpers;

namespace MV.ApplicationLayer.Services
{
    /// <summary>
    /// Người dùng tự xoá tài khoản (App Store 5.1.1(v) / Google Play) — POST /api/users/me/delete-account.
    /// </summary>
    /// <remarks>
    /// Xoá MỀM ngay lập tức, giống cách web/admin khoá tài khoản: status = 0 (mọi cổng đăng nhập,
    /// refresh token và OnTokenValidated đã chặn), is_deleted = true, deleted_at = now,
    /// deletion_source = 'self'. Dữ liệu cá nhân + file ghi âm được <c>AccountDeletionPurgeJob</c>
    /// dọn sau <see cref="AccountDeletion.PurgeAfterDays"/> ngày (khoảng đệm để xử lý khiếu nại /
    /// yêu cầu khôi phục qua hỗ trợ). Không xoá cứng hàng users: gần 20 FK NO ACTION trỏ vào users
    /// (booking, ví, giao dịch...) — xem UserService.Purge.
    /// </remarks>
    public partial class UserService
    {
        public async Task<DeleteAccountResponse> DeleteOwnAccountAsync(string userId, DeleteAccountRequest request)
        {
            var user = await _userRepository.GetUserByIdAsync(userId)
                ?? throw new UserNotFoundException();

            if (user.Isdeleted == true)
                throw new InvalidOperationException(AccountDeletion.DeletedMessage);

            if (UserRole.IsInternal(user.Primaryrole))
                throw new AccountDeletionForbiddenException(
                    "Tài khoản quản trị/nhân viên không thể tự xoá. Vui lòng liên hệ quản trị viên.");

            VerifyDeletionPassword(user, request?.Password);

            // Cùng bộ kiểm tra với xoá vĩnh viễn của admin (bỏ qua điều kiện "đã bị khoá"): ví còn
            // tiền, khoá học chưa tất toán, rút tiền đang chờ, khiếu nại chưa đóng → chưa cho xoá,
            // nếu không tiền/buổi học của người khác bị bỏ rơi cùng tài khoản.
            var blockers = await CollectPurgeBlockersAsync(user.Userid, 0);
            if (blockers.Count > 0)
                throw new AccountDeletionBlockedException(blockers);

            var now = TimeZoneHelper.UtcNow;
            var reason = string.IsNullOrWhiteSpace(request?.Reason) ? null : request!.Reason!.Trim();
            if (reason != null && reason.Length > AccountDeletion.MaxReasonLength)
                reason = reason[..AccountDeletion.MaxReasonLength];

            var deliveriesStopped = 0;
            var studentsArchived = 0;

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // Giá trị giống admin khoá tài khoản (status 0 + is_deactivated) để mọi kiểm tra sẵn có
                // đều chặn; is_deleted phân biệt "đã xoá" với "bị khoá".
                user.Status = 0;
                user.Isdeactivated = true;
                user.Deactivatedat = now;
                user.Isdeleted = true;
                user.Deletedat = now;
                user.Deletionsource = AccountDeletion.SourceSelf;
                user.Deletionreason = reason;
                // Gỡ push token + token Google Calendar ngay: không gửi thông báo / đồng bộ lịch nữa.
                user.Fcmtoken = null;
                user.Googlecalendartoken = null;
                await _userRepository.UpdateUserAsync(user);

                // Đăng xuất mọi thiết bị.
                await _refreshTokenRepository.RevokeAllByUserIdAsync(userId);

                // Gia sư: gỡ khỏi tìm kiếm, ngừng nhận đặt lịch.
                var tutorProfile = await _context.Tutorprofiles
                    .FirstOrDefaultAsync(t => t.Tutorid == userId);
                if (tutorProfile != null)
                {
                    tutorProfile.Ispublic = false;
                    tutorProfile.Isacceptingbookings = false;
                }

                // Recorder: báo cáo chưa gửi phụ huynh → dừng hẳn; buổi đang chờ chép lời → bỏ hàng đợi.
                var lessons = await _context.RecorderLessons
                    .Where(l => l.Tutorid == userId
                        && (l.Deliverystatus == RecorderDeliveryStatus.Pending
                            || l.Transcriptstatus == RecorderTranscriptStatus.Queued))
                    .ToListAsync();
                foreach (var lesson in lessons)
                {
                    if (lesson.Deliverystatus == RecorderDeliveryStatus.Pending)
                    {
                        lesson.Deliverystatus = RecorderDeliveryStatus.Failed;
                        lesson.Deliveryerror = AccountDeletion.TutorDeletedDeliveryError;
                        deliveriesStopped++;
                    }
                    if (lesson.Transcriptstatus == RecorderTranscriptStatus.Queued)
                    {
                        lesson.Transcriptstatus = RecorderTranscriptStatus.Failed;
                        lesson.Transcripterror = AccountDeletion.TutorDeletedDeliveryError;
                        lesson.Transcriptbatch = null;
                    }
                    lesson.Updatedat = now;
                }

                // Ẩn học sinh recorder khỏi danh bạ + thu hồi link mời phụ huynh còn hiệu lực.
                var students = await _context.RecorderStudents
                    .Where(s => s.Tutorid == userId && s.Archivedat == null)
                    .ToListAsync();
                foreach (var student in students)
                {
                    student.Archivedat = now;
                    student.Updatedat = now;
                }
                studentsArchived = students.Count;

                var invites = await _context.RecorderParentInvites
                    .Where(i => i.Tutorid == userId && i.Usedat == null && i.Revokedat == null)
                    .ToListAsync();
                foreach (var invite in invites)
                    invite.Revokedat = now;

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }

            _logger.LogWarning(
                "User {UserId} ({Role}) self-deleted their account. Stopped {Deliveries} pending recorder deliveries, archived {Students} recorder students. Purge after {Days} days.",
                userId, user.Primaryrole, deliveriesStopped, studentsArchived, AccountDeletion.PurgeAfterDays);

            return new DeleteAccountResponse
            {
                UserId = userId,
                DeletedAt = now,
                PurgeScheduledAt = now.AddDays(AccountDeletion.PurgeAfterDays),
                Message = $"Tài khoản đã được xoá. Dữ liệu cá nhân và bản ghi âm sẽ bị xoá vĩnh viễn sau {AccountDeletion.PurgeAfterDays} ngày."
            };
        }

        /// <summary>
        /// Tài khoản có mật khẩu do người dùng đặt → bắt buộc nhập đúng. Tài khoản tạo qua Zalo
        /// (username "social_…" / email "@tutora.invalid") chỉ có mật khẩu ngẫu nhiên người dùng
        /// không biết → cho xoá không cần mật khẩu (đã xác thực bằng JWT).
        /// </summary>
        private void VerifyDeletionPassword(User user, string? password)
        {
            var hasNoUserChosenPassword =
                (user.Username?.StartsWith("social_", StringComparison.Ordinal) ?? false)
                || (user.Email?.EndsWith("@tutora.invalid", StringComparison.OrdinalIgnoreCase) ?? false);
            if (hasNoUserChosenPassword)
                return;

            if (string.IsNullOrEmpty(password))
                throw new AccountDeletionPasswordException("Vui lòng nhập mật khẩu để xác nhận xoá tài khoản.");

            bool matches;
            try
            {
                matches = !string.IsNullOrEmpty(user.Password)
                    && _passwordRepository.VerifyPassword(password, user.Password);
            }
            catch (FormatException)
            {
                matches = false;
            }

            if (!matches)
                throw new AccountDeletionPasswordException("Mật khẩu không đúng.");
        }
    }
}
