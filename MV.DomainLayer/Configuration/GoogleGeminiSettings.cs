namespace MV.DomainLayer.Configuration
{
    public class GoogleGeminiSettings
    {
        public const string SectionName = "GoogleGemini";

        public string ApiKey { get; set; } = string.Empty;
        // gemini-pro không hỗ trợ video. gemini-2.5-flash đã bị Google chặn cấp cho API key mới
        // (deprecation chính thức 2026-10-16 nhưng chặn sớm hơn cho key mới) — dùng bản kế nhiệm.
        // Đổi sang Flash-Lite (2026-08-23): gemini-3.6-flash phản hồi quá chậm cho tóm tắt/điền báo
        // cáo từ video; Flash-Lite vẫn hỗ trợ video đầy đủ và là bản nhanh nhất dòng Gemini 3, đã
        // dùng ổn cho TranscriptModel bên dưới (bước chép lời nặng token hơn cả bước này).
        public string Model { get; set; } = "gemini-3.5-flash-lite";
        /// <summary>Model riêng cho lượt chép lời. Chép lời chỉ là ghi lại đúng những gì nghe được, không
        /// cần suy luận như tóm tắt, nên dùng bản Lite nhanh hơn (~350 so với ~280 token/giây). Đây là
        /// chặng sinh nhiều token nhất (transcript dài gấp 10-15 lần tóm tắt) nên chênh lệch tốc độ ở
        /// đây có giá trị nhất.</summary>
        public string TranscriptModel { get; set; } = "gemini-3.5-flash-lite";
        public int MaxOutputTokens { get; set; } = 4096;
        /// <summary>Giới hạn token riêng cho lượt tóm tắt+transcript (transcript đầy đủ 1 buổi học dài có thể
        /// rất nhiều token) — dùng MaxOutputTokens thường (2048-4096) sẽ bị Gemini cắt giữa chừng, JSON trả về
        /// không đóng được nên parse lỗi (đã gặp thực tế: JsonException "Expected end of string").
        ///
        /// 65536 CHÍNH LÀ TRẦN CỨNG của Gemini 3.x (đã xác nhận qua docs Google, áp dụng cho cả dòng Flash/
        /// Flash-Lite/Pro — 1M token context nhưng output luôn giới hạn ~65536, không thể chỉnh cao hơn qua
        /// config). Buổi học đủ dài để transcript vượt mốc này (đã gặp thực tế: lỗi "Buổi học quá dài,
        /// Gemini không viết kịp hết hội thoại") KHÔNG có cách nào sửa bằng cách tăng số ở đây — cần tách audio
        /// thành nhiều đoạn, chép lời riêng từng đoạn rồi ghép lại (xem RunStudentTranscriptJobAsync), hoặc
        /// chấp nhận buổi quá dài sẽ không có transcript đầy đủ (tóm tắt vẫn hoạt động bình thường, không phụ
        /// thuộc giới hạn này).</summary>
        public int TranscriptMaxOutputTokens { get; set; } = 65536;
        public float Temperature { get; set; } = 0.7f;
        /// <summary>Có chép lời (transcript) buổi ghi âm app hay không. Transcript KHÔNG hiển thị cho gia sư
        /// (họp 22/9: gia sư chỉ xem báo cáo + biên bản buổi học) — nó là "nguyên liệu thô" của hệ thống
        /// (phân tích, hỏi đáp sau này). Vì không ai chờ kết quả nên chép lời chạy nền bằng Gemini Batch
        /// API (giá 50%), ẩn danh rồi lưu thành file JSON trên kho — xem RecorderTranscriptBatchJob.
        /// Đặt false (GoogleGemini:GenerateTranscript) để ngừng xếp hàng chép lời các buổi mới.</summary>
        public bool GenerateTranscript { get; set; } = true;

        /// <summary>Chờ tối thiểu bấy nhiêu phút kể từ buổi xếp hàng sớm nhất rồi mới gửi batch, để gom
        /// nhiều buổi vào một batch. Không được quá lâu: file audio trên Gemini chỉ sống ~48 giờ.</summary>
        public int TranscriptBatchDelayMinutes { get; set; } = 30;

        /// <summary>Chép lời qua Gemini Batch API (giá 50%, chậm vài giờ) hay gọi thường (giá đầy đủ, xong
        /// trong ~30–40 phút). Mặc định TẮT: quy mô hiện tại chênh ~150–200k/tháng, không đáng độ phức tạp;
        /// Batch API cũng bị từ chối khi project Gemini chưa bật billing. Bật lại khi số buổi lớn.</summary>
        public bool TranscriptUseBatch { get; set; } = false;

        /// <summary>Giữ file transcript trên kho bao nhiêu ngày (tính từ lúc kết thúc ghi) rồi xoá.
        /// Luật BVDLCN không cho lưu vô thời hạn — mặc định 2 năm, đổi khi có chính sách chính thức.</summary>
        public int TranscriptRetentionDays { get; set; } = 180;
    }
}
