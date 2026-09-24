-- =====================================================
-- V20261004 — Báo cáo nội dung AI sai (Google Play: app có AI tạo nội dung phải cho người dùng
-- báo nội dung sai / không phù hợp).
--   * recorder.ai_feedback : gia sư bấm "Báo nội dung AI sai" trên màn xem báo cáo.
-- Chỉ thêm bảng mới trong schema recorder, không đổi dữ liệu có sẵn.
-- =====================================================
BEGIN;

CREATE TABLE IF NOT EXISTS recorder.ai_feedback (
    feedback_id uuid        NOT NULL DEFAULT gen_random_uuid(),
    lesson_id   uuid        NOT NULL,
    tutor_id    varchar(50) NOT NULL,
    reason      varchar(30) NOT NULL,
    note        varchar(1000),
    created_at  timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'utc'),
    CONSTRAINT ai_feedback_pkey PRIMARY KEY (feedback_id),
    CONSTRAINT recorder_ai_feedback_lesson_fkey FOREIGN KEY (lesson_id)
        REFERENCES recorder.lessons (lesson_id) ON DELETE CASCADE,
    CONSTRAINT recorder_ai_feedback_tutor_fkey FOREIGN KEY (tutor_id)
        REFERENCES public.users (user_id) ON DELETE CASCADE,
    CONSTRAINT recorder_ai_feedback_reason_check
        CHECK (reason IN ('wrong_content', 'wrong_student', 'inappropriate', 'other'))
);

CREATE INDEX IF NOT EXISTS ix_recorder_ai_feedback_created ON recorder.ai_feedback (created_at DESC);

COMMIT;
