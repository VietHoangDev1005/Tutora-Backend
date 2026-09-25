using Hangfire;
using Hangfire.Common;
using MV.ApplicationLayer.ServiceInterfaces;
using Xunit;

namespace MV.ApplicationLayer.Tests;

/// <summary>
/// Báo cáo AI của app ghi âm chạy trên queue riêng "recorder" (nhiều worker song song), không xếp
/// hàng sau các job nền của queue "default" (1 worker).
/// </summary>
public class RecorderQueueTests
{
    [Fact]
    public void LessonReportJob_IsEnqueuedOnRecorderQueue()
    {
        var job = Job.FromExpression<IRecorderAiService>(s => s.RunLessonReportJobAsync(Guid.Empty));

        var queue = JobFilterProviders.Providers.GetFilters(job)
            .Select(f => f.Instance)
            .OfType<QueueAttribute>()
            .Single()
            .Queue;

        Assert.Equal("recorder", queue);
    }
}
