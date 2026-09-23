namespace MV.DomainLayer.Constants;

/// <summary>Vòng đời một bản ghi âm buổi học từ app gia sư.</summary>
public static class SessionRecordingStatus
{
    /// <summary>Buổi đã lên lịch (gia sư tạo trước), chưa ghi.</summary>
    public const string Scheduled = "scheduled";

    /// <summary>App đang ghi; chưa đoạn nào được chốt.</summary>
    public const string Recording = "recording";

    /// <summary>Đang đẩy các đoạn lên S3.</summary>
    public const string Uploading = "uploading";

    /// <summary>Đã đủ đoạn, job AI đang chạy.</summary>
    public const string Processing = "processing";

    /// <summary>AI xong, chờ gia sư xem và duyệt.</summary>
    public const string AwaitingApproval = "awaiting_approval";

    /// <summary>Gia sư đã duyệt và báo cáo đã đi.</summary>
    public const string Sent = "sent";

    /// <summary>Hỏng ở đâu đó; file vẫn giữ để còn cứu.</summary>
    public const string Failed = "failed";

    /// <summary>Gia sư chủ động bỏ. Không tính vào unique index.</summary>
    public const string Discarded = "discarded";
}
