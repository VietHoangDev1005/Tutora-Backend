using System.ComponentModel.DataAnnotations;
using MV.DomainLayer.Constants;

namespace MV.DomainLayer.DTO.RequestModel
{
    /// <summary>
    /// Request đơn giản cho register (không qua Supabase)
    /// </summary>
    public class SimpleRegisterRequest
    {
        /// <summary>
        /// Email
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Số điện thoại (tùy chọn)
        /// </summary>
        public string? Phone { get; set; }

        /// <summary>
        /// Mật khẩu — tối thiểu 8 ký tự, cùng quy định với đặt lại / đổi mật khẩu.
        /// </summary>
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự.")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Họ tên đầy đủ
        /// </summary>
        [Required(ErrorMessage = "Họ tên không được để trống.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ tên phải từ 2 đến 100 ký tự.")]
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// Role: Student, Tutor, Parent (mặc định là Student)
        /// </summary>
        public string Role { get; set; } = UserRole.Student;

        /// <summary>
        /// SĐT phụ huynh để nhận ZNS theo dõi.
        /// </summary>
        public string? ParentPhone { get; set; }

        /// <summary>
        /// Người dùng đã tick đồng ý Điều khoản sử dụng (policy_documents slug "terms").
        /// Tuỳ chọn — web chưa gửi. Bắt buộc true khi Role = Tutor và Source = "mobile".
        /// </summary>
        public bool? AcceptedTerms { get; set; }

        /// <summary>
        /// Người dùng đã tick đồng ý Chính sách quyền riêng tư (policy_documents slug "privacy").
        /// Tuỳ chọn — web chưa gửi. Bắt buộc true khi Role = Tutor và Source = "mobile".
        /// </summary>
        public bool? AcceptedPrivacy { get; set; }

        /// <summary>Nơi đăng ký: "mobile" | "web" (mặc định "web").</summary>
        public string? Source { get; set; }
    }
}
