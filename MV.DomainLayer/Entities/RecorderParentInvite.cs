namespace MV.DomainLayer.Entities;

/// <summary>Link mời phụ huynh liên kết Zalo — token dùng một lần, có hạn.</summary>
public class RecorderParentInvite
{
    public Guid Inviteid { get; set; }
    public Guid Studentid { get; set; }
    public string Tutorid { get; set; } = null!;
    public string Token { get; set; } = null!;
    public DateTime Createdat { get; set; }
    public DateTime Expiresat { get; set; }
    public DateTime? Usedat { get; set; }
    public Guid? Usedbyparentid { get; set; }
    public DateTime? Revokedat { get; set; }
}
