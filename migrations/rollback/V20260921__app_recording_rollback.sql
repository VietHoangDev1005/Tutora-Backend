-- Rollback V20260921 — ghi âm từ app gia sư.
-- Lưu ý: DROP TABLE xoá luôn mọi bản ghi đã tạo. Chỉ chạy khi chắc chắn chưa có
-- buổi nào dùng luồng app, hoặc đã sao lưu bảng.
BEGIN;
DROP INDEX IF EXISTS public.idx_session_recordings_tutor;
DROP INDEX IF EXISTS public.uq_session_recordings_live;
DROP TABLE IF EXISTS public.session_recordings;
ALTER TABLE public.class_session_ai_jobs DROP COLUMN IF EXISTS source;
COMMIT;
