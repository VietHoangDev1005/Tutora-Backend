using MV.DomainLayer.DTO.RequestModel;
using MV.DomainLayer.DTO.ResponseModel;

namespace MV.ApplicationLayer.ServiceInterfaces
{
    public interface IZaloAuthService
    {
        /// <summary>
        /// Zalo Login v4 (Web OAuth): đổi authorization code lấy access token,
        /// lấy profile và tạo phiên hoàn tất role/phone OTP nếu cần.
        /// </summary>
        Task<TokenResponse> LoginWithZaloCodeAsync(ZaloWebLoginRequest request);

        /// <summary>
        /// Zalo Login từ app mobile (SDK native): nhận thẳng Zalo access token,
        /// lấy profile và đi tiếp y hệt luồng web.
        /// </summary>
        Task<TokenResponse> LoginWithZaloAccessTokenAsync(ZaloAppLoginRequest request);

        /// <summary>
        /// Xác minh access token (VD: lấy từ Zalo Mini App) qua Graph API và trả về ID
        /// người dùng theo Zalo App. <paramref name="appSecret"/> là khoá của Zalo App
        /// cấp token; null = dùng khoá ZaloOA. Trả null nếu token không hợp lệ.
        /// </summary>
        Task<string?> GetZaloAppUserIdAsync(string accessToken, string? appSecret = null);
    }
}
