using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MV.ApplicationLayer.ServiceInterfaces;
using MV.DomainLayer.DTO;
using MV.DomainLayer.DTO.RequestModel;

namespace MV.PresentationLayer.Controllers;

/// <summary>
/// Zalo Mini App phụ huynh: mở link mời của gia sư và đồng ý nhận báo cáo.
/// Không cần tài khoản Tutora — token trong link là bí mật, còn danh tính Zalo
/// được backend xác minh lại với Zalo khi đồng ý.
/// </summary>
[ApiController]
[Route("api/parent-link")]
[AllowAnonymous]
public class ParentLinkController(IRecorderParentLinkService service) : ControllerBase
{
    /// <summary>GET /api/parent-link/invites/{token} — thông tin hiển thị trước khi đồng ý.</summary>
    [HttpGet("invites/{token}")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> GetInvite(string token, CancellationToken ct) =>
        Ok(APIResponse<object>.Success(await service.GetInvitePreviewAsync(token, ct), "Lấy lời mời thành công."));

    /// <summary>POST /api/parent-link/invites/{token}/accept</summary>
    [HttpPost("invites/{token}/accept")]
    [EnableRateLimiting("otp")]
    public async Task<IActionResult> Accept(string token, [FromBody] RecorderParentLinkAcceptRequest body, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var ua = Request.Headers.UserAgent.ToString();
        var result = await service.AcceptInviteAsync(token, body, ip, ua, ct);
        return Ok(APIResponse<object>.Success(result, "Đã kết nối Zalo."));
    }
}
