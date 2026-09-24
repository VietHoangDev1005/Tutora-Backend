using System.ComponentModel.DataAnnotations;

namespace MV.DomainLayer.DTO.RequestModel;

/// <summary>Thêm / sửa học sinh trong danh bạ của gia sư.</summary>
public class RecorderStudentRequest
{
    [Required(ErrorMessage = "Tên học sinh là bắt buộc")]
    [StringLength(100)]
    public string FullName { get; set; } = null!;

    [Range(1, 12, ErrorMessage = "Lớp phải từ 1 đến 12")]
    public short? Grade { get; set; }

    [StringLength(100)]
    public string? Subject { get; set; }

    [StringLength(100)]
    public string? ParentName { get; set; }

    [StringLength(20)]
    [RegularExpression(MV.DomainLayer.Helpers.PhoneNumberHelper.VietnamPhonePattern, ErrorMessage = "Số điện thoại phụ huynh không hợp lệ")]
    public string? ParentPhone { get; set; }

    /// <summary>Gia sư xác nhận phụ huynh đã đồng ý ghi âm và nhận báo cáo.</summary>
    public bool ParentConsent { get; set; }

    /// <summary>Phiên bản nội dung đồng ý phụ huynh đã đọc (app hiện gửi "v2").</summary>
    [StringLength(30)]
    public string? ConsentVersion { get; set; }

    [StringLength(1000)]
    public string? Note { get; set; }

    /// <summary>
    /// Thời khoá biểu hằng tuần. null = giữ nguyên lịch cũ; danh sách rỗng =
    /// xoá lịch. Đổi lịch thì các buổi CHƯA ghi từ hôm nay trở đi được sinh lại.
    /// </summary>
    public List<RecorderScheduleSlot>? Schedule { get; set; }

    /// <summary>Ngày bắt đầu áp dụng lịch (giờ VN). null = hôm nay.</summary>
    public DateOnly? ScheduleFrom { get; set; }

    /// <summary>Ngày kết thúc lịch, tính cả ngày này. null = không giới hạn.</summary>
    public DateOnly? ScheduleUntil { get; set; }
}

/// <summary>Một khung giờ trong tuần (giờ Việt Nam).</summary>
public class RecorderScheduleSlot
{
    /// <summary>1 = Thứ 2 … 7 = Chủ nhật.</summary>
    [Range(1, 7)]
    public int DayOfWeek { get; set; }

    /// <summary>"HH:mm"</summary>
    [Required, RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$")]
    public string Start { get; set; } = null!;

    /// <summary>"HH:mm"</summary>
    [Required, RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$")]
    public string End { get; set; } = null!;
}

/// <summary>Gia sư tạo trước một buổi dạy cho học sinh ngoài nền tảng.</summary>
public class RecorderLessonCreateRequest
{
    [Required]
    public Guid StudentId { get; set; }

    public DateTime? ScheduledStart { get; set; }
    public DateTime? ScheduledEnd { get; set; }

    [StringLength(100)]
    public string? Subject { get; set; }
}

/// <summary>Gia sư báo nội dung AI tạo ra bị sai / không phù hợp.</summary>
public class RecorderAiFeedbackRequest
{
    /// <summary>wrong_content | wrong_student | inappropriate | other.</summary>
    [Required(ErrorMessage = "Chọn lý do")]
    [RegularExpression("^(wrong_content|wrong_student|inappropriate|other)$", ErrorMessage = "Lý do không hợp lệ")]
    public string Reason { get; set; } = null!;

    [StringLength(1000, ErrorMessage = "Mô tả tối đa 1000 ký tự")]
    public string? Note { get; set; }
}

/// <summary>Gia sư duyệt báo cáo (đã sửa) để gửi phụ huynh.</summary>
public class RecorderApproveRequest
{
    [Required(ErrorMessage = "Nội dung buổi học là bắt buộc")]
    [StringLength(2000)]
    public string LessonContent { get; set; } = null!;

    [StringLength(1000)]
    public string? Homework { get; set; }

    [StringLength(1000)]
    public string? TutorNotes { get; set; }

    // ── Bản tóm tắt ngắn gửi qua tin Zalo (tham số template tối đa 200 ký tự) ──
    // Bỏ trống thì dùng bản nháp AI (ZaloSummary).

    [StringLength(200)]
    public string? ZaloContent { get; set; }

    [StringLength(200)]
    public string? ZaloHomework { get; set; }

    [StringLength(200)]
    public string? ZaloNotes { get; set; }
}

/// <summary>
/// Mini App gửi lên khi phụ huynh bấm đồng ý. AccessToken lấy từ getAccessToken()
/// (backend xác minh qua Graph API); IdByOA lấy từ getUserInfo() sau khi followOA.
/// </summary>
public class RecorderParentLinkAcceptRequest
{
    [System.ComponentModel.DataAnnotations.Required]
    public string AccessToken { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required]
    public string IdByOA { get; set; } = string.Empty;

    /// <summary>Phiên bản nội dung đồng ý phụ huynh đã đọc.</summary>
    public string? ConsentVersion { get; set; }

    /// <summary>Phụ huynh đã tích đồng ý.</summary>
    public bool Agreed { get; set; }
}
