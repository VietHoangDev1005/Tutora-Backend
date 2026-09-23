namespace MV.DomainLayer.Entities;

/// <summary>
/// Phụ huynh đã tự xác nhận qua Zalo Mini App (schema <c>recorder</c>). Không phải
/// tài khoản Tutora — chỉ là người nhận báo cáo, định danh bằng UID theo OA.
/// </summary>
public class RecorderParent
{
    public Guid Parentid { get; set; }

    /// <summary>UID theo OA Tutora — gửi Tin Tư vấn / ZBS qua UID.</summary>
    public string Zalouid { get; set; } = null!;

    /// <summary>ID theo Zalo App (Mini App / Graph API).</summary>
    public string? Zaloappuserid { get; set; }
    public string? Displayname { get; set; }
    public bool Isfollower { get; set; }

    /// <summary>Lần cuối phụ huynh tương tác với OA (UTC).</summary>
    public DateTime? Lastinteractionat { get; set; }
    public DateTime Createdat { get; set; }
    public DateTime Updatedat { get; set; }
}
