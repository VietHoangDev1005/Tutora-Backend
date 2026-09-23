namespace MV.ApplicationLayer.ServiceInterfaces;

/// <summary>
/// Kho chứa các đoạn ghi âm gửi từ app gia sư.
///
/// Tách sau interface vì hai lý do: bucket có thể đổi nhà cung cấp (S3 ↔ R2)
/// mà không đụng controller, và nếu sau này tách service lưu trữ sang
/// TypeScript thì chỉ cần thay implementation.
/// </summary>
public interface IAppRecordingStorage
{
    bool Enabled { get; }

    /// <summary>Prefix của một bản ghi, vd app-recordings/1234/{recordingId}/</summary>
    string BuildPrefix(string tutorId, Guid lessonId);

    /// <summary>
    /// URL presigned để app PUT thẳng một đoạn lên kho, không đi qua backend.
    /// App upload 22 MB mỗi buổi; cho nó đi vòng qua server chỉ tốn băng thông
    /// và biến backend thành nút thắt.
    /// </summary>
    string CreateUploadUrl(string prefix, int partNumber, TimeSpan lifetime);

    /// <summary>Liệt kê các đoạn đã có thật trên kho, theo đúng thứ tự phần.</summary>
    Task<IReadOnlyList<AppRecordingPart>> ListPartsAsync(string prefix, CancellationToken ct = default);

    /// <summary>Mở luồng đọc một đoạn để đẩy lên Gemini.</summary>
    Task<Stream> OpenPartAsync(string key, CancellationToken ct = default);

    /// <summary>Tải một file (vd merged.m4a) lên kho.</summary>
    Task UploadAsync(string key, string filePath, string contentType, CancellationToken ct = default);

    /// <summary>URL presigned để app GET (nghe lại) một file.</summary>
    string CreateDownloadUrl(string key, TimeSpan lifetime);

    /// <summary>Xoá mọi object dưới prefix (hết hạn lưu trữ). Trả về số object đã xoá.</summary>
    Task<int> DeletePrefixAsync(string prefix, CancellationToken ct = default);

    // ── Transcript thô (JSON) — bucket riêng nếu cấu hình TranscriptBucket ──

    /// <summary>lesson-transcripts/{yyyy}/{MM}/{lessonId}.json — không chứa id gia sư (ẩn danh).</summary>
    string BuildTranscriptKey(Guid lessonId, DateTime utcDate);

    Task PutTranscriptAsync(string key, string json, CancellationToken ct = default);

    /// <summary>Đọc lại transcript (vd cho tính năng hỏi đáp sau này). Null nếu không có.</summary>
    Task<string?> GetTranscriptAsync(string key, CancellationToken ct = default);

    Task DeleteTranscriptAsync(string key, CancellationToken ct = default);
}

public record AppRecordingPart(int PartNumber, string Key, long Bytes);
