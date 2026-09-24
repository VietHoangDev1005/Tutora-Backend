using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MV.ApplicationLayer.Interfaces;
using MV.ApplicationLayer.ServiceInterfaces;
using MV.DomainLayer.Constants;
using MV.DomainLayer.Helpers;

namespace MV.ApplicationLayer.BackgroundJobs;

/// <summary>
/// Dọn dữ liệu của tài khoản người dùng TỰ XOÁ (users.deletion_source = 'self') sau
/// <see cref="AccountDeletion.PurgeAfterDays"/> ngày. Chạy mỗi ngày một lần, mỗi tài khoản đúng
/// một lần (đánh dấu users.purged_at).
///
/// XOÁ / ẨN DANH:
///   * File trên kho: toàn bộ đoạn ghi âm + merged.m4a (DeletePrefixAsync theo storage_key) và
///     file transcript JSON (DeleteTranscriptAsync) của mọi buổi recorder gia sư đã ghi.
///   * recorder.lessons của gia sư: xoá nội dung báo cáo (report_*), tóm tắt Zalo (zalo_*),
///     ai_result/ai_error, transcript/transcript_key/transcript_error, consent_snapshot,
///     storage_key/audio_key, tham chiếu file Gemini. Giữ dòng (lịch, trạng thái, thời lượng)
///     để thống kê — không còn nội dung cá nhân.
///   * recorder.students của gia sư: tên → "Học sinh đã xoá", xoá parent_name, parent_phone,
///     note, schedule, subject, linked_student_user_id, gỡ parent_id. Không xoá cứng vì
///     recorder.consent_events CASCADE theo student — xoá dòng là mất bằng chứng đồng ý.
///   * users: full_name → "Người dùng đã xoá"; phone, email, username, zalo_user_id → NULL (SĐT/
///     email/Zalo đăng ký lại được); avatar, địa chỉ, ngày sinh, giới tính, CCCD (số + link ảnh +
///     ekyc_raw_data), google_calendar_token, fcm_token, parent_code → NULL; mật khẩu → giá trị
///     không đăng nhập được.
///   * tutorprofiles: bio, headline, video giới thiệu, học vấn, kinh nghiệm → NULL (đã ẩn từ lúc xoá).
///   * studentprofiles của chính tài khoản (học sinh tự đăng ký / tài khoản con): tên → ẩn danh,
///     ngày sinh, trường, mục tiêu, avatar, SĐT phụ huynh → NULL.
///
/// GIỮ LẠI (bằng chứng pháp lý / sổ sách, người khác cũng là một bên):
///   * recorder.consent_events, user_policy_acceptances.
///   * Ví, giao dịch, booking, buổi học, khiếu nại, đánh giá, tin nhắn chat, tài khoản ngân hàng,
///     lịch sử đăng nhập — gắn với user_id đã ẩn danh.
///
/// Lỗi xoá file ở bất kỳ buổi nào → không đổi DB của tài khoản đó, ngày mai thử lại.
/// </summary>
public class AccountDeletionPurgeJob(IServiceProvider sp, ILogger<AccountDeletionPurgeJob> logger)
    : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromHours(24);
    private const int BatchSize = 20;
    private const string AnonymizedStudentName = "Học sinh đã xoá";

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Đợi app khởi động xong (migration, Hangfire) rồi mới quét.
        try { await Task.Delay(TimeSpan.FromMinutes(5), ct); }
        catch (OperationCanceledException) { return; }

        while (!ct.IsCancellationRequested)
        {
            try { await SweepAsync(ct); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Lỗi khi dọn dữ liệu tài khoản đã tự xoá.");
            }

            try { await Task.Delay(_interval, ct); }
            catch (OperationCanceledException) { return; }
        }
    }

    private async Task SweepAsync(CancellationToken ct)
    {
        List<string> userIds;
        using (var scope = sp.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var cutoff = TimeZoneHelper.UtcNow.AddDays(-AccountDeletion.PurgeAfterDays);
            userIds = await db.Users
                .AsNoTracking()
                .Where(u => u.Isdeleted == true
                    && u.Deletionsource == AccountDeletion.SourceSelf
                    && u.Purgedat == null
                    && u.Deletedat != null && u.Deletedat < cutoff)
                .OrderBy(u => u.Deletedat)
                .Select(u => u.Userid)
                .Take(BatchSize)
                .ToListAsync(ct);
        }

        foreach (var userId in userIds)
        {
            // Mỗi tài khoản một scope/DbContext riêng: lỗi của tài khoản này không kéo theo change
            // tracker bẩn sang tài khoản sau.
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var storage = scope.ServiceProvider.GetRequiredService<IAppRecordingStorage>();
            try
            {
                await PurgeUserAsync(db, storage, userId, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Chưa dọn được dữ liệu tài khoản đã xoá {UserId}, lượt sau thử lại.", userId);
            }
        }
    }

    private async Task PurgeUserAsync(IAppDbContext db, IAppRecordingStorage storage, string userId, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Userid == userId, ct);
        if (user == null || user.Purgedat != null) return;

        var now = TimeZoneHelper.UtcNow;
        var lessons = await db.RecorderLessons.Where(l => l.Tutorid == userId).ToListAsync(ct);

        // ── 1. File trên kho (trước khi đụng DB: lỗi thì để nguyên để thử lại) ──
        var hasFiles = lessons.Any(l => !string.IsNullOrWhiteSpace(l.Storagekey)
                                        || !string.IsNullOrWhiteSpace(l.Transcriptkey));
        if (hasFiles && !storage.Enabled)
        {
            logger.LogWarning("Kho ghi âm chưa cấu hình — chưa dọn được file của tài khoản đã xoá {UserId}.", userId);
            return;
        }

        var filesDeleted = 0;
        foreach (var lesson in lessons)
        {
            if (!string.IsNullOrWhiteSpace(lesson.Storagekey))
                filesDeleted += await storage.DeletePrefixAsync(lesson.Storagekey!, ct);
            if (!string.IsNullOrWhiteSpace(lesson.Transcriptkey))
            {
                await storage.DeleteTranscriptAsync(lesson.Transcriptkey!, ct);
                filesDeleted++;
            }
        }

        // ── 2. recorder.lessons: bỏ nội dung, giữ khung ──
        foreach (var lesson in lessons)
        {
            if (!string.IsNullOrWhiteSpace(lesson.Storagekey) && lesson.Audiodeletedat == null)
                lesson.Audiodeletedat = now;
            lesson.Storagekey = null;
            lesson.Audiokey = null;
            lesson.Consentsnapshot = null;
            lesson.Airesult = null;
            lesson.Aierror = null;
            lesson.Geminifilename = null;
            lesson.Geminifileuri = null;
            lesson.Geminifileexpiresat = null;
            lesson.Reportcontent = null;
            lesson.Reporthomework = null;
            lesson.Reportnotes = null;
            lesson.Zalocontent = null;
            lesson.Zalohomework = null;
            lesson.Zalonotes = null;
            lesson.Transcript = null;
            lesson.Transcriptkey = null;
            lesson.Transcripterror = null;
            lesson.Transcriptbatch = null;
            if (lesson.Transcriptstatus != RecorderTranscriptStatus.None)
                lesson.Transcriptstatus = RecorderTranscriptStatus.Deleted;
            lesson.Errormessage = null;
            if (lesson.Deliverystatus == RecorderDeliveryStatus.Pending)
            {
                lesson.Deliverystatus = RecorderDeliveryStatus.Failed;
                lesson.Deliveryerror = AccountDeletion.TutorDeletedDeliveryError;
            }
            lesson.Updatedat = now;
        }

        // ── 3. recorder.students: ẩn danh (giữ dòng cho consent_events) ──
        var students = await db.RecorderStudents.Where(s => s.Tutorid == userId).ToListAsync(ct);
        foreach (var s in students)
        {
            s.Fullname = AnonymizedStudentName;
            s.Parentname = null;
            s.Parentphone = null;
            s.Note = null;
            s.Schedule = null;
            s.Subject = null;
            s.Linkedstudentuserid = null;
            s.Parentid = null;
            s.Parentlinkedat = null;
            s.Archivedat ??= now;
            s.Updatedat = now;
        }

        // ── 4. Hồ sơ gia sư / học sinh của chính tài khoản ──
        var tutorProfile = await db.Tutorprofiles.FirstOrDefaultAsync(t => t.Tutorid == userId, ct);
        if (tutorProfile != null)
        {
            tutorProfile.Ispublic = false;
            tutorProfile.Isacceptingbookings = false;
            tutorProfile.Bio = null;
            tutorProfile.Headline = null;
            tutorProfile.Videointrourl = null;
            tutorProfile.Education = null;
            tutorProfile.Experience = null;
        }

        var ownStudentProfiles = await db.Studentprofiles
            .IgnoreQueryFilters()
            .Where(p => p.Studentid == userId || p.Linkeduserid == userId)
            .ToListAsync(ct);
        foreach (var p in ownStudentProfiles)
        {
            p.Fullname = AccountDeletion.AnonymizedFullName;
            p.Birthdate = null;
            p.School = null;
            p.Learninggoals = null;
            p.Avatarurl = null;
            p.Parentphone = null;
        }

        // ── 5. users: ẩn danh PII, giải phóng SĐT/email/Zalo để đăng ký lại ──
        user.Fullname = AccountDeletion.AnonymizedFullName;
        user.Phone = null;
        user.Isphoneverified = false;
        user.Email = null!;
        user.Isemailverified = false;
        user.Username = null;
        user.Zalouserid = null;
        user.Avatarurl = null;
        user.Address = null;
        user.Birthdate = null;
        user.Gender = null;
        user.Identitynumber = null;
        user.Idcardfronturl = null;
        user.Idcardbackurl = null;
        user.Ekycrawdata = null;
        user.Googlecalendartoken = null;
        user.Fcmtoken = null;
        user.Parentcode = null;
        user.Parentcodeexpiresat = null;
        // Không phải hash PBKDF2 hợp lệ (thiếu 3 phần) → VerifyPassword luôn false.
        user.Password = "purged";
        user.Purgedat = now;

        await db.SaveChangesAsync(ct);

        logger.LogWarning(
            "Đã dọn tài khoản tự xoá {UserId}: {Files} file trên kho, {Lessons} buổi recorder, {Students} học sinh recorder ẩn danh.",
            userId, filesDeleted, lessons.Count, students.Count);
    }
}
