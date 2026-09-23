namespace MV.DomainLayer.Configuration;

/// <summary>
/// Kho chứa file ghi âm buổi dạy TẠI NHÀ (app gia sư). Độc lập hoàn toàn với
/// Agora — để trống thì endpoint ghi âm báo "Kho lưu trữ bản ghi chưa được cấu hình".
///
/// Bất kỳ kho S3-compatible nào hỗ trợ presigned PUT (SigV4) đều dùng được.
/// Supabase Storage:
///   Endpoint  = https://&lt;project-ref&gt;.storage.supabase.co/storage/v1/s3
///   Region    = region của project (vd. ap-southeast-1)
///   AccessKey / SecretKey = Storage → S3 Connection → New access key
///   Bucket    = bucket PRIVATE tạo sẵn trong Storage
/// </summary>
public class AppRecordingStorageSettings
{
    public const string SectionName = "AppRecordingStorage";

    public string Bucket { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string? Endpoint { get; set; }
    public string Region { get; set; } = "ap-southeast-1";

    /// <summary>Bucket riêng (private) cho transcript thô dạng JSON. Để trống = dùng chung Bucket,
    /// file nằm dưới prefix lesson-transcripts/ (job xoá audio sau 30 ngày không đụng tới).</summary>
    public string? TranscriptBucket { get; set; }
}
