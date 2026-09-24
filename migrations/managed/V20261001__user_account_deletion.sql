-- =====================================================
-- V20261001 — Người dùng tự xoá tài khoản (app gia sư, yêu cầu của App Store / Google Play).
--   * Dùng lại cột có sẵn users.is_deleted + users.deleted_at (xoá mềm) và status = 0
--     (mọi cổng đăng nhập / refresh / OnTokenValidated đã chặn status = 0).
--   * deletion_source : 'self' = người dùng tự xoá qua POST /api/users/me/delete-account;
--                       NULL = dòng is_deleted cũ (luồng admin cũ) — job dọn KHÔNG đụng tới.
--   * deletion_reason : lý do người dùng nhập (tuỳ chọn).
--   * purged_at       : job AccountDeletionPurgeJob đã xoá file ghi âm/transcript, xoá
--                       nội dung báo cáo, ẩn danh học sinh recorder và PII của user
--                       (sau AccountDeletion.PurgeAfterDays = 30 ngày). Chạy đúng một lần.
-- Chỉ thêm cột nullable — không khoá bảng lâu, không đổi dữ liệu cũ.
-- =====================================================
BEGIN;

ALTER TABLE public.users ADD COLUMN IF NOT EXISTS deletion_source varchar(20);
ALTER TABLE public.users ADD COLUMN IF NOT EXISTS deletion_reason varchar(500);
ALTER TABLE public.users ADD COLUMN IF NOT EXISTS purged_at timestamp without time zone;

-- Job dọn quét các tài khoản tự xoá chưa purge, theo deleted_at.
CREATE INDEX IF NOT EXISTS idx_users_self_deletion_purge
    ON public.users (deleted_at)
    WHERE deletion_source = 'self' AND purged_at IS NULL;

COMMIT;
