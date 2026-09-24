-- =====================================================
-- V20260928 — Tóm tắt ngắn gửi phụ huynh qua tin Zalo.
--   * Template ZBS giới hạn giá trị dòng bảng 90 ký tự; trước đây job gửi cắt
--     báo cáo đầy đủ (report_*) → câu bị cụt, còn dính markdown.
--   * Gemini sinh thêm bản tóm tắt ngắn (≤ 85 ký tự/field) nằm trong ai_result
--     (ZaloSummary, jsonb — không cần cột mới cho bản nháp). Gia sư sửa/duyệt
--     trên app, bản đã duyệt lưu vào các cột dưới đây:
--   * zalo_content  : tóm tắt nội dung buổi học
--   * zalo_homework : tóm tắt bài tập về nhà
--   * zalo_notes    : tóm tắt nhận xét
-- Null → job gửi fallback về report_* đã bỏ markdown rồi cắt ngắn.
-- Chỉ đụng schema recorder.
-- =====================================================
BEGIN;

ALTER TABLE recorder.lessons ADD COLUMN IF NOT EXISTS zalo_content varchar(90);
ALTER TABLE recorder.lessons ADD COLUMN IF NOT EXISTS zalo_homework varchar(90);
ALTER TABLE recorder.lessons ADD COLUMN IF NOT EXISTS zalo_notes varchar(90);

COMMIT;
