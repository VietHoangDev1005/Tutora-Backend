namespace MV.DomainLayer.Configuration;

/// <summary>
/// Tài khoản gia sư dành cho người duyệt app của Google Play (App access → test login).
///
///   DemoTutorUserIds : userid các gia sư demo. Báo cáo của học sinh ngoài nền tảng do các
///                      tài khoản này duyệt được đánh dấu "đã gửi" ngay, KHÔNG gửi Zalo thật —
///                      người duyệt ở ngoài Việt Nam, không có Zalo, và số phụ huynh demo không
///                      được nhận tin. Mọi bước khác (ghi âm, upload, AI) vẫn chạy như thật.
///
/// Cấu hình qua biến môi trường: AppReview__DemoTutorUserIds__0=&lt;userid&gt;
/// </summary>
public class AppReviewSettings
{
    public const string SectionName = "AppReview";

    public List<string> DemoTutorUserIds { get; set; } = [];

    public bool IsDemoTutor(string? userId) =>
        !string.IsNullOrEmpty(userId) && DemoTutorUserIds.Contains(userId, StringComparer.Ordinal);
}
