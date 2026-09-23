-- Rollback V20260927. File JSON đã lưu trên kho không bị xoá theo.
BEGIN;
DROP INDEX IF EXISTS recorder.idx_recorder_lessons_transcript_queue;
ALTER TABLE recorder.lessons DROP COLUMN IF EXISTS transcript_queued_at;
ALTER TABLE recorder.lessons DROP COLUMN IF EXISTS transcript_batch;
ALTER TABLE recorder.lessons DROP COLUMN IF EXISTS transcript_model;
ALTER TABLE recorder.lessons DROP COLUMN IF EXISTS transcript_key;
COMMIT;
