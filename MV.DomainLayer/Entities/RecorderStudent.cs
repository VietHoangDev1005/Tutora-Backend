namespace MV.DomainLayer.Entities;

/// <summary>
/// Học sinh trong danh bạ riêng của gia sư (schema <c>recorder</c>) — dùng cho
/// học sinh NGOÀI nền tảng, không có booking. Không phải tài khoản: không tạo
/// user Parent/Student nào, SĐT phụ huynh chỉ để gửi báo cáo.
/// </summary>
public class RecorderStudent
{
    public Guid Studentid { get; set; }
    public string Tutorid { get; set; } = null!;
    public string Fullname { get; set; } = null!;
    public short? Grade { get; set; }
    public string? Subject { get; set; }
    public string? Parentname { get; set; }
    public string? Parentphone { get; set; }

    /// <summary>Xem <see cref="Constants.RecorderConsentStatus"/>.</summary>
    public string Consentstatus { get; set; } = null!;
    public DateTime? Consentat { get; set; }
    public string? Consentversion { get; set; }
    public string? Note { get; set; }

    /// <summary>Thời khoá biểu hằng tuần (jsonb) — xem RecorderScheduleSlot.</summary>
    public string? Schedule { get; set; }

    /// <summary>Ngày bắt đầu áp dụng lịch hiện tại (giờ VN).</summary>
    public DateOnly? Schedulefrom { get; set; }

    /// <summary>Ngày kết thúc lịch (giờ VN, tính cả ngày này). null = không giới hạn.</summary>
    public DateOnly? Scheduleuntil { get; set; }

    /// <summary>Nối với tài khoản học sinh thật khi phụ huynh đặt booking trên Tutora.</summary>
    public string? Linkedstudentuserid { get; set; }

    /// <summary>Phụ huynh đã tự liên kết qua Zalo Mini App (xem RecorderParent).</summary>
    public Guid? Parentid { get; set; }
    public DateTime? Parentlinkedat { get; set; }
    public DateTime? Archivedat { get; set; }
    public DateTime Createdat { get; set; }
    public DateTime Updatedat { get; set; }
}
