using System.Text.Json;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MV.ApplicationLayer.Interfaces;
using MV.ApplicationLayer.ServiceInterfaces;
using MV.DomainLayer.Constants;
using MV.DomainLayer.DTO.RequestModel;
using MV.DomainLayer.DTO.ResponseModel;
using MV.DomainLayer.Entities;
using MV.DomainLayer.Exceptions;
using MV.DomainLayer.Helpers;

namespace MV.ApplicationLayer.Services;

/// <summary>
/// Ghi âm buổi dạy từ app gia sư. Mọi trạng thái nằm trong recorder.lessons —
/// một dòng cho mỗi buổi, dù là buổi có booking (Classsessionid) hay buổi của
/// học sinh ngoài nền tảng (Studentid). App gọi LessonId là "recordingId".
/// </summary>
public class AppRecordingService(
    IAppDbContext db,
    IAppRecordingStorage storage,
    IClassSessionService classSessionService,
    IBackgroundJobClient backgroundJobClient,
    ILogger<AppRecordingService> logger) : IAppRecordingService
{
    /// <summary>
    /// Đủ dài để gia sư upload xong một đoạn 5 phút trên 4G yếu, đủ ngắn để một
    /// URL rò ra ngoài không dùng được lâu.
    /// </summary>
    private static readonly TimeSpan UploadUrlLifetime = TimeSpan.FromMinutes(30);

    // ── Bắt đầu ghi ──────────────────────────────────────────────────────────

    public async Task<AppRecordingStartResponse> StartForClassSessionAsync(
        int classSessionId, string tutorUserId, string? consentSnapshotJson, CancellationToken ct = default)
    {
        EnsureStorage();

        var session = await db.ClassSessions
            .Include(s => s.Student)
            .FirstOrDefaultAsync(s => s.Classsessionid == classSessionId, ct)
            ?? throw new RecorderNotFoundException("Không tìm thấy buổi học.");

        if (session.Tutorid != tutorUserId)
            throw new UnauthorizedAccessException("Bạn không dạy buổi học này.");

        // Một buổi booking = một dòng nhật ký (unique index). App bị giết giữa
        // buổi rồi mở lại phải nối tiếp đúng bản ghi cũ.
        var lesson = await db.RecorderLessons
            .FirstOrDefaultAsync(l => l.Classsessionid == classSessionId, ct);

        if (lesson == null)
        {
            lesson = NewLesson(tutorUserId);
            lesson.Classsessionid = classSessionId;
            lesson.Scheduledstart = session.Scheduledstart;
            lesson.Scheduledend = session.Scheduledend;
            db.RecorderLessons.Add(lesson);
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                // Hai máy cùng bấm ghi: unique index là trọng tài, kẻ thua dùng lại dòng của kẻ thắng.
                db.RecorderLessons.Remove(lesson);
                lesson = await db.RecorderLessons.FirstAsync(l => l.Classsessionid == classSessionId, ct);
            }
        }

        return await OpenRecordingAsync(lesson, session.Student?.Fullname ?? "Học sinh", consentSnapshotJson, ct);
    }

    public async Task<AppRecordingStartResponse> StartForLessonAsync(
        Guid lessonId, string tutorUserId, string? consentSnapshotJson, CancellationToken ct = default)
    {
        EnsureStorage();
        var lesson = await LoadOwnedAsync(lessonId, tutorUserId, ct);
        return await OpenRecordingAsync(lesson, await StudentNameAsync(lesson, ct), consentSnapshotJson, ct);
    }

    public async Task<AppRecordingStartResponse> StartForStudentAsync(
        Guid studentId, string tutorUserId, string? consentSnapshotJson, CancellationToken ct = default)
    {
        EnsureStorage();
        var student = await db.RecorderStudents
            .FirstOrDefaultAsync(s => s.Studentid == studentId && s.Tutorid == tutorUserId && s.Archivedat == null, ct)
            ?? throw new RecorderNotFoundException("Không tìm thấy học sinh.");

        if (student.Consentstatus == RecorderConsentStatus.Declined)
            throw new RecorderNotReadyException("Phụ huynh đã từ chối ghi âm cho học sinh này.");

        // Bấm ghi lần nữa sau khi app bị giết: nối tiếp buổi đang ghi dở trong
        // hôm nay thay vì mở buổi mới.
        var since = TimeZoneHelper.UtcNow.AddHours(-6);
        var lesson = await db.RecorderLessons
            .Where(l => l.Studentid == studentId
                && (l.Status == SessionRecordingStatus.Recording || l.Status == SessionRecordingStatus.Uploading)
                && l.Startedat >= since)
            .OrderByDescending(l => l.Startedat)
            .FirstOrDefaultAsync(ct);

        // Có buổi theo thời khoá biểu quanh giờ này (±3 giờ) thì ghi vào đúng buổi đó,
        // để lịch và báo cáo khớp nhau thay vì mọc thêm một buổi lẻ.
        if (lesson == null)
        {
            var now = TimeZoneHelper.UtcNow;
            var windowStart = now.AddHours(-3);
            var windowEnd = now.AddHours(3);
            lesson = (await db.RecorderLessons
                    .Where(l => l.Studentid == studentId
                        && l.Status == SessionRecordingStatus.Scheduled
                        && l.Scheduledstart >= windowStart && l.Scheduledstart <= windowEnd)
                    .ToListAsync(ct))
                .OrderBy(l => Math.Abs((l.Scheduledstart!.Value - now).Ticks))
                .FirstOrDefault();
        }

        if (lesson == null)
        {
            lesson = NewLesson(tutorUserId);
            lesson.Studentid = studentId;
            lesson.Subject = student.Subject;
            lesson.Scheduledstart = TimeZoneHelper.UtcNow;
            db.RecorderLessons.Add(lesson);
            await db.SaveChangesAsync(ct);
        }

        return await OpenRecordingAsync(lesson, student.Fullname, consentSnapshotJson, ct);
    }

    /// <summary>Đưa một dòng nhật ký về trạng thái đang ghi, hoặc trả lại bản ghi còn dở.</summary>
    private async Task<AppRecordingStartResponse> OpenRecordingAsync(
        RecorderLesson lesson, string studentName, string? consentSnapshotJson, CancellationToken ct)
    {
        switch (lesson.Status)
        {
            case SessionRecordingStatus.Recording:
            case SessionRecordingStatus.Uploading:
            {
                var parts = await storage.ListPartsAsync(lesson.Storagekey!, ct);
                return new AppRecordingStartResponse
                {
                    RecordingId = lesson.Lessonid,
                    StudentName = studentName,
                    UploadedParts = parts.Count
                };
            }
            case SessionRecordingStatus.Scheduled:
            case SessionRecordingStatus.Discarded:
            case SessionRecordingStatus.Failed:
                break;
            default:
                // processing / awaiting_approval / sent: buổi này đã có bản ghi.
                throw new AppRecordingClosedException();
        }

        // Ghi mới (hoặc ghi lại sau khi huỷ): prefix mới để không lẫn đoạn cũ.
        var now = TimeZoneHelper.UtcNow;
        lesson.Status = SessionRecordingStatus.Recording;
        lesson.Startedat = now;
        lesson.Endedat = null;
        lesson.Durationsec = 0;
        lesson.Bytes = 0;
        lesson.Partcount = 0;
        lesson.Storagekey = storage.BuildPrefix(lesson.Tutorid, Guid.NewGuid());
        lesson.Consentsnapshot = consentSnapshotJson;
        lesson.Aistatus = RecorderAiStatus.None;
        lesson.Airesult = null;
        lesson.Aierror = null;
        lesson.Geminifilename = null;
        lesson.Geminifileuri = null;
        lesson.Geminifileexpiresat = null;
        lesson.Transcript = null;
        lesson.Transcriptstatus = RecorderTranscriptStatus.None;
        lesson.Transcripterror = null;
        // Ghi lại từ đầu: transcript cũ (nếu có) không còn khớp bản ghi mới.
        lesson.Transcriptkey = null;
        lesson.Transcriptmodel = null;
        lesson.Transcriptbatch = null;
        lesson.Transcriptqueuedat = null;
        lesson.Audiokey = null;
        lesson.Audiodeletedat = null;
        lesson.Errormessage = null;
        lesson.Updatedat = now;
        await db.SaveChangesAsync(ct);

        return new AppRecordingStartResponse
        {
            RecordingId = lesson.Lessonid,
            StudentName = studentName,
            UploadedParts = 0
        };
    }

    // ── Upload + chốt ────────────────────────────────────────────────────────

    public async Task<AppRecordingUploadUrlResponse> CreateUploadUrlAsync(
        Guid recordingId, string tutorUserId, int partNumber, CancellationToken ct = default)
    {
        if (partNumber < 0)
            throw new AppRecordingInvalidPartException();

        var lesson = await LoadOwnedAsync(recordingId, tutorUserId, ct);
        if (lesson.Status is not (SessionRecordingStatus.Recording or SessionRecordingStatus.Uploading))
            throw new AppRecordingClosedException();

        if (lesson.Status == SessionRecordingStatus.Recording)
        {
            lesson.Status = SessionRecordingStatus.Uploading;
            lesson.Updatedat = TimeZoneHelper.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        return new AppRecordingUploadUrlResponse
        {
            PartNumber = partNumber,
            Url = storage.CreateUploadUrl(lesson.Storagekey!, partNumber, UploadUrlLifetime),
            ExpiresAt = DateTime.UtcNow.Add(UploadUrlLifetime)
        };
    }

    public async Task<AppRecordingStatusResponse> CompleteAsync(
        Guid recordingId, string tutorUserId, int durationSec, CancellationToken ct = default)
    {
        var lesson = await LoadOwnedAsync(recordingId, tutorUserId, ct);

        // Gọi complete lần hai (app retry) không được tạo job thứ hai — tốn tiền
        // model hai lần cho cùng một kết quả.
        if (lesson.Status is SessionRecordingStatus.Processing
            or SessionRecordingStatus.AwaitingApproval
            or SessionRecordingStatus.Sent)
        {
            return await BuildStatusAsync(lesson, ct);
        }

        if (lesson.Status is not (SessionRecordingStatus.Recording or SessionRecordingStatus.Uploading
            or SessionRecordingStatus.Failed))
            throw new AppRecordingClosedException();

        var parts = await storage.ListPartsAsync(lesson.Storagekey!, ct);
        if (parts.Count == 0)
            throw new AppRecordingEmptyException();

        var now = TimeZoneHelper.UtcNow;
        lesson.Partcount = parts.Count;
        lesson.Bytes = parts.Sum(p => p.Bytes);
        lesson.Durationsec = durationSec;
        lesson.Endedat = now;
        lesson.Status = SessionRecordingStatus.Processing;
        lesson.Aistatus = RecorderAiStatus.Pending;
        lesson.Aierror = null;
        lesson.Updatedat = now;
        await db.SaveChangesAsync(ct);

        backgroundJobClient.Enqueue<IRecorderAiService>(s => s.RunLessonReportJobAsync(lesson.Lessonid));

        logger.LogInformation(
            "Buổi {LessonId} chốt với {PartCount} đoạn ({Bytes} byte), đã xếp hàng job AI.",
            lesson.Lessonid, parts.Count, lesson.Bytes);

        return await BuildStatusAsync(lesson, ct);
    }

    public async Task<AppRecordingStatusResponse> GetAsync(
        Guid recordingId, string tutorUserId, CancellationToken ct = default)
    {
        var lesson = await LoadOwnedAsync(recordingId, tutorUserId, ct);
        return await BuildStatusAsync(lesson, ct);
    }

    public async Task DiscardAsync(Guid recordingId, string tutorUserId, CancellationToken ct = default)
    {
        var lesson = await LoadOwnedAsync(recordingId, tutorUserId, ct);
        if (lesson.Status == SessionRecordingStatus.Sent)
            throw new AppRecordingAlreadySentException();

        lesson.Status = SessionRecordingStatus.Discarded;
        lesson.Endedat ??= TimeZoneHelper.UtcNow;
        lesson.Updatedat = TimeZoneHelper.UtcNow;
        await db.SaveChangesAsync(ct);
        // File trên kho để lifecycle rule của bucket tự dọn: xoá ngay ở đây thì
        // một lần bấm nhầm là mất sạch.
    }

    public async Task<AppRecordingAudioUrlResponse> GetAudioUrlAsync(
        Guid recordingId, string tutorUserId, CancellationToken ct = default)
    {
        var lesson = await LoadOwnedAsync(recordingId, tutorUserId, ct);
        if (lesson.Audiodeletedat != null)
            throw new RecorderNotReadyException(
                $"File âm thanh đã được xoá sau {RecorderRetention.AudioDays} ngày lưu trữ. Báo cáo và lời thoại vẫn còn.");
        if (string.IsNullOrWhiteSpace(lesson.Audiokey))
            throw new RecorderNotReadyException("File nghe lại chưa sẵn sàng, thử lại sau ít phút.");

        var lifetime = TimeSpan.FromHours(2);
        return new AppRecordingAudioUrlResponse
        {
            Url = storage.CreateDownloadUrl(lesson.Audiokey!, lifetime),
            ExpiresAt = DateTime.UtcNow.Add(lifetime)
        };
    }

    // ── Duyệt + gửi ──────────────────────────────────────────────────────────

    public async Task<AppRecordingStatusResponse> ApproveAsync(
        Guid recordingId, string tutorUserId, RecorderApproveRequest request, CancellationToken ct = default)
    {
        var lesson = await LoadOwnedAsync(recordingId, tutorUserId, ct);

        if (lesson.Status == SessionRecordingStatus.Sent)
            throw new AppRecordingAlreadySentException();
        if (lesson.Status != SessionRecordingStatus.AwaitingApproval)
            throw new RecorderNotReadyException("Báo cáo chưa sẵn sàng để duyệt.");

        var now = TimeZoneHelper.UtcNow;
        lesson.Reportcontent = request.LessonContent.Trim();
        lesson.Reporthomework = string.IsNullOrWhiteSpace(request.Homework) ? null : request.Homework.Trim();
        lesson.Reportnotes = string.IsNullOrWhiteSpace(request.TutorNotes) ? null : request.TutorNotes.Trim();

        // Tóm tắt ngắn cho tin Zalo: gia sư không gửi (app cũ / không sửa) thì lấy bản nháp AI.
        var zaloDraft = ParseDraft(lesson.Airesult)?.ZaloSummary;
        lesson.Zalocontent = ZaloValue(request.ZaloContent, zaloDraft?.Content);
        lesson.Zalohomework = ZaloValue(request.ZaloHomework, zaloDraft?.Homework);
        lesson.Zalonotes = ZaloValue(request.ZaloNotes, zaloDraft?.Notes);

        lesson.Approvedat = now;
        lesson.Updatedat = now;

        if (lesson.Classsessionid is int classSessionId)
        {
            // Buổi có booking: đi đúng luồng nộp báo cáo của nền tảng (thông báo
            // phụ huynh, giải ngân... đều do luồng đó lo).
            lesson.Deliverychannel = RecorderDeliveryChannel.Booking;
            await classSessionService.SubmitReportAsync(classSessionId, tutorUserId, new SubmitReportRequest
            {
                ContentCovered = lesson.Reportcontent,
                HomeworkAssigned = lesson.Reporthomework,
                TutorNotes = lesson.Reportnotes
            });
            lesson.Deliverystatus = RecorderDeliveryStatus.Sent;
            lesson.Sentat = now;
        }
        else
        {
            // Học sinh ngoài nền tảng: Tutora gửi ZNS tới SĐT phụ huynh. Template
            // ZNS chưa được Zalo duyệt → giữ ở pending; job gửi sẽ quét các dòng này.
            lesson.Deliverychannel = RecorderDeliveryChannel.Zns;
            lesson.Deliverystatus = RecorderDeliveryStatus.Pending;
        }

        lesson.Status = SessionRecordingStatus.Sent;
        await db.SaveChangesAsync(ct);
        return await BuildStatusAsync(lesson, ct);
    }

    // ── nội bộ ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Giá trị gia sư gửi lên thắng bản nháp AI — kể cả chuỗi rỗng (gia sư chủ ý xoá, tin Zalo sẽ ghi
    /// "Không có"). Chỉ khi app không gửi trường này (null, app cũ) mới lấy bản nháp. Cột DB là
    /// varchar(200) nên cắt cứng cho chắc.
    /// </summary>
    private static string? ZaloValue(string? edited, string? draft)
    {
        var value = edited is not null ? edited.Trim()
            : !string.IsNullOrWhiteSpace(draft) ? draft.Trim()
            : null;
        return value is { Length: > 200 } ? value[..200] : value;
    }

    private void EnsureStorage()
    {
        if (!storage.Enabled)
            throw new InvalidOperationException("Kho lưu trữ bản ghi chưa được cấu hình.");
    }

    private static RecorderLesson NewLesson(string tutorUserId)
    {
        var now = TimeZoneHelper.UtcNow;
        return new RecorderLesson
        {
            Lessonid = Guid.NewGuid(),
            Tutorid = tutorUserId,
            Status = SessionRecordingStatus.Scheduled,
            Aistatus = RecorderAiStatus.None,
            Createdat = now,
            Updatedat = now
        };
    }

    private async Task<RecorderLesson> LoadOwnedAsync(Guid lessonId, string tutorUserId, CancellationToken ct)
    {
        var lesson = await db.RecorderLessons.FirstOrDefaultAsync(l => l.Lessonid == lessonId, ct)
            ?? throw new RecorderNotFoundException("Không tìm thấy bản ghi.");

        if (lesson.Tutorid != tutorUserId)
            throw new UnauthorizedAccessException("Bản ghi này không phải của bạn.");

        return lesson;
    }

    private async Task<string> StudentNameAsync(RecorderLesson lesson, CancellationToken ct)
    {
        if (lesson.Studentid is Guid sid)
        {
            return await db.RecorderStudents.Where(s => s.Studentid == sid)
                .Select(s => s.Fullname).FirstOrDefaultAsync(ct) ?? "Học sinh";
        }
        if (lesson.Classsessionid is int cs)
        {
            return await db.ClassSessions.Where(s => s.Classsessionid == cs)
                .Select(s => s.Student != null ? s.Student.Fullname : null).FirstOrDefaultAsync(ct) ?? "Học sinh";
        }
        return "Học sinh";
    }

    private async Task<AppRecordingStatusResponse> BuildStatusAsync(RecorderLesson l, CancellationToken ct)
    {
        // Báo cáo đã duyệt thắng bản nháp AI.
        var draft = ParseDraft(l.Airesult);
        var student = l.Studentid is Guid sid
            ? await db.RecorderStudents.Where(s => s.Studentid == sid)
                .Select(s => new { s.Grade, s.Parentphone }).FirstOrDefaultAsync(ct)
            : null;
        // Biên bản nằm trong bản nháp AI (Airesult), không bị báo cáo đã duyệt ghi đè — duyệt chỉ đụng
        // tới phần gửi phụ huynh. List có thể null nếu JSON ghi "keyPoints": null nên phải ?? lại.
        var minutes = draft?.SessionMinutes;
        return new AppRecordingStatusResponse
        {
            RecordingId = l.Lessonid,
            ClassSessionId = l.Classsessionid,
            StudentId = l.Studentid,
            StudentName = await StudentNameAsync(l, ct),
            Status = l.Status,
            PartCount = l.Partcount,
            Bytes = l.Bytes,
            DurationSec = l.Durationsec,
            AiStatus = l.Aistatus,
            LessonContent = l.Reportcontent ?? draft?.LessonContent,
            Homework = l.Reporthomework ?? draft?.Homework,
            TutorNotes = l.Reportnotes ?? draft?.TutorNotes,
            ZaloContent = l.Zalocontent ?? draft?.ZaloSummary?.Content,
            ZaloHomework = l.Zalohomework ?? draft?.ZaloSummary?.Homework,
            ZaloNotes = l.Zalonotes ?? draft?.ZaloSummary?.Notes,
            ErrorMessage = l.Aierror ?? l.Errormessage,
            DeliveryChannel = l.Deliverychannel,
            DeliveryStatus = l.Deliverystatus,
            DeliveryError = l.Deliveryerror,
            ParentPhone = student?.Parentphone,
            Grade = student?.Grade,
            Subject = l.Subject,
            ScheduledStart = l.Scheduledstart,
            StartedAt = l.Startedat,
            EndedAt = l.Endedat,
            ApprovedAt = l.Approvedat,
            SentAt = l.Sentat,
            Transcript = l.Transcript,
            TranscriptStatus = l.Transcriptstatus,
            AudioAvailable = l.Audiokey != null && l.Audiodeletedat == null,
            AudioExpiresAt = l.Endedat?.AddDays(RecorderRetention.AudioDays),
            SessionMinutes = minutes == null
                ? null
                : new AppRecordingSessionMinutes
                {
                    Summary = minutes.Summary,
                    KeyPoints = minutes.KeyPoints ?? new List<string>(),
                    FollowUps = minutes.FollowUps ?? new List<string>()
                }
        };
    }

    /// <summary>
    /// Job ghi bằng JsonSerializer mặc định (PascalCase). JSON hỏng thì coi như
    /// chưa có nháp chứ không làm sập endpoint app đang poll.
    /// </summary>
    internal static TutorReportAiFillResult? ParseDraft(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<TutorReportAiFillResult>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
