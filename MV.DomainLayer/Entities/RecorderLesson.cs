namespace MV.DomainLayer.Entities;

/// <summary>
/// Một buổi dạy trong nhật ký của gia sư (schema <c>recorder</c>): lịch, bản
/// ghi âm, bản nháp AI, báo cáo đã duyệt và trạng thái gửi phụ huynh.
///
/// Thuộc về đúng MỘT trong hai: <see cref="Studentid"/> (học sinh ngoài nền
/// tảng) hoặc <see cref="Classsessionid"/> (buổi có booking). Không bao giờ
/// tạo dòng trong class_sessions cho buổi ngoài nền tảng.
/// </summary>
public class RecorderLesson
{
    public Guid Lessonid { get; set; }
    public string Tutorid { get; set; } = null!;
    public Guid? Studentid { get; set; }
    public int? Classsessionid { get; set; }

    public DateTime? Scheduledstart { get; set; }
    public DateTime? Scheduledend { get; set; }
    public string? Subject { get; set; }

    /// <summary>Xem <see cref="Constants.SessionRecordingStatus"/>.</summary>
    public string Status { get; set; } = null!;

    public DateTime? Startedat { get; set; }
    public DateTime? Endedat { get; set; }
    public int Durationsec { get; set; }
    public long Bytes { get; set; }
    public int Partcount { get; set; }
    public string? Storagekey { get; set; }
    public string? Consentsnapshot { get; set; }

    /// <summary>Xem <see cref="Constants.RecorderAiStatus"/>.</summary>
    public string Aistatus { get; set; } = null!;
    public string? Airesult { get; set; }
    public string? Aierror { get; set; }
    public string? Geminifilename { get; set; }
    public string? Geminifileuri { get; set; }
    public DateTime? Geminifileexpiresat { get; set; }

    public string? Reportcontent { get; set; }
    public string? Reporthomework { get; set; }
    public string? Reportnotes { get; set; }
    public DateTime? Approvedat { get; set; }
    public string? Deliverychannel { get; set; }
    public string? Deliverystatus { get; set; }
    public string? Deliveryerror { get; set; }
    public DateTime? Sentat { get; set; }

    /// <summary>Lời thoại có mốc thời gian: mỗi dòng "[mm:ss] Gia sư: …".</summary>
    public string? Transcript { get; set; }

    /// <summary>Xem <see cref="Constants.RecorderAiStatus"/> (dùng chung bộ giá trị).</summary>
    public string Transcriptstatus { get; set; } = "none";
    public string? Transcripterror { get; set; }

    /// <summary>Key file JSON transcript (đã ẩn danh) trên kho — nguyên liệu thô, gia sư không xem.</summary>
    public string? Transcriptkey { get; set; }
    public string? Transcriptmodel { get; set; }

    /// <summary>Batch Gemini đang chép lời buổi này (batches/...).</summary>
    public string? Transcriptbatch { get; set; }
    public DateTime? Transcriptqueuedat { get; set; }

    /// <summary>Key của file đã ghép (merged.m4a) trên kho — để nghe lại.</summary>
    public string? Audiokey { get; set; }

    /// <summary>File âm thanh đã bị xoá theo hạn lưu trữ.</summary>
    public DateTime? Audiodeletedat { get; set; }

    public string? Errormessage { get; set; }
    public DateTime Createdat { get; set; }
    public DateTime Updatedat { get; set; }

    public virtual RecorderStudent? Student { get; set; }
    public virtual ClassSession? ClassSession { get; set; }
}
