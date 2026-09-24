namespace MV.DomainLayer.Constants;

/// <summary>Người dùng tự xoá tài khoản (POST /api/users/me/delete-account).</summary>
public static class AccountDeletion
{
    /// <summary>Sau bao nhiêu ngày kể từ lúc xoá thì job dọn dữ liệu cá nhân + file ghi âm.</summary>
    public const int PurgeAfterDays = 30;

    /// <summary>users.deletion_source của tài khoản tự xoá — job dọn chỉ xử lý giá trị này.</summary>
    public const string SourceSelf = "self";

    /// <summary>Tên hiển thị thay cho họ tên thật sau khi dọn.</summary>
    public const string AnonymizedFullName = "Người dùng đã xoá";

    /// <summary>Lỗi trả về khi tài khoản đã xoá cố đăng nhập / refresh.</summary>
    public const string DeletedMessage = "Tài khoản đã bị xoá.";

    /// <summary>Lý do ghi vào recorder.lessons.delivery_error cho báo cáo chưa gửi.</summary>
    public const string TutorDeletedDeliveryError = "Tài khoản gia sư đã bị xoá.";

    public const int MaxReasonLength = 500;
}

/// <summary>Slug văn bản pháp lý trong policy_documents mà người dùng phải đồng ý khi đăng ký.</summary>
public static class PolicySlugs
{
    public const string Terms = "terms";
    public const string Privacy = "privacy";

    /// <summary>Chính sách quyền riêng tư riêng của app gia sư (V20261002) — đăng ký từ app ghi slug này.</summary>
    public const string PrivacyApp = "privacy-app";

    /// <summary>Không có bản published nào của slug lúc đăng ký — vẫn lưu dòng đồng ý.</summary>
    public const string UnpublishedVersion = "unpublished";
}

/// <summary>Nguồn đồng ý (user_policy_acceptances.source).</summary>
public static class PolicyAcceptanceSource
{
    public const string Mobile = "mobile";
    public const string Web = "web";
}
