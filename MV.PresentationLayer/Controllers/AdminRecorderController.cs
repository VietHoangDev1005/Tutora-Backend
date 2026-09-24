using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MV.ApplicationLayer.ServiceInterfaces;
using MV.DomainLayer.Constants;
using MV.DomainLayer.DTO;
using MV.DomainLayer.DTO.ResponseModel;

namespace MV.PresentationLayer.Controllers;

/// <summary>
/// Truy cập bản ghi âm từ app gia sư (schema recorder) — CHỈ admin Tutora. Gia sư không nghe
/// lại được bản ghi; mọi lượt admin mở file đều được ghi log (admin id + lesson id).
/// </summary>
[ApiController]
[Route("api/admin/recorder")]
[Authorize(Roles = UserRole.Admin)]
public class AdminRecorderController(
    IAppRecordingService recordings,
    ILogger<AdminRecorderController> logger) : ControllerBase
{
    private string AdminUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    /// <summary>
    /// GET /api/admin/recorder/lessons?search=&amp;onlyWithAudio=&amp;page=&amp;pageSize= — danh sách buổi ghi âm
    /// từ app gia sư (mới nhất trước) cho trang "Bản ghi âm" trên CMS.
    /// </summary>
    [HttpGet("lessons")]
    public async Task<IActionResult> ListLessons(
        [FromQuery] string? search, [FromQuery] bool onlyWithAudio = false,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(APIResponse<AdminRecorderLessonPage>.Success(
            await recordings.ListLessonsForAdminAsync(search, onlyWithAudio, page, pageSize, ct),
            "Lấy danh sách bản ghi thành công."));

    /// <summary>
    /// GET /api/admin/recorder/lessons/{lessonId}/audio — link presigned 10 phút tới merged.m4a.
    /// 404 nếu không có buổi; 400 nếu file đã hết hạn lưu trữ / chưa ghép xong.
    /// </summary>
    [HttpGet("lessons/{lessonId:guid}/audio")]
    public async Task<IActionResult> GetLessonAudio(Guid lessonId, CancellationToken ct)
    {
        // Ghi log TRƯỚC khi cấp link: cả lượt thử thất bại cũng là một lần truy cập cần dấu vết.
        logger.LogWarning(
            "[RecorderAudioAccess] Admin {AdminUserId} requested audio of recorder lesson {LessonId} from {Ip}.",
            AdminUserId, lessonId, HttpContext.Connection.RemoteIpAddress?.ToString());

        var result = await recordings.GetAudioUrlForAdminAsync(lessonId, ct);
        return Ok(APIResponse<AppRecordingAudioUrlResponse>.Success(result, "Đã cấp link nghe bản ghi (10 phút)."));
    }
}
