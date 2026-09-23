using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using MV.ApplicationLayer.ServiceInterfaces;
using MV.DomainLayer.Configuration;

namespace MV.ApplicationLayer.Services;

/// <summary>
/// Kho S3-compatible chứa các đoạn ghi âm từ app gia sư (Supabase Storage),
/// cấu hình ở section AppRecordingStorage. Luồng này KHÔNG dùng Agora: không
/// kênh RTC, không Cloud Recording, không dùng chung kho hay khoá với Agora.
/// </summary>
public class AppRecordingStorage(IOptions<AppRecordingStorageSettings> settings) : IAppRecordingStorage
{
    private readonly StorageTarget _s = StorageTarget.From(settings.Value);
    private readonly string _transcriptBucket =
        string.IsNullOrWhiteSpace(settings.Value.TranscriptBucket) ? settings.Value.Bucket : settings.Value.TranscriptBucket!;

    private sealed record StorageTarget(
        string StorageBucket,
        string StorageAccessKey,
        string StorageSecretKey,
        string? StorageEndpoint,
        string? StorageRegionName)
    {
        public static StorageTarget From(AppRecordingStorageSettings c) =>
            new(c.Bucket, c.AccessKey, c.SecretKey, c.Endpoint, c.Region);
    }

    public bool Enabled =>
        !string.IsNullOrWhiteSpace(_s.StorageBucket)
        && !string.IsNullOrWhiteSpace(_s.StorageAccessKey)
        && !string.IsNullOrWhiteSpace(_s.StorageSecretKey);

    /// <summary>app-recordings/{tutorId}/{lessonId}/ — gom theo gia sư, không theo buổi booking.</summary>
    public string BuildPrefix(string tutorId, Guid lessonId) =>
        $"app-recordings/{tutorId}/{lessonId:N}/";

    public string CreateUploadUrl(string prefix, int partNumber, TimeSpan lifetime)
    {
        using var client = CreateClient();
        return client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _s.StorageBucket,
            Key = PartKey(prefix, partNumber),
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.Add(lifetime),
            ContentType = "audio/mp4"
        });
    }

    public async Task<IReadOnlyList<AppRecordingPart>> ListPartsAsync(string prefix, CancellationToken ct = default)
    {
        using var client = CreateClient();

        var parts = new List<AppRecordingPart>();
        string? token = null;
        do
        {
            var res = await client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = _s.StorageBucket,
                Prefix = prefix,
                ContinuationToken = token
            }, ct);

            // AWSSDK v4 trả S3Objects = null (không phải list rỗng) khi prefix
            // chưa có object nào — tức mọi bản ghi chưa upload được đoạn nào.
            foreach (var o in res.S3Objects ?? [])
            {
                var number = ParsePartNumber(o.Key);
                // SDK mới trả Size là long? — object rỗng vẫn là object.
                if (number != null) parts.Add(new AppRecordingPart(number.Value, o.Key, o.Size ?? 0));
            }

            token = res.IsTruncated == true ? res.NextContinuationToken : null;
        } while (token != null);

        // Sắp theo SỐ phần, không theo tên: "part-10" đứng trước "part-9" nếu so
        // chuỗi, và ghép sai thứ tự thì buổi học kể chuyện lộn xộn.
        parts.Sort((a, b) => a.PartNumber.CompareTo(b.PartNumber));
        return parts;
    }

    public async Task<Stream> OpenPartAsync(string key, CancellationToken ct = default)
    {
        var client = CreateClient();
        var res = await client.GetObjectAsync(_s.StorageBucket, key, ct);
        // Không dispose client ở đây: stream trả ra còn sống, đóng client là
        // đóng luôn kết nối đang đọc dở.
        return new S3ObjectStream(res, client);
    }

    public async Task UploadAsync(string key, string filePath, string contentType, CancellationToken ct = default)
    {
        using var client = CreateClient();
        await client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _s.StorageBucket,
            Key = key,
            FilePath = filePath,
            ContentType = contentType
        }, ct);
    }

    public string CreateDownloadUrl(string key, TimeSpan lifetime)
    {
        using var client = CreateClient();
        return client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _s.StorageBucket,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(lifetime)
        });
    }

    public async Task<int> DeletePrefixAsync(string prefix, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(prefix)) return 0;
        using var client = CreateClient();
        var deleted = 0;
        string? token = null;
        do
        {
            var res = await client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = _s.StorageBucket,
                Prefix = prefix,
                ContinuationToken = token
            }, ct);
            // Xoá từng object: DeleteObjects (xoá hàng loạt) cần Content-MD5, không phải
            // kho S3-compatible nào cũng hỗ trợ giống AWS.
            foreach (var o in res.S3Objects ?? [])
            {
                await client.DeleteObjectAsync(_s.StorageBucket, o.Key, ct);
                deleted++;
            }
            token = res.IsTruncated == true ? res.NextContinuationToken : null;
        } while (token != null);
        return deleted;
    }

    public string BuildTranscriptKey(Guid lessonId, DateTime utcDate) =>
        $"lesson-transcripts/{utcDate:yyyy}/{utcDate:MM}/{lessonId:N}.json";

    public async Task PutTranscriptAsync(string key, string json, CancellationToken ct = default)
    {
        using var client = CreateClient();
        await client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _transcriptBucket,
            Key = key,
            ContentBody = json,
            ContentType = "application/json; charset=utf-8"
        }, ct);
    }

    public async Task<string?> GetTranscriptAsync(string key, CancellationToken ct = default)
    {
        using var client = CreateClient();
        try
        {
            using var res = await client.GetObjectAsync(_transcriptBucket, key, ct);
            using var reader = new StreamReader(res.ResponseStream);
            return await reader.ReadToEndAsync(ct);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task DeleteTranscriptAsync(string key, CancellationToken ct = default)
    {
        using var client = CreateClient();
        await client.DeleteObjectAsync(_transcriptBucket, key, ct);
    }

    /// <summary>part-0007.m4a → 7. Trả null cho mọi thứ không phải một đoạn.</summary>
    private static int? ParsePartNumber(string key)
    {
        var name = key[(key.LastIndexOf('/') + 1)..];
        if (!name.StartsWith("part-", StringComparison.Ordinal)) return null;
        if (!name.EndsWith(".m4a", StringComparison.OrdinalIgnoreCase)) return null;
        var digits = name[5..^4];
        return int.TryParse(digits, out var n) ? n : null;
    }

    private static string PartKey(string prefix, int partNumber) =>
        $"{prefix}part-{partNumber:D4}.m4a";

    private IAmazonS3 CreateClient()
    {
        var creds = new BasicAWSCredentials(_s.StorageAccessKey, _s.StorageSecretKey);

        if (!string.IsNullOrWhiteSpace(_s.StorageEndpoint))
        {
            var serviceUrl = _s.StorageEndpoint!.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? _s.StorageEndpoint!
                : $"https://{_s.StorageEndpoint}";
            return new AmazonS3Client(creds, new AmazonS3Config
            {
                ServiceURL = serviceUrl,
                ForcePathStyle = true,
                AuthenticationRegion = string.IsNullOrWhiteSpace(_s.StorageRegionName)
                    ? "us-west-004"
                    : _s.StorageRegionName
            });
        }

        var regionName = string.IsNullOrWhiteSpace(_s.StorageRegionName)
            ? "ap-southeast-1"
            : _s.StorageRegionName;
        return new AmazonS3Client(creds, RegionEndpoint.GetBySystemName(regionName));
    }

    /// <summary>Giữ client sống đúng bằng vòng đời của stream đang đọc.</summary>
    private sealed class S3ObjectStream(GetObjectResponse response, IAmazonS3 client) : Stream
    {
        private readonly Stream _inner = response.ResponseStream;

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => response.ContentLength;
        public override long Position
        {
            get => _inner.Position;
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct) =>
            _inner.ReadAsync(buffer, offset, count, ct);

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default) =>
            _inner.ReadAsync(buffer, ct);

        public override void Flush() => _inner.Flush();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
                response.Dispose();
                client.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
