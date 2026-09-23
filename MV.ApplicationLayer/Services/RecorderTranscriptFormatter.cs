using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using MV.DomainLayer.Entities;

namespace MV.ApplicationLayer.Services;

/// <summary>
/// Biến văn bản chép lời của Gemini ("[mm:ss] Gia sư: …" mỗi dòng) thành file JSON có cấu trúc,
/// đã ẩn danh — định dạng "nguyên liệu thô" lưu trên kho. Giữ mốc thời gian + người nói từng
/// lượt để sau này dùng lại được (hỏi đáp trích "phút 12:30", phân tích, tạo chỉ mục tìm kiếm)
/// mà không phải chép lời lại.
/// </summary>
public static partial class RecorderTranscriptFormatter
{
    public const string PromptVersion = "lesson-transcript-v1";

    public sealed record Segment(int Start, string Speaker, string Text);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        // Giữ nguyên tiếng Việt có dấu trong file (dễ đọc, nhỏ hơn) thay vì \uXXXX.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false
    };

    [GeneratedRegex(@"^\[(\d{1,3}):(\d{2})\]\s*([^:]{1,20}):\s*(.*)$")]
    private static partial Regex LineRegex();

    [GeneratedRegex(@"(?<!\d)(?:\+?84|0)\d{9,10}(?!\d)")]
    private static partial Regex PhoneRegex();

    /// <summary>Tách từng lượt nói. Dòng không đúng định dạng được nối vào lượt trước.</summary>
    public static List<Segment> Parse(string text)
    {
        var result = new List<Segment>();
        foreach (var raw in text.Replace("\r", "").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0) continue;

            var m = LineRegex().Match(line);
            if (m.Success)
            {
                var start = int.Parse(m.Groups[1].Value) * 60 + int.Parse(m.Groups[2].Value);
                result.Add(new Segment(start, MapSpeaker(m.Groups[3].Value), m.Groups[4].Value.Trim()));
            }
            else if (result.Count > 0)
            {
                var last = result[^1];
                result[^1] = last with { Text = $"{last.Text} {line}" };
            }
            else
            {
                result.Add(new Segment(0, "unknown", line));
            }
        }
        return result.Where(s => s.Text.Length > 0).ToList();
    }

    private static string MapSpeaker(string label) => label.Trim().ToLowerInvariant() switch
    {
        "gia sư" => "tutor",
        "học sinh" => "student",
        _ => "unknown"
    };

    /// <summary>
    /// Lớp ẩn danh thứ hai (ngoài yêu cầu trong prompt): thay tên thật đã biết của học sinh, gia sư,
    /// phụ huynh và mọi số điện thoại. Chỉ thay đúng từ viết hoa đứng riêng để không phá câu thường
    /// (vd tên "An" không đụng tới "an toàn").
    /// </summary>
    public static List<Segment> Anonymize(List<Segment> segments, string? studentName, string? tutorName, string? parentName)
    {
        var replacements = new List<(Regex Pattern, string Tag)>();
        void Add(string? fullName, string tag)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return;
            var name = fullName.Trim();
            // Tên đầy đủ trước, rồi tên gọi (từ cuối) — tên gọi là cách hay dùng nhất khi nói chuyện.
            replacements.Add((new Regex($@"(?<!\p{{L}}){Regex.Escape(name)}(?!\p{{L}})"), tag));
            var given = name.Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
            if (given is { Length: >= 2 } && char.IsUpper(given[0]))
                replacements.Add((new Regex($@"(?<!\p{{L}}){Regex.Escape(given)}(?!\p{{L}})"), tag));
        }
        Add(studentName, "[HỌC SINH]");
        Add(tutorName, "[GIA SƯ]");
        Add(parentName, "[PHỤ HUYNH]");

        return segments.Select(s =>
        {
            var t = PhoneRegex().Replace(s.Text, "[SĐT]");
            foreach (var (pattern, tag) in replacements) t = pattern.Replace(t, tag);
            return s with { Text = t };
        }).ToList();
    }

    public static string BuildJson(
        RecorderLesson lesson, RecorderStudent? student, List<Segment> segments, string model, DateTime createdAtUtc)
    {
        var doc = new
        {
            schemaVersion = 1,
            lessonId = lesson.Lessonid,
            source = lesson.Classsessionid != null ? "booking" : "offplatform",
            subject = lesson.Subject ?? student?.Subject,
            grade = student?.Grade,
            durationSec = lesson.Durationsec,
            recordedAt = lesson.Startedat,
            language = "vi",
            model,
            promptVersion = PromptVersion,
            consentStatus = student?.Consentstatus,
            anonymized = true,
            createdAt = createdAtUtc,
            segments = segments.Select(s => new { start = s.Start, speaker = s.Speaker, text = s.Text })
        };
        return JsonSerializer.Serialize(doc, JsonOptions);
    }
}
