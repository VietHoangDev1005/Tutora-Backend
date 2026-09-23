using System.ComponentModel.DataAnnotations;

namespace MV.DomainLayer.DTO.RequestModel;

/// <summary>
/// Đăng nhập Zalo từ app mobile: Zalo SDK native tự đổi mã PKCE lấy access token
/// trên thiết bị, app chỉ gửi access token lên để backend tra Zalo ID qua Graph API.
/// </summary>
public class ZaloAppLoginRequest
{
    [Required(ErrorMessage = "Access token Zalo là bắt buộc.")]
    public string AccessToken { get; set; } = string.Empty;
}
