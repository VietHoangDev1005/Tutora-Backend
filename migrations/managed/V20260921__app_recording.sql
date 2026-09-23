-- =====================================================
-- V20260921 — Ghi âm buổi học từ app gia sư (v0.1).
--
-- Bối cảnh: buổi dạy TẠI NHÀ không có kênh Agora nào để ghi, nên gia sư bấm
-- ghi âm ngay trên điện thoại. App cắt thành đoạn 5 phút, upload từng đoạn lên
-- S3 (dùng lại bucket Agora đang ghi vào, khác prefix), rồi backend đưa thẳng
-- audio lên Gemini — bỏ qua Drive và bỏ qua bước ffmpeg tách tiếng khỏi video.
--
-- Điểm cốt lõi của thiết kế nằm ở UNIQUE INDEX phía dưới: mỗi buổi học chỉ có
-- đúng một bản ghi còn sống. Client không bao giờ gửi kèm người nhận — server
-- tự join class_sessions → students → parent. Gửi nhầm phụ huynh vì thế không
-- phải là chuyện "code cẩn thận", mà là chuyện không biểu diễn được.
-- =====================================================
BEGIN;

CREATE TABLE IF NOT EXISTS public.session_recordings (
    recording_id       uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    class_session_id   integer     NOT NULL,
    tutor_id           varchar(50) NOT NULL,

    -- recording | uploading | processing | awaiting_approval | sent | failed | discarded
    status             varchar(20) NOT NULL DEFAULT 'recording',

    started_at         timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ended_at           timestamp without time zone,
    duration_sec       integer     NOT NULL DEFAULT 0,
    bytes              bigint      NOT NULL DEFAULT 0,
    part_count         integer     NOT NULL DEFAULT 0,

    -- Prefix trên S3, vd app-recordings/1234/<recording_id>/
    storage_key        text,

    -- Ảnh chụp sự đồng ý tại thời điểm bấm ghi: ai đồng ý, lúc nào, bản thông
    -- báo phiên bản nào. Lưu snapshot chứ không tham chiếu, vì văn bản thông báo
    -- sẽ đổi mà bằng chứng cho buổi này thì phải giữ nguyên như lúc đó.
    consent_snapshot   jsonb,

    error_message      text,
    created_at         timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT session_recordings_session_fkey FOREIGN KEY (class_session_id)
        REFERENCES public.class_sessions(class_session_id) ON DELETE CASCADE
);

-- Một buổi = một bản ghi sống. Bản đã huỷ không tính, để gia sư lỡ tay huỷ thì
-- còn ghi lại được.
CREATE UNIQUE INDEX IF NOT EXISTS uq_session_recordings_live
    ON public.session_recordings (class_session_id)
    WHERE status <> 'discarded';

CREATE INDEX IF NOT EXISTS idx_session_recordings_tutor
    ON public.session_recordings (tutor_id, started_at DESC);

-- Nguồn audio của một job AI. NULL = luồng cũ (video Agora trên Drive), giữ
-- nguyên hành vi cho mọi job đã có. 'app_audio' = file từ app gia sư.
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                   WHERE table_name = 'class_session_ai_jobs' AND column_name = 'source') THEN
        ALTER TABLE public.class_session_ai_jobs ADD COLUMN source varchar(20);
    END IF;
END $$;

COMMIT;
