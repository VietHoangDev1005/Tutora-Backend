-- Rollback V20261001. Cột is_deleted/deleted_at có từ trước nên giữ nguyên;
-- tài khoản đã tự xoá vẫn bị khoá (status = 0) nhưng job dọn sẽ không còn nhận ra.
BEGIN;
DROP INDEX IF EXISTS public.idx_users_self_deletion_purge;
ALTER TABLE public.users DROP COLUMN IF EXISTS purged_at;
ALTER TABLE public.users DROP COLUMN IF EXISTS deletion_reason;
ALTER TABLE public.users DROP COLUMN IF EXISTS deletion_source;
COMMIT;
