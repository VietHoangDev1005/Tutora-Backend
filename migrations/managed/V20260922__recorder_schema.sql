-- =====================================================
-- V20260922 — Khu "recorder": ghi âm buổi dạy từ app gia sư, cho cả
-- học sinh ngoài nền tảng (không có booking) lẫn buổi có booking.
--
-- Nguyên tắc: mọi thứ của tính năng này nằm trong schema riêng `recorder`.
-- Bảng ở đây trỏ TỚI bảng cũ (users, class_sessions); bảng cũ không có cột
-- nào trỏ ngược lại. Xoá cả schema (`DROP SCHEMA recorder CASCADE`) không làm
-- hỏng booking, thanh toán, web hay bot.
--
-- Migration này cũng dọn hai thứ V20260921 đã thêm:
--   * public.session_recordings  → chuyển dữ liệu sang recorder.lessons rồi xoá
--   * class_session_ai_jobs.source → xoá (job AI của app giờ nằm trong recorder.lessons)
-- =====================================================
BEGIN;

CREATE SCHEMA IF NOT EXISTS recorder;

-- ── Học sinh của gia sư (danh bạ riêng, KHÔNG phải tài khoản) ──────────────
-- Không tạo user Parent/Student nào. SĐT phụ huynh chỉ dùng để gửi báo cáo.
CREATE TABLE IF NOT EXISTS recorder.students (
    student_id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tutor_id            varchar(50)  NOT NULL,
    full_name           varchar(100) NOT NULL,
    grade               smallint,                -- 1..12, NULL = không rõ
    subject             varchar(100),
    parent_name         varchar(100),
    parent_phone        varchar(20),
    -- unknown | tutor_confirmed (gia sư xác nhận PH đã đồng ý) | parent_confirmed | declined
    consent_status      varchar(20)  NOT NULL DEFAULT 'unknown',
    consent_at          timestamp without time zone,
    note                text,
    -- Dành cho sau này: nối với hồ sơ học sinh thật khi PH đặt booking trên Tutora.
    linked_student_user_id varchar(50),
    archived_at         timestamp without time zone,
    created_at          timestamp without time zone NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC'),
    updated_at          timestamp without time zone NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC'),

    CONSTRAINT recorder_students_tutor_fkey FOREIGN KEY (tutor_id)
        REFERENCES public.users (user_id) ON DELETE CASCADE,
    CONSTRAINT recorder_students_linked_fkey FOREIGN KEY (linked_student_user_id)
        REFERENCES public.users (user_id) ON DELETE SET NULL,
    CONSTRAINT recorder_students_grade_check CHECK (grade IS NULL OR grade BETWEEN 1 AND 12),
    CONSTRAINT recorder_students_consent_check
        CHECK (consent_status IN ('unknown', 'tutor_confirmed', 'parent_confirmed', 'declined'))
);

CREATE INDEX IF NOT EXISTS idx_recorder_students_tutor
    ON recorder.students (tutor_id) WHERE archived_at IS NULL;

-- ── Buổi dạy (nhật ký) = lịch + bản ghi + báo cáo AI + trạng thái gửi ───────
-- Đúng MỘT trong hai: student_id (ngoài nền tảng) hoặc class_session_id (có booking).
-- Không tạo dòng nào trong public.class_sessions cho buổi ngoài nền tảng.
CREATE TABLE IF NOT EXISTS recorder.lessons (
    lesson_id           uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tutor_id            varchar(50)  NOT NULL,
    student_id          uuid,
    class_session_id    integer,

    scheduled_start     timestamp without time zone,
    scheduled_end       timestamp without time zone,
    subject             varchar(100),

    -- scheduled | recording | uploading | processing | awaiting_approval | sent | failed | discarded
    status              varchar(20)  NOT NULL DEFAULT 'scheduled',

    -- Bản ghi
    started_at          timestamp without time zone,
    ended_at            timestamp without time zone,
    duration_sec        integer      NOT NULL DEFAULT 0,
    bytes               bigint       NOT NULL DEFAULT 0,
    part_count          integer      NOT NULL DEFAULT 0,
    storage_key         text,
    consent_snapshot    jsonb,

    -- AI
    ai_status           varchar(20)  NOT NULL DEFAULT 'none',   -- none | pending | processing | completed | failed
    ai_result           jsonb,
    ai_error            text,
    gemini_file_name    text,
    gemini_file_uri     text,
    gemini_file_expires_at timestamp without time zone,

    -- Báo cáo đã duyệt + gửi
    report_content      text,
    report_homework     text,
    report_notes        text,
    approved_at         timestamp without time zone,
    delivery_channel    varchar(20),                 -- booking | zns
    delivery_status     varchar(20),                 -- pending | sent | failed
    delivery_error      text,
    sent_at             timestamp without time zone,

    error_message       text,
    created_at          timestamp without time zone NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC'),
    updated_at          timestamp without time zone NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC'),

    CONSTRAINT recorder_lessons_tutor_fkey FOREIGN KEY (tutor_id)
        REFERENCES public.users (user_id) ON DELETE CASCADE,
    CONSTRAINT recorder_lessons_student_fkey FOREIGN KEY (student_id)
        REFERENCES recorder.students (student_id) ON DELETE CASCADE,
    -- Xoá buổi học cũ không bị chặn: nhật ký chỉ mất liên kết.
    CONSTRAINT recorder_lessons_session_fkey FOREIGN KEY (class_session_id)
        REFERENCES public.class_sessions (class_session_id) ON DELETE SET NULL,
    CONSTRAINT recorder_lessons_one_owner_check
        CHECK (student_id IS NULL OR class_session_id IS NULL),
    CONSTRAINT recorder_lessons_status_check CHECK (status IN
        ('scheduled', 'recording', 'uploading', 'processing', 'awaiting_approval', 'sent', 'failed', 'discarded')),
    CONSTRAINT recorder_lessons_ai_status_check CHECK (ai_status IN
        ('none', 'pending', 'processing', 'completed', 'failed'))
);

-- Một buổi có booking = đúng một dòng nhật ký (huỷ bản ghi thì dùng lại dòng đó).
CREATE UNIQUE INDEX IF NOT EXISTS uq_recorder_lessons_class_session
    ON recorder.lessons (class_session_id) WHERE class_session_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_recorder_lessons_tutor_time
    ON recorder.lessons (tutor_id, scheduled_start DESC);

CREATE INDEX IF NOT EXISTS idx_recorder_lessons_student
    ON recorder.lessons (student_id, scheduled_start DESC) WHERE student_id IS NOT NULL;

-- ── Chuyển dữ liệu từ V20260921 ─────────────────────────────────────────────
DO $$
BEGIN
    IF to_regclass('public.session_recordings') IS NOT NULL THEN
        INSERT INTO recorder.lessons (
            lesson_id, tutor_id, class_session_id, status,
            started_at, ended_at, duration_sec, bytes, part_count, storage_key,
            consent_snapshot, error_message, created_at, updated_at,
            ai_status, ai_result, ai_error, gemini_file_name, gemini_file_uri, gemini_file_expires_at)
        SELECT DISTINCT ON (r.class_session_id)
               r.recording_id, r.tutor_id, r.class_session_id, r.status,
               r.started_at, r.ended_at, r.duration_sec, r.bytes, r.part_count, r.storage_key,
               r.consent_snapshot, r.error_message, r.created_at, r.created_at,
               COALESCE(j.status, 'none'), j.result_json, j.error_message,
               j.gemini_file_name, j.gemini_file_uri, j.gemini_file_expires_at
        FROM public.session_recordings r
        LEFT JOIN LATERAL (
            SELECT * FROM public.class_session_ai_jobs a
            WHERE a.class_session_id = r.class_session_id
              AND a.job_type = 'tutor_report_fill'
            ORDER BY a.created_at DESC
            LIMIT 1
        ) j ON TRUE
        -- Bản sống (khác discarded) thắng; cùng loại thì bản mới nhất.
        ORDER BY r.class_session_id, (r.status = 'discarded'), r.started_at DESC
        ON CONFLICT DO NOTHING;

        DROP TABLE public.session_recordings;
    END IF;
END $$;

ALTER TABLE public.class_session_ai_jobs DROP COLUMN IF EXISTS source;

COMMIT;
