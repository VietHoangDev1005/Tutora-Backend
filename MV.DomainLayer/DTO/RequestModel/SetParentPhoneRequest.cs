using System.ComponentModel.DataAnnotations;

namespace MV.DomainLayer.DTO.RequestModel;

/// <summary>
/// Học sinh tự đăng ký nhập/cập nhật SĐT phụ huynh (tùy chọn) để nhận ZNS theo dõi.
/// Có thể check thêm số điện thoại đã được sử dụng.
/// </summary>
public class SetParentPhoneRequest
{
    [RegularExpression(MV.DomainLayer.Helpers.PhoneNumberHelper.VietnamPhonePattern, ErrorMessage = "Số điện thoại phụ huynh không hợp lệ.")]
    public string? ParentPhone { get; set; }
}
