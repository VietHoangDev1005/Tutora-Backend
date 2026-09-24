using MV.ApplicationLayer.ServiceInterfaces;

namespace MV.ApplicationLayer.Tests;

/// <summary>Kho ghi âm giả cho test: ghi lại prefix / key đã xoá, không gọi mạng.</summary>
internal sealed class FakeStorage : IAppRecordingStorage
{
    public bool Enabled { get; set; } = true;
    public List<string> DeletedPrefixes { get; } = [];
    public List<string> DeletedTranscripts { get; } = [];

    public string BuildPrefix(string tutorId, Guid lessonId) => $"app/{tutorId}/{lessonId}";
    public string CreateUploadUrl(string prefix, int partNumber, TimeSpan lifetime) => $"https://fake/{prefix}/{partNumber}";
    public Task<IReadOnlyList<AppRecordingPart>> ListPartsAsync(string prefix, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AppRecordingPart>>([]);
    public Task<Stream> OpenPartAsync(string key, CancellationToken ct = default) => Task.FromResult<Stream>(new MemoryStream());
    public Task UploadAsync(string key, string filePath, string contentType, CancellationToken ct = default) => Task.CompletedTask;
    public string CreateDownloadUrl(string key, TimeSpan lifetime) => $"https://fake/{key}";
    public Task<int> DeletePrefixAsync(string prefix, CancellationToken ct = default)
    {
        DeletedPrefixes.Add(prefix);
        return Task.FromResult(1);
    }
    public string BuildTranscriptKey(Guid lessonId, DateTime utcDate) => $"lesson-transcripts/{lessonId}.json";
    public Task PutTranscriptAsync(string key, string json, CancellationToken ct = default) => Task.CompletedTask;
    public Task<string?> GetTranscriptAsync(string key, CancellationToken ct = default) => Task.FromResult<string?>(null);
    public Task DeleteTranscriptAsync(string key, CancellationToken ct = default)
    {
        DeletedTranscripts.Add(key);
        return Task.CompletedTask;
    }
}
