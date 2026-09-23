-- =====================================================
-- V20260927 — Transcript thành "nguyên liệu thô" của hệ thống.
--   * Gia sư không còn xem transcript (họp 22/9). Transcript được chép lời
--     chạy nền bằng Gemini Batch API (rẻ 50%), ẩn danh, rồi lưu thành file
--     JSON trên kho (Storage) — DB chỉ giữ key trỏ tới file.
--   * transcript_key   : key file JSON trên kho
--   * transcript_model : model đã chép lời (để biết khi nào cần chép lại)
--   * transcript_batch : tên batch Gemini đang xử lý (batches/...)
--   * transcript_queued_at : lúc xếp hàng chờ chép lời
-- Cột transcript (chữ) cũ giữ lại để job chuyển dần dữ liệu cũ sang kho rồi để trống.
-- Chỉ đụng schema recorder.
-- =====================================================
BEGIN;

ALTER TABLE recorder.lessons ADD COLUMN IF NOT EXISTS transcript_key text;
ALTER TABLE recorder.lessons ADD COLUMN IF NOT EXISTS transcript_model varchar(60);
ALTER TABLE recorder.lessons ADD COLUMN IF NOT EXISTS transcript_batch varchar(100);
ALTER TABLE recorder.lessons ADD COLUMN IF NOT EXISTS transcript_queued_at timestamp without time zone;

-- Job nền quét hàng đợi chép lời theo trạng thái.
CREATE INDEX IF NOT EXISTS idx_recorder_lessons_transcript_queue
    ON recorder.lessons (transcript_status, transcript_queued_at)
    WHERE transcript_status IN ('queued', 'batching');

COMMIT;
