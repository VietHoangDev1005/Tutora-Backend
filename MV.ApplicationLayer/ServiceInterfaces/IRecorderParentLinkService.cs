using MV.DomainLayer.DTO.RequestModel;
using MV.DomainLayer.DTO.ResponseModel;

namespace MV.ApplicationLayer.ServiceInterfaces;

/// <summary>
/// Liên kết phụ huynh (học sinh ngoài nền tảng) với OA Tutora qua Zalo Mini App:
/// gia sư tạo link mời → phụ huynh mở trong Mini App, quan tâm OA, đồng ý →
/// backend xác minh với Zalo rồi gắn UID vào học sinh.
/// </summary>
public interface IRecorderParentLinkService
{
    /// <summary>Gia sư tạo link mời mới (thu hồi link cũ còn hiệu lực).</summary>
    Task<RecorderParentInviteResponse> CreateInviteAsync(Guid studentId, string tutorId, CancellationToken ct = default);

    /// <summary>Gia sư gỡ liên kết phụ huynh khỏi học sinh.</summary>
    Task UnlinkAsync(Guid studentId, string tutorId, CancellationToken ct = default);

    /// <summary>Mini App xem trước lời mời (không cần đăng nhập — token là bí mật).</summary>
    Task<RecorderParentInvitePreviewResponse> GetInvitePreviewAsync(string token, CancellationToken ct = default);

    /// <summary>Phụ huynh đồng ý trong Mini App.</summary>
    Task<RecorderParentLinkResultResponse> AcceptInviteAsync(
        string token, RecorderParentLinkAcceptRequest request, string? ipAddress, string? userAgent,
        CancellationToken ct = default);
}
