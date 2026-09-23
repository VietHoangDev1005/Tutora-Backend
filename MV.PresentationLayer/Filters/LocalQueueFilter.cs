using Hangfire.States;
using MV.ApplicationLayer.ServiceInterfaces;

namespace MV.PresentationLayer.Filters;

/// <summary>
/// Chỉ dùng khi chạy backend ở máy dev nhưng trỏ vào DB dùng chung với prod/Railway
/// (bật bằng cấu hình <c>Hangfire:LocalQueue</c>, ví dụ env <c>Hangfire__LocalQueue=local_dev</c>).
///
/// Hangfire lưu hàng đợi trong chính DB đó, nên job do máy dev tạo có thể bị worker của
/// VPS/Railway (đang chạy code cũ) rút mất. Filter này chuyển riêng các job điền báo cáo AI sang
/// một queue mà chỉ máy dev lắng nghe; mọi job khác vẫn đi queue gốc như bình thường.
/// Không cấu hình → filter không được đăng ký, hành vi prod không đổi.
/// </summary>
public sealed class LocalQueueFilter(string queue) : IElectStateFilter
{
    public void OnStateElection(ElectStateContext context)
    {
        if (context.CandidateState is not EnqueuedState enqueued) return;

        var job = context.BackgroundJob?.Job;
        var isAppReport = job?.Type == typeof(IRecorderAiService)
            && job.Method.Name == nameof(IRecorderAiService.RunLessonReportJobAsync);
        var isVideoReport = job?.Type == typeof(IClassSessionVideoAiService)
            && job.Method.Name == nameof(IClassSessionVideoAiService.RunTutorReportFillJobAsync);
        if (!isAppReport && !isVideoReport) return;

        enqueued.Queue = queue;
    }
}
