namespace MV.DomainLayer.Constants;

/// <summary>Trạng thái job AI của một buổi trong recorder.lessons.</summary>
public static class RecorderAiStatus
{
    public const string None = "none";
    public const string Pending = "pending";
    public const string Processing = "processing";
    public const string Completed = "completed";
    public const string Failed = "failed";
}

/// <summary>Phụ huynh (học sinh ngoài nền tảng) đã đồng ý ghi âm + nhận báo cáo chưa.</summary>
public static class RecorderConsentStatus
{
    public const string Unknown = "unknown";
    /// <summary>Gia sư xác nhận đã hỏi và phụ huynh đồng ý.</summary>
    public const string TutorConfirmed = "tutor_confirmed";
    /// <summary>Phụ huynh tự xác nhận qua Zalo Mini App (đã quan tâm OA).</summary>
    public const string ParentConfirmed = "parent_confirmed";
    public const string Declined = "declined";
}

/// <summary>Kênh đưa báo cáo tới phụ huynh.</summary>
public static class RecorderDeliveryChannel
{
    /// <summary>Buổi có booking: đi luồng nộp báo cáo sẵn có của nền tảng.</summary>
    public const string Booking = "booking";
    /// <summary>Học sinh ngoài nền tảng: Tutora gửi ZNS tới SĐT phụ huynh.</summary>
    public const string Zns = "zns";
}

/// <summary>Hạn lưu trữ file âm thanh gốc. Báo cáo + lời thoại giữ lâu dài.</summary>
public static class RecorderRetention
{
    public const int AudioDays = 30;
}

public static class RecorderDeliveryStatus
{
    public const string Pending = "pending";
    public const string Sent = "sent";
    public const string Failed = "failed";
}

/// <summary>Hành động ghi vào recorder.consent_events.</summary>
public static class RecorderConsentAction
{
    public const string Linked = "linked";
    public const string Unlinked = "unlinked";
    public const string Declined = "declined";
}

/// <summary>Cách phụ huynh xác nhận.</summary>
public static class RecorderConsentMethod
{
    /// <summary>Mở link mời trong Mini App, quan tâm OA và bấm đồng ý.</summary>
    public const string MiniAppFollow = "miniapp_follow";
    public const string Tutor = "tutor";
}

/// <summary>Link mời phụ huynh.</summary>
public static class RecorderParentInviteRules
{
    public const int ValidDays = 14;
    /// <summary>Phiên bản nội dung đồng ý hiện trên Mini App — đổi khi sửa nội dung.</summary>
    public const string ConsentVersion = "2026-09-v1";
}

/// <summary>Trạng thái liên kết Zalo của phụ huynh, hiển thị trên app gia sư.</summary>
public static class RecorderParentLinkStatus
{
    public const string None = "none";
    public const string Invited = "invited";
    public const string Linked = "linked";
    /// <summary>Đã liên kết nhưng phụ huynh bỏ quan tâm OA.</summary>
    public const string Unfollowed = "unfollowed";
}

/// <summary>
/// Trạng thái transcript thô (recorder.lessons.transcript_status). Transcript không hiển thị cho
/// gia sư — được chép lời nền qua Gemini Batch API rồi lưu JSON ẩn danh trên kho.
/// </summary>
public static class RecorderTranscriptStatus
{
    public const string None = "none";
    /// <summary>Đã xếp hàng, chờ job gom vào batch.</summary>
    public const string Queued = "queued";
    /// <summary>Đang nằm trong một batch Gemini (xem transcript_batch).</summary>
    public const string Batching = "batching";
    public const string Completed = "completed";
    public const string Failed = "failed";
    /// <summary>File đã xoá theo hạn lưu trữ transcript.</summary>
    public const string Deleted = "deleted";
}
