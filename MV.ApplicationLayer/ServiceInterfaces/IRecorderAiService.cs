using MV.DomainLayer.Entities;

namespace MV.ApplicationLayer.ServiceInterfaces;

/// <summary>Job Hangfire: ghép các đoạn ghi âm của một buổi → Gemini → bản nháp báo cáo.</summary>
public interface IRecorderAiService
{
    Task RunLessonReportJobAsync(Guid lessonId);

    /// <summary>Đảm bảo buổi có file audio trên Gemini còn sống ít nhất <paramref name="minRemaining"/>
    /// (ghép lại + upload lại nếu cần). Trả về fileUri.</summary>
    Task<string> EnsureGeminiFileAsync(RecorderLesson lesson, TimeSpan minRemaining, CancellationToken ct = default);
}
