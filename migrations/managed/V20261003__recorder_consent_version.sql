-- =====================================================
-- V20261003 — Phiên bản nội dung đồng ý ghi âm (Google Play / Luật BVDLCN 91/2025).
--   * recorder.students.consent_version : nội dung phụ huynh đã đọc khi gia sư tick xác nhận
--     (app hiện gửi 'v1'; 'v0' = xác nhận từ bản app cũ chưa hiện nội dung đồng ý).
--   * recorder.consent_events.action thêm 'granted' / 'withdrawn' — gia sư xác nhận / bỏ
--     xác nhận trong app (method = 'tutor'), kèm consent_version làm bằng chứng.
--   * Backend từ nay CHẶN ghi âm khi học sinh chưa có xác nhận đồng ý (trước chỉ chặn declined).
-- Chỉ đụng schema recorder.
-- =====================================================
BEGIN;

ALTER TABLE recorder.students ADD COLUMN IF NOT EXISTS consent_version varchar(30);

-- Xác nhận cũ (trước khi có nội dung đồng ý trong app) → v0.
UPDATE recorder.students
   SET consent_version = 'v0'
 WHERE consent_status = 'tutor_confirmed' AND consent_version IS NULL;

ALTER TABLE recorder.consent_events DROP CONSTRAINT IF EXISTS recorder_consent_events_action_check;
ALTER TABLE recorder.consent_events ADD CONSTRAINT recorder_consent_events_action_check
    CHECK (action IN ('linked', 'unlinked', 'declined', 'granted', 'withdrawn'));

COMMIT;
