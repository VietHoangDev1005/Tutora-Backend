namespace MV.DomainLayer.Helpers;

/// <summary>
/// Chuẩn hoá số điện thoại Việt Nam về MỘT dạng duy nhất để lưu DB: <c>+84xxxxxxxxx</c>.
/// Người dùng được nhập tự do (0…, 84…, +84…, có dấu cách / chấm / gạch); mọi điểm nhận SĐT
/// ở backend (đăng ký, đăng nhập, OTP, quên mật khẩu, admin, SĐT phụ huynh…) đều đi qua đây
/// trước khi lưu hoặc tra cứu.
/// </summary>
public static class PhoneNumberHelper
{
    /// <summary>
    /// Pattern cho [RegularExpression] ở DTO: nhận 0… / 84… / +84… (9–10 chữ số sau mã vùng),
    /// cho phép dấu cách, chấm, gạch giữa các số. Service chuẩn hoá về +84… trước khi lưu.
    /// </summary>
    public const string VietnamPhonePattern = @"^\s*(\+?84|0)([\s.\-]?\d){9,10}\s*$";

    /// <summary>
    /// Trả về <c>+84xxxxxxxxx</c>. Chuỗi rỗng → null. Chuỗi không nhận ra là SĐT Việt Nam thì
    /// trả lại nguyên văn (đã bỏ khoảng trắng) để tầng validation báo lỗi, không đoán bừa.
    /// </summary>
    public static string? ToE164(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return null;
        var p = new string(phone.Where(c => !char.IsWhiteSpace(c) && c is not '.' and not '-' and not '(' and not ')').ToArray());

        string? national = null;
        if (p.StartsWith("+84", StringComparison.Ordinal)) national = p[3..];
        else if (p.StartsWith("84", StringComparison.Ordinal) && p.Length >= 11) national = p[2..];
        else if (p.StartsWith('0')) national = p[1..];

        return national is { Length: 9 or 10 } && national.All(char.IsAsciiDigit)
            ? "+84" + national
            : p;
    }

    /// <summary>Đúng định dạng SĐT Việt Nam (sau khi chuẩn hoá).</summary>
    public static bool IsValidVietnamPhone(string? phone)
    {
        var e164 = ToE164(phone);
        return e164 is { Length: 12 or 13 } && e164.StartsWith("+84", StringComparison.Ordinal)
            && e164[3..].All(char.IsAsciiDigit);
    }

    /// <summary>
    /// Mọi cách viết cũ của cùng một số (+84…, 84…, 0…) — dùng khi tra cứu để vẫn khớp dữ liệu
    /// lưu trước khi chạy migration chuẩn hoá (V20261005__normalize_phone_e164).
    /// </summary>
    public static string[] LookupVariants(string? phone)
    {
        var e164 = ToE164(phone);
        if (e164 == null) return [];
        if (!IsValidVietnamPhone(e164)) return [e164];
        var national = e164[3..];
        return [e164, "84" + national, "0" + national];
    }

    /// <summary>
    /// Từ khoá tìm kiếm (admin gõ "0901…" hay "+84901…") → phần số sau mã vùng, để
    /// <c>Contains</c> khớp được với SĐT đã lưu dạng +84. Không phải SĐT thì trả nguyên văn.
    /// </summary>
    public static string SearchFragment(string term)
    {
        if (!LooksLikePhone(term)) return term;
        var p = new string(term.Where(char.IsAsciiDigit).ToArray());
        if (term.TrimStart().StartsWith('+') && p.StartsWith("84", StringComparison.Ordinal)) return p[2..];
        if (p.StartsWith("84", StringComparison.Ordinal) && p.Length >= 11) return p[2..];
        if (p.StartsWith('0')) return p[1..];
        return p;
    }

    /// <summary>Chuỗi đầu vào của ô "Email hoặc SĐT" có phải SĐT không.</summary>
    public static bool LooksLikePhone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Contains('@')) return false;
        var p = value.Trim();
        return (p.StartsWith('+') || char.IsAsciiDigit(p[0]))
            && p.All(c => char.IsAsciiDigit(c) || c is '+' or ' ' or '.' or '-');
    }
}
