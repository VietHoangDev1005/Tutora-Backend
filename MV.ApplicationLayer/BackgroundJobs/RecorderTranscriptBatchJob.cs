using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MV.ApplicationLayer.Interfaces;
using MV.ApplicationLayer.ServiceInterfaces;
using MV.ApplicationLayer.Services;
using MV.DomainLayer.Configuration;
using MV.DomainLayer.Constants;
using MV.DomainLayer.DTO.ResponseModel;
using MV.DomainLayer.Entities;
using MV.DomainLayer.Exceptions;
using MV.DomainLayer.Helpers;

namespace MV.ApplicationLayer.BackgroundJobs;

/// <summary>
/// Chép lời nền cho các buổi ghi âm từ app bằng Gemini Batch API (giá 50%) và lưu transcript
/// ẩn danh dạng JSON lên kho. Transcript là nguyên liệu thô của hệ thống — gia sư không xem,
/// không ai chờ, nên chấp nhận chậm vài giờ để lấy giá rẻ.
///
/// Mỗi 10 phút:
///   1. Kiểm tra các batch đang chạy → lấy kết quả, ẩn danh, lưu JSON, xoá batch trên Google.
///   2. Chuyển transcript dạng chữ cũ (cột transcript) sang file JSON trên kho.
///   3. Gom các buổi đang xếp hàng (chờ ≥ TranscriptBatchDelayMinutes) thành một batch mới.
///   4. Xoá file transcript quá hạn lưu (TranscriptRetentionDays).
/// </summary>
public class RecorderTranscriptBatchJob(IServiceProvider sp, ILogger<RecorderTranscriptBatchJob> logger)
    : BackgroundService
{
    private const string AudioMimeType = "audio/mp4";
    private const int MaxPerBatch = 50;
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(10);

    /// <summary>Batch có thể mất tới 24 giờ — file audio trên Gemini (sống ~48h) phải còn ít nhất
    /// chừng này lúc gửi, nếu không yêu cầu trong batch sẽ lỗi vì file đã hết hạn.</summary>
    private static readonly TimeSpan MinGeminiFileLife = TimeSpan.FromHours(30);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try { await Task.Delay(TimeSpan.FromMinutes(2), ct); }
        catch (OperationCanceledException) { return; }

        while (!ct.IsCancellationRequested)
        {
            try { await RunOnceAsync(ct); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Lỗi job chép lời nền (transcript batch).");
            }

            try { await Task.Delay(Interval, ct); }
            catch (OperationCanceledException) { return; }
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IAppRecordingStorage>();
        var gemini = scope.ServiceProvider.GetRequiredService<IGeminiVideoAnalysisService>();
        var recorderAi = scope.ServiceProvider.GetRequiredService<IRecorderAiService>();
        var settings = scope.ServiceProvider.GetRequiredService<IOptions<GoogleGeminiSettings>>().Value;

        if (!storage.Enabled || string.IsNullOrWhiteSpace(settings.ApiKey)) return;

        await PollBatchesAsync(db, storage, gemini, settings, ct);
        await MigrateLegacyTextAsync(db, storage, ct);
        if (settings.GenerateTranscript)
            await SubmitQueuedAsync(db, storage, gemini, recorderAi, settings, ct);
        await DeleteExpiredAsync(db, storage, settings, ct);
    }

    // ── 1. Batch đang chạy ───────────────────────────────────────────────────

    private async Task PollBatchesAsync(
        IAppDbContext db, IAppRecordingStorage storage, IGeminiVideoAnalysisService gemini,
        GoogleGeminiSettings settings, CancellationToken ct)
    {
        var batchNames = await db.RecorderLessons
            .Where(l => l.Transcriptstatus == RecorderTranscriptStatus.Batching && l.Transcriptbatch != null)
            .Select(l => l.Transcriptbatch!)
            .Distinct()
            .ToListAsync(ct);

        foreach (var batchName in batchNames)
        {
            GeminiBatchStatus status;
            try { status = await gemini.GetBatchAsync(batchName, ct); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Không đọc được batch {Batch}, thử lại lượt sau.", batchName);
                continue;
            }
            if (status.State == GeminiBatchState.Running) continue;

            var lessons = await db.RecorderLessons
                .Where(l => l.Transcriptbatch == batchName && l.Transcriptstatus == RecorderTranscriptStatus.Batching)
                .ToListAsync(ct);
            var now = TimeZoneHelper.UtcNow;

            if (status.State != GeminiBatchState.Succeeded)
            {
                logger.LogWarning("Batch {Batch} kết thúc với trạng thái {State} ({Raw}).", batchName, status.State, status.RawState);
                foreach (var l in lessons) RequeueOrFail(l, $"Batch chép lời {status.State}.", now);
                await db.SaveChangesAsync(ct);
                await gemini.DeleteBatchAsync(batchName, ct);
                continue;
            }

            var byKey = status.Items.Where(i => i.Key != null)
                .GroupBy(i => i.Key!)
                .ToDictionary(g => g.Key, g => g.First());
            // Phòng khi Google không trả lại metadata.key: kết quả theo đúng thứ tự gửi, và lúc gửi
            // các buổi được sắp theo (Transcriptqueuedat, Lessonid) — ghép theo vị trí.
            if (byKey.Count == 0 && status.Items.Count == lessons.Count)
            {
                var ordered = lessons.OrderBy(l => l.Transcriptqueuedat).ThenBy(l => l.Lessonid).ToList();
                for (var i = 0; i < ordered.Count; i++) byKey[KeyOf(ordered[i])] = status.Items[i];
            }
            var context = await LoadContextAsync(db, lessons, ct);

            foreach (var lesson in lessons)
            {
                try
                {
                    if (!byKey.TryGetValue(KeyOf(lesson), out var item) || item.Text is null)
                    {
                        RequeueOrFail(lesson, item?.Error ?? "Không có kết quả trong batch.", now);
                    }
                    else
                    {
                        await SaveTranscriptAsync(storage, lesson, context, item.Text, settings.TranscriptModel, now, ct);
                    }
                    await db.SaveChangesAsync(ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Không lưu được transcript buổi {LessonId}.", lesson.Lessonid);
                    RequeueOrFail(lesson, "Không lưu được transcript lên kho.", now);
                    await db.SaveChangesAsync(ct);
                }
            }

            // Kết quả đã nằm trên kho của mình — xoá bản Google giữ (mặc định 6 tuần).
            await gemini.DeleteBatchAsync(batchName, ct);
            logger.LogInformation("Batch {Batch}: xử lý {Count} buổi.", batchName, lessons.Count);
        }
    }

    /// <summary>Lỗi lần đầu → xếp hàng lại; lỗi lần hai → failed (tránh lặp vô hạn tốn tiền).</summary>
    private static void RequeueOrFail(RecorderLesson lesson, string error, DateTime now)
    {
        var firstFailure = string.IsNullOrEmpty(lesson.Transcripterror);
        lesson.Transcriptbatch = null;
        lesson.Transcripterror = Truncate(error, 500);
        lesson.Transcriptstatus = firstFailure ? RecorderTranscriptStatus.Queued : RecorderTranscriptStatus.Failed;
        if (firstFailure) lesson.Transcriptqueuedat = now;
        lesson.Updatedat = now;
    }

    // ── 2. Dữ liệu cũ: transcript dạng chữ trong DB → JSON trên kho ─────────────

    private async Task MigrateLegacyTextAsync(IAppDbContext db, IAppRecordingStorage storage, CancellationToken ct)
    {
        var lessons = await db.RecorderLessons
            .Where(l => l.Transcript != null && l.Transcriptkey == null)
            .OrderBy(l => l.Createdat)
            .Take(20)
            .ToListAsync(ct);
        if (lessons.Count == 0) return;

        var context = await LoadContextAsync(db, lessons, ct);
        var now = TimeZoneHelper.UtcNow;
        foreach (var lesson in lessons)
        {
            try
            {
                await SaveTranscriptAsync(storage, lesson, context, lesson.Transcript!, "legacy", now, ct);
                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Không chuyển được transcript cũ của buổi {LessonId}.", lesson.Lessonid);
            }
        }
    }

    // ── 3. Gom buổi đang xếp hàng thành batch mới ─────────────────────────────

    private async Task SubmitQueuedAsync(
        IAppDbContext db, IAppRecordingStorage storage, IGeminiVideoAnalysisService gemini, IRecorderAiService recorderAi,
        GoogleGeminiSettings settings, CancellationToken ct)
    {
        var queued = await db.RecorderLessons
            .Where(l => l.Transcriptstatus == RecorderTranscriptStatus.Queued
                && l.Status != SessionRecordingStatus.Discarded)
            .OrderBy(l => l.Transcriptqueuedat)
            .ThenBy(l => l.Lessonid)
            .Take(MaxPerBatch)
            .ToListAsync(ct);
        if (queued.Count == 0) return;

        var now = TimeZoneHelper.UtcNow;
        var oldest = queued[0].Transcriptqueuedat ?? now;
        // Chờ gom thêm buổi cho đỡ số batch — trừ khi đã đủ một batch đầy.
        if (settings.TranscriptUseBatch && queued.Count < MaxPerBatch
            && oldest > now.AddMinutes(-settings.TranscriptBatchDelayMinutes)) return;

        var items = new List<GeminiBatchAudioItem>();
        var included = new List<RecorderLesson>();
        foreach (var lesson in queued)
        {
            if (lesson.Audiodeletedat != null || string.IsNullOrWhiteSpace(lesson.Storagekey))
            {
                lesson.Transcriptstatus = RecorderTranscriptStatus.Failed;
                lesson.Transcripterror = "Không còn file ghi âm để chép lời.";
                lesson.Updatedat = now;
                continue;
            }
            try
            {
                var uri = await recorderAi.EnsureGeminiFileAsync(
                    lesson, settings.TranscriptUseBatch ? MinGeminiFileLife : TimeSpan.FromMinutes(30), ct);
                items.Add(new GeminiBatchAudioItem(KeyOf(lesson), uri, AudioMimeType));
                included.Add(lesson);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Không chuẩn bị được audio cho buổi {LessonId}.", lesson.Lessonid);
                RequeueOrFail(lesson, "Không chuẩn bị được audio cho Gemini.", now);
            }
        }

        if (items.Count > 0 && !settings.TranscriptUseBatch)
        {
            await TranscribeDirectAsync(db, storage, gemini, settings, items, included, now, ct);
        }
        else if (items.Count > 0)
        {
            try
            {
                var batchName = await gemini.CreateLessonTranscriptBatchAsync(
                    items, $"tutora-transcripts-{now:yyyyMMdd-HHmm}", ct);
                foreach (var lesson in included)
                {
                    lesson.Transcriptstatus = RecorderTranscriptStatus.Batching;
                    lesson.Transcriptbatch = batchName;
                    lesson.Updatedat = now;
                }
            }
            catch (GeminiApiException ex) when (ex.StatusCode == 400)
            {
                // 400 FAILED_PRECONDITION: project/khoá API chưa dùng được Batch API (thường do chưa
                // bật billing — gói free, hoặc model/vùng chưa hỗ trợ batch). Thử lại cũng vô ích →
                // chép lời từng buổi bằng lời gọi thường (giá đầy đủ) để transcript vẫn có.
                logger.LogWarning(
                    "Batch API bị từ chối (400) — chép lời trực tiếp {Count} buổi. Kiểm tra billing của project Gemini.",
                    items.Count);
                await TranscribeDirectAsync(db, storage, gemini, settings, items, included, now, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Lỗi tạm thời (mạng, 5xx…): để nguyên queued, lượt sau thử lại.
                logger.LogError(ex, "Không tạo được batch chép lời cho {Count} buổi.", items.Count);
            }
        }
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Chép lời từng buổi bằng lời gọi thường (không batch). Mỗi buổi lưu riêng để một buổi
    /// lỗi không kéo theo các buổi khác.</summary>
    private async Task TranscribeDirectAsync(
        IAppDbContext db, IAppRecordingStorage storage, IGeminiVideoAnalysisService gemini,
        GoogleGeminiSettings settings, List<GeminiBatchAudioItem> items, List<RecorderLesson> lessons,
        DateTime now, CancellationToken ct)
    {
        var context = await LoadContextAsync(db, lessons, ct);
        foreach (var lesson in lessons)
        {
            try
            {
                var uri = items.First(i => i.Key == KeyOf(lesson)).FileUri;
                var text = await gemini.TranscribeLessonAudioAsync(uri, AudioMimeType, ct);
                await SaveTranscriptAsync(storage, lesson, context, text, settings.TranscriptModel, now, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Chép lời trực tiếp buổi {LessonId} thất bại.", lesson.Lessonid);
                RequeueOrFail(lesson, "Chép lời trực tiếp thất bại.", now);
            }
            await db.SaveChangesAsync(ct);
        }
        logger.LogInformation("Chép lời trực tiếp {Count} buổi.", lessons.Count);
    }

    // ── 4. Hạn lưu transcript ────────────────────────────────────────────────

    private async Task DeleteExpiredAsync(
        IAppDbContext db, IAppRecordingStorage storage, GoogleGeminiSettings settings, CancellationToken ct)
    {
        var cutoff = TimeZoneHelper.UtcNow.AddDays(-settings.TranscriptRetentionDays);
        var expired = await db.RecorderLessons
            .Where(l => l.Transcriptkey != null && (l.Endedat ?? l.Createdat) < cutoff)
            .OrderBy(l => l.Createdat)
            .Take(50)
            .ToListAsync(ct);

        foreach (var lesson in expired)
        {
            try
            {
                await storage.DeleteTranscriptAsync(lesson.Transcriptkey!, ct);
                lesson.Transcriptkey = null;
                lesson.Transcriptstatus = RecorderTranscriptStatus.Deleted;
                lesson.Updatedat = TimeZoneHelper.UtcNow;
                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Không xoá được transcript hết hạn của buổi {LessonId}.", lesson.Lessonid);
            }
        }
    }

    // ── nội bộ ────────────────────────────────────────────────────────────────

    private sealed record NameContext(
        Dictionary<Guid, RecorderStudent> Students, Dictionary<string, string?> TutorNames);

    private static async Task<NameContext> LoadContextAsync(IAppDbContext db, List<RecorderLesson> lessons, CancellationToken ct)
    {
        var studentIds = lessons.Where(l => l.Studentid != null).Select(l => l.Studentid!.Value).Distinct().ToList();
        var tutorIds = lessons.Select(l => l.Tutorid).Distinct().ToList();

        var students = await db.RecorderStudents
            .Where(s => studentIds.Contains(s.Studentid))
            .ToDictionaryAsync(s => s.Studentid, ct);
        var tutors = await db.Users
            .Where(u => tutorIds.Contains(u.Userid))
            .Select(u => new { u.Userid, u.Fullname })
            .ToDictionaryAsync(u => u.Userid, u => u.Fullname, ct);
        return new NameContext(students, tutors);
    }

    private static async Task SaveTranscriptAsync(
        IAppRecordingStorage storage, RecorderLesson lesson, NameContext context, string text,
        string model, DateTime now, CancellationToken ct)
    {
        RecorderStudent? student = null;
        if (lesson.Studentid is { } sid) context.Students.TryGetValue(sid, out student);
        context.TutorNames.TryGetValue(lesson.Tutorid, out var tutorName);

        var segments = RecorderTranscriptFormatter.Anonymize(
            RecorderTranscriptFormatter.Parse(text), student?.Fullname, tutorName, student?.Parentname);
        var json = RecorderTranscriptFormatter.BuildJson(lesson, student, segments, model, now);

        var key = storage.BuildTranscriptKey(lesson.Lessonid, lesson.Endedat ?? lesson.Createdat);
        await storage.PutTranscriptAsync(key, json, ct);

        lesson.Transcriptkey = key;
        lesson.Transcriptmodel = model;
        lesson.Transcriptstatus = RecorderTranscriptStatus.Completed;
        lesson.Transcripterror = null;
        lesson.Transcriptbatch = null;
        // Bản chữ trong DB (chưa ẩn danh) không giữ nữa — nguồn duy nhất là file trên kho.
        lesson.Transcript = null;
        lesson.Updatedat = now;
    }

    private static string KeyOf(RecorderLesson lesson) => lesson.Lessonid.ToString("N");

    private static string Truncate(string v, int max) => v.Length <= max ? v : v[..max];
}
