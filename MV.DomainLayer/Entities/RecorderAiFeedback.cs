namespace MV.DomainLayer.Entities;

/// <summary>
/// Gia sư báo nội dung AI tạo ra (báo cáo / biên bản) bị sai hoặc không phù hợp — yêu cầu của
/// Google Play với app có tính năng AI tạo nội dung. Chỉ thêm, không sửa; Tutora đọc để sửa prompt.
/// </summary>
public class RecorderAiFeedback
{
    public Guid Feedbackid { get; set; }
    public Guid Lessonid { get; set; }
    public string Tutorid { get; set; } = null!;

    /// <summary>Xem <see cref="Constants.RecorderAiFeedbackReason"/>.</summary>
    public string Reason { get; set; } = null!;
    public string? Note { get; set; }
    public DateTime Createdat { get; set; }
}
