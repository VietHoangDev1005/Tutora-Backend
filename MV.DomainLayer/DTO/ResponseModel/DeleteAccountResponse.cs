namespace MV.DomainLayer.DTO.ResponseModel;

/// <summary>Kết quả POST /api/users/me/delete-account.</summary>
public class DeleteAccountResponse
{
    public string UserId { get; set; } = string.Empty;

    /// <summary>Thời điểm xoá (UTC).</summary>
    public DateTime DeletedAt { get; set; }

    /// <summary>Sau thời điểm này (UTC) dữ liệu cá nhân + file ghi âm sẽ bị dọn vĩnh viễn.</summary>
    public DateTime PurgeScheduledAt { get; set; }

    public string Message { get; set; } = string.Empty;
}
