using Hangfire;
using MV.DomainLayer.Entities;

namespace MV.ApplicationLayer.ServiceInterfaces;

/// <summary>Job Hangfire: ghép các đoạn ghi âm của một buổi → Gemini → bản nháp báo cáo.</summary>
public interface IRecorderAiService
{
    /// <summary>Queue riêng "recorder" (Program.cs: recorder-worker, nhiều worker song song) — trước
    /// đây rơi vào "default" với 1 worker nên báo cáo xếp hàng từng cái một. Đo 2026-09-25: bản ghi
    /// 52 phút mất ~45 s, phần lớn là chờ Gemini, nên chạy song song không tốn thêm CPU đáng kể.</summary>
    [Queue(RecorderQueue)]
    Task RunLessonReportJobAsync(Guid lessonId);

    /// <summary>Tên queue của job báo cáo app ghi âm.</summary>
    const string RecorderQueue = "recorder";

    /// <summary>Đảm bảo buổi có file audio trên Gemini còn sống ít nhất <paramref name="minRemaining"/>
    /// (ghép lại + upload lại nếu cần). Trả về fileUri.</summary>
    Task<string> EnsureGeminiFileAsync(RecorderLesson lesson, TimeSpan minRemaining, CancellationToken ct = default);
}
