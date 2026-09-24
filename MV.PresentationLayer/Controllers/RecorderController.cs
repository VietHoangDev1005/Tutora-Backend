using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MV.ApplicationLayer.ServiceInterfaces;
using MV.DomainLayer.Constants;
using MV.DomainLayer.DTO;
using MV.DomainLayer.DTO.RequestModel;

namespace MV.PresentationLayer.Controllers;

/// <summary>
/// Học sinh ngoài nền tảng + nhật ký buổi dạy của gia sư (app ghi âm).
/// Dữ liệu nằm riêng trong schema recorder — không tạo tài khoản, booking
/// hay class_session nào.
/// </summary>
[ApiController]
[Route("api/recorder")]
[Authorize(Roles = UserRole.Tutor)]
public class RecorderController(IRecorderService service, IRecorderParentLinkService parentLink) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    // ── Học sinh ─────────────────────────────────────────────────────────────

    [HttpGet("students")]
    public async Task<IActionResult> ListStudents(CancellationToken ct) =>
        Ok(APIResponse<object>.Success(await service.ListStudentsAsync(UserId, ct), "Lấy danh sách học sinh thành công."));

    [HttpGet("students/{studentId:guid}")]
    public async Task<IActionResult> GetStudent(Guid studentId, CancellationToken ct) =>
        Ok(APIResponse<object>.Success(await service.GetStudentAsync(studentId, UserId, ct), "Lấy học sinh thành công."));

    [HttpPost("students")]
    public async Task<IActionResult> CreateStudent([FromBody] RecorderStudentRequest body, CancellationToken ct) =>
        Ok(APIResponse<object>.Success(await service.CreateStudentAsync(UserId, body, ct), "Đã thêm học sinh."));

    [HttpPut("students/{studentId:guid}")]
    public async Task<IActionResult> UpdateStudent(Guid studentId, [FromBody] RecorderStudentRequest body, CancellationToken ct) =>
        Ok(APIResponse<object>.Success(await service.UpdateStudentAsync(studentId, UserId, body, ct), "Đã cập nhật học sinh."));

    /// <summary>
    /// Xoá vĩnh viễn học sinh và toàn bộ dữ liệu (ghi âm, bản chép lời, báo cáo, đồng ý).
    /// Không khôi phục được. (Tính năng "ẩn học sinh" đã bỏ.)
    /// </summary>
    [HttpDelete("students/{studentId:guid}")]
    public async Task<IActionResult> DeleteStudentPermanently(Guid studentId, CancellationToken ct)
    {
        await service.DeleteStudentPermanentlyAsync(studentId, UserId, ct);
        return Ok(APIResponse<object>.Success(new { }, "Đã xoá học sinh và toàn bộ dữ liệu."));
    }

    // ── Liên kết Zalo phụ huynh ─────────────────────────────────────────────

    /// <summary>Tạo link mời phụ huynh mở trong Zalo Mini App (link cũ bị thu hồi).</summary>
    [HttpPost("students/{studentId:guid}/parent-invite")]
    public async Task<IActionResult> CreateParentInvite(Guid studentId, CancellationToken ct) =>
        Ok(APIResponse<object>.Success(await parentLink.CreateInviteAsync(studentId, UserId, ct), "Đã tạo link mời."));

    /// <summary>Gỡ liên kết Zalo của phụ huynh khỏi học sinh.</summary>
    [HttpDelete("students/{studentId:guid}/parent-link")]
    public async Task<IActionResult> UnlinkParent(Guid studentId, CancellationToken ct)
    {
        await parentLink.UnlinkAsync(studentId, UserId, ct);
        return Ok(APIResponse<object>.Success(new { }, "Đã gỡ liên kết phụ huynh."));
    }

    // ── Buổi dạy ─────────────────────────────────────────────────────────────

    /// <summary>GET /api/recorder/lessons?from=&amp;to=&amp;studentId=</summary>
    [HttpGet("lessons")]
    public async Task<IActionResult> ListLessons(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] Guid? studentId, CancellationToken ct) =>
        Ok(APIResponse<object>.Success(await service.ListLessonsAsync(UserId, from, to, studentId, ct), "Lấy danh sách buổi thành công."));

    [HttpPost("lessons")]
    public async Task<IActionResult> CreateLesson([FromBody] RecorderLessonCreateRequest body, CancellationToken ct) =>
        Ok(APIResponse<object>.Success(await service.CreateLessonAsync(UserId, body, ct), "Đã tạo buổi học."));

    [HttpDelete("lessons/{lessonId:guid}")]
    public async Task<IActionResult> DeleteLesson(Guid lessonId, CancellationToken ct)
    {
        await service.DeleteLessonAsync(lessonId, UserId, ct);
        return Ok(APIResponse<object>.Success(new { }, "Đã xoá buổi học."));
    }
}
