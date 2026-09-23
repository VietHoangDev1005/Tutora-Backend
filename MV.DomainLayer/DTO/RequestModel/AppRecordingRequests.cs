namespace MV.DomainLayer.DTO.RequestModel;

public class AppRecordingStartRequest
{
    /// <summary>
    /// JSON ghi lại sự đồng ý tại thời điểm bấm ghi: ai đồng ý, lúc nào, phiên
    /// bản thông báo nào. Lưu snapshot vì văn bản thông báo sẽ đổi, còn bằng
    /// chứng cho buổi này phải giữ nguyên như lúc đó.
    /// </summary>
    public string? ConsentSnapshot { get; set; }
}

public class AppRecordingCompleteRequest
{
    /// <summary>Độ dài buổi ghi theo đồng hồ của app, tính bằng giây.</summary>
    public int DurationSec { get; set; }
}
