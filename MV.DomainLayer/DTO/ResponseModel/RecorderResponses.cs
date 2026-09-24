namespace MV.DomainLayer.DTO.ResponseModel;

public class RecorderStudentResponse
{
    public Guid StudentId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public short? Grade { get; set; }
    public string? Subject { get; set; }
    public string? ParentName { get; set; }
    public string? ParentPhone { get; set; }
    public string ConsentStatus { get; set; } = string.Empty;
    public string? ConsentVersion { get; set; }
    public DateTime? ConsentAt { get; set; }
    public string? Note { get; set; }
    public List<MV.DomainLayer.DTO.RequestModel.RecorderScheduleSlot> Schedule { get; set; } = new();
    public DateOnly? ScheduleFrom { get; set; }
    public DateOnly? ScheduleUntil { get; set; }
    public int LessonCount { get; set; }
    public DateTime? LastLessonAt { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>none | invited | linked | unfollowed — xem RecorderParentLinkStatus.</summary>
    public string ParentLinkStatus { get; set; } = "none";
    public DateTime? ParentLinkedAt { get; set; }
    /// <summary>Tên Zalo của phụ huynh đã liên kết (nếu có).</summary>
    public string? ParentZaloName { get; set; }
    /// <summary>Hạn của link mời đang chờ (khi ParentLinkStatus = invited).</summary>
    public DateTime? InviteExpiresAt { get; set; }
}

/// <summary>Một buổi trong nhật ký — dùng cho danh sách và chi tiết.</summary>
public class RecorderLessonResponse
{
    public Guid LessonId { get; set; }
    public Guid? StudentId { get; set; }
    public int? ClassSessionId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public short? Grade { get; set; }
    public string? Subject { get; set; }
    public DateTime? ScheduledStart { get; set; }
    public DateTime? ScheduledEnd { get; set; }
    public string Status { get; set; } = string.Empty;
    public string AiStatus { get; set; } = string.Empty;
    public int DurationSec { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? DeliveryChannel { get; set; }
    public string? DeliveryStatus { get; set; }
}

/// <summary>Link mời phụ huynh vừa tạo — app gia sư mở menu chia sẻ với ShareText.</summary>
public class RecorderParentInviteResponse
{
    public string InviteUrl { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string ShareText { get; set; } = string.Empty;
}

/// <summary>Thông tin Mini App hiển thị khi phụ huynh mở link mời (chưa đăng nhập).</summary>
public class RecorderParentInvitePreviewResponse
{
    /// <summary>valid | expired | used | revoked</summary>
    public string Status { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string TutorName { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public short? Grade { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string ConsentVersion { get; set; } = string.Empty;
    public string OaId { get; set; } = string.Empty;
}

public class RecorderParentLinkResultResponse
{
    public string StudentName { get; set; } = string.Empty;
    public string TutorName { get; set; } = string.Empty;
    public DateTime LinkedAt { get; set; }
}
