namespace MV.DomainLayer.DTO.ResponseModel;

/// <summary>File đã upload lên Gemini File API — Name dùng để poll trạng thái, Uri dùng để tham chiếu trong generateContent.</summary>
public sealed record GeminiUploadedFile(string Name, string Uri);

/// <summary>Một lượt hội thoại dùng để dựng lại context cho follow-up chat. Role: "user" | "assistant".</summary>
public sealed record GeminiChatTurn(string Role, string Content);

/// <summary>Kết quả AI tự động điền báo cáo buổi học cho gia sư — khớp 3 field free-text của <c>ClassSessionReport</c>.</summary>
public sealed class TutorReportAiFillResult
{
    public string LessonContent { get; set; } = string.Empty;
    public string Homework { get; set; } = string.Empty;
    public string TutorNotes { get; set; } = string.Empty;

    /// <summary>
    /// Biên bản buổi học dành riêng cho GIA SƯ (không gửi phụ huynh) — thay cho việc xem lại toàn bộ
    /// lời thoại/audio. Sinh chung trong cùng lượt gọi Gemini với báo cáo để không tốn thêm một lượt
    /// nghe audio. Nullable: bản nháp cũ (trước khi có field này) hoặc model bỏ sót thì là null.
    /// </summary>
    public TutorSessionMinutes? SessionMinutes { get; set; }

    /// <summary>
    /// Bản tóm tắt NGẮN gửi phụ huynh qua tin Zalo (ZBS template: giá trị dòng bảng tối đa 90 ký tự).
    /// Sinh chung lượt gọi Gemini với báo cáo đầy đủ. Nullable: bản nháp cũ hoặc model bỏ sót thì là
    /// null — job gửi Zalo sẽ fallback về bản đầy đủ bị cắt ngắn.
    /// </summary>
    public TutorZaloSummary? ZaloSummary { get; set; }
}

/// <summary>Tóm tắt báo cáo cho tin Zalo: mỗi field một câu hoàn chỉnh, văn bản thuần, ≤ 90 ký tự.</summary>
public sealed class TutorZaloSummary
{
    public string? Content { get; set; }
    public string? Homework { get; set; }
    public string? Notes { get; set; }
}

/// <summary>Biên bản buổi học cho gia sư: tóm tắt ngắn, ý chính đã dạy, việc cần nhớ cho buổi sau.</summary>
public sealed class TutorSessionMinutes
{
    /// <summary>2–4 câu tóm tắt buổi học.</summary>
    public string? Summary { get; set; }

    /// <summary>3–6 ý chính đã dạy/thảo luận.</summary>
    public List<string> KeyPoints { get; set; } = new();

    /// <summary>0–5 việc gia sư cần làm/nhớ cho buổi sau.</summary>
    public List<string> FollowUps { get; set; } = new();
}

/// <summary>Một buổi trong batch chép lời: Key để khớp kết quả trả về (lessonId).</summary>
public sealed record GeminiBatchAudioItem(string Key, string FileUri, string MimeType);

public enum GeminiBatchState { Running, Succeeded, Failed, Cancelled, Expired }

/// <summary>Kết quả một yêu cầu trong batch. Text null = yêu cầu đó lỗi (xem Error).</summary>
public sealed record GeminiBatchItemResult(string? Key, string? Text, string? Error);

public sealed record GeminiBatchStatus(GeminiBatchState State, IReadOnlyList<GeminiBatchItemResult> Items, string? RawState);
