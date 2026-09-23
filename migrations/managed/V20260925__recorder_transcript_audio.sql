-- =====================================================
-- V20260925 — Transcript + file nghe lại cho buổi ghi âm từ app.
--   * transcript: lời thoại có mốc thời gian, AI chép sau khi viết báo cáo
--   * audio_key : file đã ghép (merged.m4a) trên kho, để app nghe lại
--   * audio_deleted_at: file âm thanh đã bị xoá theo hạn lưu trữ (30 ngày)
-- Chỉ đụng schema recorder.
-- =====================================================
BEGIN;

ALTER TABLE recorder.lessons ADD COLUMN IF NOT EXISTS transcript text;
ALTER TABLE recorder.lessons ADD COLUMN IF NOT EXISTS transcript_status varchar(20) NOT NULL DEFAULT 'none';
ALTER TABLE recorder.lessons ADD COLUMN IF NOT EXISTS transcript_error text;
ALTER TABLE recorder.lessons ADD COLUMN IF NOT EXISTS audio_key text;
ALTER TABLE recorder.lessons ADD COLUMN IF NOT EXISTS audio_deleted_at timestamp without time zone;

-- Job dọn file quét theo ngày kết thúc ghi.
CREATE INDEX IF NOT EXISTS idx_recorder_lessons_audio_retention
    ON recorder.lessons (ended_at)
    WHERE audio_deleted_at IS NULL AND storage_key IS NOT NULL;

COMMIT;
