using MV.DomainLayer.DTO.RequestModel;
using MV.DomainLayer.DTO.ResponseModel;

namespace MV.ApplicationLayer.ServiceInterfaces;

/// <summary>
/// Ghi âm buổi dạy từ app gia sư — dữ liệu nằm trong recorder.lessons.
/// "recordingId" mà app dùng chính là LessonId.
/// </summary>
public interface IAppRecordingService
{
    /// <summary>Buổi có booking.</summary>
    Task<AppRecordingStartResponse> StartForClassSessionAsync(
        int classSessionId, string tutorUserId, string? consentSnapshotJson, CancellationToken ct = default);

    /// <summary>Buổi đã tạo sẵn trong nhật ký (học sinh ngoài nền tảng).</summary>
    Task<AppRecordingStartResponse> StartForLessonAsync(
        Guid lessonId, string tutorUserId, string? consentSnapshotJson, CancellationToken ct = default);

    /// <summary>Ghi ngay cho một học sinh ngoài nền tảng — tự tạo buổi mới.</summary>
    Task<AppRecordingStartResponse> StartForStudentAsync(
        Guid studentId, string tutorUserId, string? consentSnapshotJson, CancellationToken ct = default);

    Task<AppRecordingUploadUrlResponse> CreateUploadUrlAsync(
        Guid recordingId, string tutorUserId, int partNumber, CancellationToken ct = default);

    Task<AppRecordingStatusResponse> CompleteAsync(
        Guid recordingId, string tutorUserId, int durationSec, CancellationToken ct = default);

    Task<AppRecordingStatusResponse> GetAsync(
        Guid recordingId, string tutorUserId, CancellationToken ct = default);

    Task DiscardAsync(Guid recordingId, string tutorUserId, CancellationToken ct = default);

    /// <summary>Gia sư báo nội dung AI (báo cáo / biên bản) của buổi này bị sai hoặc không phù hợp.</summary>
    Task ReportAiFeedbackAsync(Guid recordingId, string tutorUserId, RecorderAiFeedbackRequest request, CancellationToken ct = default);

    /// <summary>
    /// Link presigned ngắn hạn (10 phút) để ADMIN nghe file đã ghép. Gia sư không được nghe lại bản
    /// ghi — không có endpoint nào cho gia sư gọi hàm này. Hết hạn lưu trữ thì báo lỗi.
    /// </summary>
    Task<AppRecordingAudioUrlResponse> GetAudioUrlForAdminAsync(Guid lessonId, CancellationToken ct = default);

    /// <summary>Gia sư duyệt báo cáo đã sửa → gửi phụ huynh.</summary>
    Task<AppRecordingStatusResponse> ApproveAsync(
        Guid recordingId, string tutorUserId, RecorderApproveRequest request, CancellationToken ct = default);
}
