namespace MV.DomainLayer.Entities;

/// <summary>
/// Nhật ký đồng ý của phụ huynh — bằng chứng khi Zalo yêu cầu chứng minh quan hệ
/// với người nhận, hoặc khi phụ huynh thực hiện quyền của chủ thể dữ liệu.
/// Chỉ thêm, không sửa.
/// </summary>
public class RecorderConsentEvent
{
    public Guid Eventid { get; set; }
    public Guid Studentid { get; set; }
    public Guid? Parentid { get; set; }

    /// <summary>Xem <see cref="Constants.RecorderConsentAction"/>.</summary>
    public string Action { get; set; } = null!;
    public string Method { get; set; } = null!;
    public string? Consentversion { get; set; }
    public string? Zalouid { get; set; }
    public string? Ipaddress { get; set; }
    public string? Useragent { get; set; }
    public DateTime Createdat { get; set; }
}
