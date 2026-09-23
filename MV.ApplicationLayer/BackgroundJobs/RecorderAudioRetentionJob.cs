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
/// Xoá file âm thanh của buổi ghi âm từ app sau <see cref="RecorderRetention.AudioDays"/> ngày
/// (các đoạn part-*.m4a + merged.m4a). Báo cáo và lời thoại vẫn giữ trong DB.
///
/// Supabase Storage không có lifecycle rule như S3 nên phải tự dọn. Chạy 6 giờ một lần,
/// mỗi lượt tối đa 200 buổi để không giữ kết nối DB quá lâu.
/// </summary>
public class RecorderAudioRetentionJob(IServiceProvider sp, ILogger<RecorderAudioRetentionJob> logger)
    : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromHours(6);
    private const int BatchSize = 200;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Đợi app khởi động xong (migration, Hangfire) rồi mới quét.
        try { await Task.Delay(TimeSpan.FromMinutes(1), ct); }
        catch (OperationCanceledException) { return; }

        while (!ct.IsCancellationRequested)
        {
            try { await SweepAsync(ct); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Lỗi khi dọn file ghi âm hết hạn.");
            }

            try { await Task.Delay(_interval, ct); }
            catch (OperationCanceledException) { return; }
        }
    }

    private async Task SweepAsync(CancellationToken ct)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IAppRecordingStorage>();
        if (!storage.Enabled) return;

        var cutoff = TimeZoneHelper.UtcNow.AddDays(-RecorderRetention.AudioDays);
        var expired = await db.RecorderLessons
            .Where(l => l.Audiodeletedat == null
                && l.Storagekey != null
                && l.Endedat != null && l.Endedat < cutoff)
            .OrderBy(l => l.Endedat)
            .Take(BatchSize)
            .ToListAsync(ct);

        foreach (var lesson in expired)
        {
            try
            {
                var n = await storage.DeletePrefixAsync(lesson.Storagekey!, ct);
                lesson.Audiodeletedat = TimeZoneHelper.UtcNow;
                lesson.Audiokey = null;
                // File Gemini cũng hết hạn sau ~48h — xoá tham chiếu cho sạch.
                lesson.Geminifilename = null;
                lesson.Geminifileuri = null;
                lesson.Geminifileexpiresat = null;
                await db.SaveChangesAsync(ct);
                logger.LogInformation("Đã xoá {Count} file âm thanh của buổi {LessonId} (quá {Days} ngày).",
                    n, lesson.Lessonid, RecorderRetention.AudioDays);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Chưa xoá được file âm thanh của buổi {LessonId}, lượt sau thử lại.", lesson.Lessonid);
            }
        }
    }
}
