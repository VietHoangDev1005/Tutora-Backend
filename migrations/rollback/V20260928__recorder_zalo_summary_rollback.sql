-- Rollback V20260928. Bản nháp tóm tắt trong ai_result (jsonb) không bị ảnh hưởng.
BEGIN;
ALTER TABLE recorder.lessons DROP COLUMN IF EXISTS zalo_notes;
ALTER TABLE recorder.lessons DROP COLUMN IF EXISTS zalo_homework;
ALTER TABLE recorder.lessons DROP COLUMN IF EXISTS zalo_content;
COMMIT;
