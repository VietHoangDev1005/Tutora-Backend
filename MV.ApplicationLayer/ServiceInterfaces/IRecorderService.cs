using MV.DomainLayer.DTO.RequestModel;
using MV.DomainLayer.DTO.ResponseModel;

namespace MV.ApplicationLayer.ServiceInterfaces;

/// <summary>Danh bạ học sinh ngoài nền tảng + nhật ký buổi dạy của gia sư.</summary>
public interface IRecorderService
{
    Task<IReadOnlyList<RecorderStudentResponse>> ListStudentsAsync(string tutorId, CancellationToken ct = default);
    Task<RecorderStudentResponse> GetStudentAsync(Guid studentId, string tutorId, CancellationToken ct = default);
    Task<RecorderStudentResponse> CreateStudentAsync(string tutorId, RecorderStudentRequest request, CancellationToken ct = default);
    Task<RecorderStudentResponse> UpdateStudentAsync(Guid studentId, string tutorId, RecorderStudentRequest request, CancellationToken ct = default);
    /// <summary>
    /// Xoá VĨNH VIỄN học sinh: file ghi âm + bản chép lời trên kho, mọi buổi, báo cáo, lời mời và
    /// nhật ký đồng ý (xoá theo FK cascade). Dùng cho yêu cầu xoá dữ liệu của phụ huynh / gia sư.
    /// </summary>
    Task DeleteStudentPermanentlyAsync(Guid studentId, string tutorId, CancellationToken ct = default);

    /// <summary>Buổi dạy của gia sư (cả ngoài nền tảng lẫn có booking đã ghi âm), lọc theo khoảng thời gian / học sinh.</summary>
    Task<IReadOnlyList<RecorderLessonResponse>> ListLessonsAsync(
        string tutorId, DateTime? from, DateTime? to, Guid? studentId, CancellationToken ct = default);
    Task<RecorderLessonResponse> CreateLessonAsync(string tutorId, RecorderLessonCreateRequest request, CancellationToken ct = default);
    /// <summary>Chỉ xoá được buổi chưa ghi âm (status scheduled).</summary>
    Task DeleteLessonAsync(Guid lessonId, string tutorId, CancellationToken ct = default);
}
