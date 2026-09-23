namespace MV.DomainLayer.DTO.ResponseModel.Zalo;

/// <summary>
/// Kết quả gửi tin nhắn Zalo OA / ZNS
/// </summary>
public class ZaloSendResult
{
    public bool Success { get; set; }
    public string? MessageId { get; set; }
    public string? Error { get; set; }
    public bool FallbackUsed { get; set; }

    /// <summary>Mã lỗi Zalo trả về (trường "error", vd -118 SĐT không dùng Zalo).
    /// null khi không gọi tới được Zalo hoặc lỗi không có mã.</summary>
    public int? ErrorCode { get; set; }

    /// <summary>Lỗi tạm thời (mạng, timeout, HTTP 5xx, không lấy được token) — nên thử lại sau,
    /// khác với lỗi phía người nhận (không dùng Zalo, từ chối nhận tin...) thử lại cũng vô ích.</summary>
    public bool IsTransient { get; set; }
}

/// <summary>
/// Nút quick reply cho Zalo OA message
/// </summary>
public class ZaloQuickReply
{
    public string Title { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string? ImageIcon { get; set; }
    public string Type { get; set; } = "oa.query.show";
}

/// <summary>
/// Thông tin người dùng theo OA (GET v3.0/oa/user/detail). Chỉ giữ những trường
/// dùng để xác minh liên kết phụ huynh.
/// </summary>
public class ZaloOAUserDetail
{
    public string UserId { get; set; } = string.Empty;
    public string? UserIdByApp { get; set; }
    public string? DisplayName { get; set; }
    public bool IsFollower { get; set; }
}
