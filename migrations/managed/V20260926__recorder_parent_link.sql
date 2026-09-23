-- =====================================================
-- V20260926 — Liên kết phụ huynh qua Zalo Mini App (học sinh ngoài nền tảng).
--
--   * recorder.parents        : phụ huynh đã tự xác nhận qua Mini App (có UID theo OA)
--   * recorder.parent_invites : link mời gia sư gửi cho phụ huynh (token dùng 1 lần)
--   * recorder.consent_events : nhật ký đồng ý / huỷ đồng ý — bằng chứng khi Zalo
--                               hoặc phụ huynh hỏi lại (PDPL, quy định ZBS)
--   * recorder.students.parent_id: học sinh đang gắn với phụ huynh nào
--
-- Vẫn chỉ đụng schema recorder. Không tạo user nào trong public.users.
-- =====================================================
BEGIN;

CREATE TABLE IF NOT EXISTS recorder.parents (
    parent_id           uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    -- UID của người dùng theo OA Tutora — dùng để gửi tin OA / ZBS qua UID.
    zalo_uid            varchar(50)  NOT NULL,
    -- ID theo Zalo App (Mini App / Graph API) — dùng đối chiếu khi xác minh.
    zalo_app_user_id    varchar(50),
    display_name        varchar(100),
    is_follower         boolean      NOT NULL DEFAULT true,
    -- Lần cuối phụ huynh tương tác với OA (mở cửa sổ Tin Tư vấn 7 ngày / 48h miễn phí).
    last_interaction_at timestamp without time zone,
    created_at          timestamp without time zone NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC'),
    updated_at          timestamp without time zone NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC'),
    CONSTRAINT recorder_parents_zalo_uid_key UNIQUE (zalo_uid)
);

CREATE TABLE IF NOT EXISTS recorder.parent_invites (
    invite_id           uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    student_id          uuid         NOT NULL,
    tutor_id            varchar(50)  NOT NULL,
    token               varchar(64)  NOT NULL,
    created_at          timestamp without time zone NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC'),
    expires_at          timestamp without time zone NOT NULL,
    used_at             timestamp without time zone,
    used_by_parent_id   uuid,
    revoked_at          timestamp without time zone,
    CONSTRAINT recorder_parent_invites_token_key UNIQUE (token),
    CONSTRAINT recorder_parent_invites_student_fkey FOREIGN KEY (student_id)
        REFERENCES recorder.students (student_id) ON DELETE CASCADE,
    CONSTRAINT recorder_parent_invites_tutor_fkey FOREIGN KEY (tutor_id)
        REFERENCES public.users (user_id) ON DELETE CASCADE,
    CONSTRAINT recorder_parent_invites_parent_fkey FOREIGN KEY (used_by_parent_id)
        REFERENCES recorder.parents (parent_id) ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS idx_recorder_parent_invites_student
    ON recorder.parent_invites (student_id) WHERE used_at IS NULL AND revoked_at IS NULL;

CREATE TABLE IF NOT EXISTS recorder.consent_events (
    event_id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    student_id          uuid         NOT NULL,
    parent_id           uuid,
    -- linked | unlinked | declined
    action              varchar(20)  NOT NULL,
    -- miniapp_follow | tutor | parent_request ...
    method              varchar(30)  NOT NULL,
    -- Phiên bản nội dung phụ huynh đã đọc khi đồng ý (VD: 2026-09-v1).
    consent_version     varchar(30),
    zalo_uid            varchar(50),
    ip_address          varchar(64),
    user_agent          text,
    created_at          timestamp without time zone NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC'),
    CONSTRAINT recorder_consent_events_student_fkey FOREIGN KEY (student_id)
        REFERENCES recorder.students (student_id) ON DELETE CASCADE,
    CONSTRAINT recorder_consent_events_parent_fkey FOREIGN KEY (parent_id)
        REFERENCES recorder.parents (parent_id) ON DELETE SET NULL,
    CONSTRAINT recorder_consent_events_action_check
        CHECK (action IN ('linked', 'unlinked', 'declined'))
);

CREATE INDEX IF NOT EXISTS idx_recorder_consent_events_student
    ON recorder.consent_events (student_id, created_at DESC);

ALTER TABLE recorder.students ADD COLUMN IF NOT EXISTS parent_id uuid;
ALTER TABLE recorder.students ADD COLUMN IF NOT EXISTS parent_linked_at timestamp without time zone;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'recorder_students_parent_fkey') THEN
        ALTER TABLE recorder.students
            ADD CONSTRAINT recorder_students_parent_fkey FOREIGN KEY (parent_id)
            REFERENCES recorder.parents (parent_id) ON DELETE SET NULL;
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS idx_recorder_students_parent
    ON recorder.students (parent_id) WHERE parent_id IS NOT NULL;

COMMIT;
