-- =====================================================
-- V20260930 — Lưu bằng chứng người dùng đồng ý Điều khoản / Chính sách quyền riêng tư.
--   * user_policy_acceptances: mỗi dòng = một người dùng đã bấm đồng ý một văn bản
--     (policy_documents.slug) ở đúng phiên bản đang xuất bản lúc đó.
--   * policy_version cùng kiểu với policy_documents.version (varchar(20)); không có
--     bản published thì ghi 'unpublished' (đăng ký không bị chặn).
--   * source: 'mobile' (app gia sư) | 'web'. ip_address lấy từ request đăng ký.
--   * FK users ON DELETE CASCADE: xoá mềm tài khoản (users.is_deleted) KHÔNG xoá dòng
--     nào ở đây — bằng chứng pháp lý được giữ. Chỉ khi hàng users bị xoá cứng (dọn
--     tài khoản rác chưa xác thực OTP, admin xoá vĩnh viễn) thì dòng mới đi theo;
--     để NO ACTION thì hai luồng đó sẽ bị FK chặn giữa chừng.
-- Chỉ thêm bảng mới, không đụng bảng cũ.
-- =====================================================
BEGIN;

CREATE TABLE IF NOT EXISTS public.user_policy_acceptances (
    id              bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id         varchar(50)  NOT NULL,
    policy_slug     varchar(80)  NOT NULL,
    policy_version  varchar(20)  NOT NULL,
    accepted_at     timestamp without time zone NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC'),
    source          varchar(20)  NOT NULL DEFAULT 'web',
    ip_address      varchar(64),

    CONSTRAINT user_policy_acceptances_user_fkey FOREIGN KEY (user_id)
        REFERENCES public.users (user_id) ON DELETE CASCADE,
    CONSTRAINT user_policy_acceptances_source_check CHECK (source IN ('mobile', 'web')),
    CONSTRAINT uq_user_policy_acceptances_user_slug_version
        UNIQUE (user_id, policy_slug, policy_version)
);

CREATE INDEX IF NOT EXISTS idx_user_policy_acceptances_user
    ON public.user_policy_acceptances (user_id);

COMMIT;
