using System;

namespace MV.DomainLayer.Entities;

/// <summary>
/// Bằng chứng người dùng đã đồng ý một văn bản pháp lý (policy_documents.slug) ở đúng phiên bản
/// đang xuất bản lúc đăng ký. Chỉ thêm, không sửa; giữ lại cả khi tài khoản đã tự xoá.
/// </summary>
public partial class UserPolicyAcceptance
{
    public long Id { get; set; }

    public string Userid { get; set; } = null!;

    public string Policyslug { get; set; } = null!;

    /// <summary>policy_documents.version lúc đồng ý, hoặc "unpublished".</summary>
    public string Policyversion { get; set; } = null!;

    public DateTime Acceptedat { get; set; }

    /// <summary>mobile | web.</summary>
    public string Source { get; set; } = null!;

    public string? Ipaddress { get; set; }
}
