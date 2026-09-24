-- Rollback V20260930. Mất toàn bộ bằng chứng đồng ý đã ghi — chỉ chạy khi chắc chắn.
BEGIN;
DROP TABLE IF EXISTS public.user_policy_acceptances;
COMMIT;
