using System.ComponentModel.DataAnnotations;

namespace MV.DomainLayer.DTO.RequestModel;

/// <summary>Body của POST /api/users/me/delete-account.</summary>
public class DeleteAccountRequest
{
    /// <summary>
    /// Mật khẩu hiện tại. Bắt buộc với tài khoản đăng ký bằng SĐT/email + mật khẩu; bỏ qua được
    /// với tài khoản tạo qua Zalo (không có mật khẩu do người dùng đặt).
    /// </summary>
    public string? Password { get; set; }

    /// <summary>Lý do xoá (tuỳ chọn, tối đa 500 ký tự).</summary>
    [StringLength(500, ErrorMessage = "Lý do tối đa 500 ký tự.")]
    public string? Reason { get; set; }
}
