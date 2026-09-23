using MV.DomainLayer.DTO.ResponseModel.Zalo;

namespace MV.ApplicationLayer.ServiceInterfaces;

public interface IZaloOAService
{
    /// <summary>
    /// Send an OTP through ZNS using the verified phone number.
    /// </summary>
    Task<ZaloSendResult> SendZnsOtpAsync(string phone, string otp);

    /// <summary>
    /// Send a post-classSession report notification to the student's parent via Zalo OA template.
    /// </summary>
    Task<ZaloSendResult> SendClassSessionReportAsync(int classSessionId);

    /// <summary>
    /// Send a generic Zalo OA template message to a user with dynamic data fields.
    /// </summary>
    Task<ZaloSendResult> SendNotificationAsync(string userId, string templateId, Dictionary<string, string> data);

    /// <summary>
    /// Gửi ZBS Template Message (thay ZNS) thẳng tới SĐT — không cần UID/Mini App.
    /// Trả về <see cref="ZaloSendResult.ErrorCode"/> để bên gọi phân loại lỗi (SĐT không dùng Zalo,
    /// ngoài giờ gửi, từ chối nhận tin...) và <see cref="ZaloSendResult.IsTransient"/> cho lỗi mạng/5xx.
    /// Không ném exception (trừ khi <paramref name="ct"/> bị huỷ).
    /// </summary>
    Task<ZaloSendResult> SendZbsTemplateByPhoneAsync(
        string phone, string templateId, Dictionary<string, string> templateData, CancellationToken ct = default);

    /// <summary>
    /// Check whether a user has linked their Zalo account.
    /// </summary>
    Task<bool> IsZaloLinkedAsync(string userId);

    /// <summary>
    /// Lấy thông tin người dùng theo UID của OA (user_id_by_app, trạng thái quan tâm).
    /// Trả null khi không gọi được (mock mode, UID sai, thiếu quyền).
    /// </summary>
    Task<ZaloOAUserDetail?> GetOAUserDetailAsync(string oaUserId, CancellationToken ct = default);

    // ── Token management ──────────────────────────────────────────────────

    /// <summary>
    /// Return a valid Zalo OA access token, refreshing if necessary.
    /// </summary>
    Task<string> GetOAAccessTokenAsync();

    /// <summary>
    /// Proactively refresh the access token if it is missing or about to expire.
    /// Called by a background job so the token never lapses between the lazy
    /// refresh points (the bot only reads the token, it never triggers a refresh).
    /// </summary>
    Task EnsureFreshTokenAsync();

    // ── OA Reply API ──────────────────────────────────────────────────────

    /// <summary>
    /// Send a plain text reply to a Zalo user from the OA account.
    /// </summary>
    Task SendOAMessageAsync(string recipientZaloId, string text);

    /// <summary>
    /// Send a text message with quick-reply buttons to a Zalo user from the OA account.
    /// </summary>
    Task SendOAMessageWithButtonsAsync(string recipientZaloId, string text, List<ZaloQuickReply> buttons);
}
