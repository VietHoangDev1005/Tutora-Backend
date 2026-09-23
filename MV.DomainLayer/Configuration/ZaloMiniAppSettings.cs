namespace MV.DomainLayer.Configuration;

/// <summary>
/// Zalo Mini App cho phụ huynh (liên kết Zalo, xem báo cáo).
///
///   MiniAppId : ID Mini App (Mini App Center) — dùng tạo link https://zalo.me/s/{MiniAppId}/
///   SecretKey : khoá bí mật của Zalo App chứa Mini App — backend dùng để gọi Graph API
///               với access token lấy từ Mini App (getAccessToken). Để trống thì dùng
///               khoá của ZaloOA (khi Mini App và OA dùng chung một Zalo App).
///   AllowUnverifiedInDevelopment: chỉ Development — chấp nhận idByOA từ Mini App mà
///               không đối chiếu với Zalo (khi chưa có OA token). KHÔNG bật ở production.
/// </summary>
public class ZaloMiniAppSettings
{
    public const string SectionName = "ZaloMiniApp";

    public string MiniAppId { get; set; } = string.Empty;
    public string? SecretKey { get; set; }
    public bool AllowUnverifiedInDevelopment { get; set; }

    public string BuildUrl(string path, string query) =>
        $"https://zalo.me/s/{MiniAppId}/{path.TrimStart('/')}{(string.IsNullOrEmpty(query) ? "" : "?" + query)}";
}
