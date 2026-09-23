-- =====================================================
-- V20260923 — Thời khoá biểu hằng tuần cho học sinh ngoài nền tảng.
--
-- Gia sư đặt lịch cố định (vd. T2 + T5, 19:00–20:30). Backend sinh sẵn các
-- dòng recorder.lessons trạng thái 'scheduled' để tab Lịch hiển thị được.
-- Chỉ đụng schema recorder.
-- =====================================================
BEGIN;

-- [{"dayOfWeek":1,"start":"19:00","end":"20:30"}, ...] — dayOfWeek 1 = Thứ 2 … 7 = Chủ nhật, giờ VN.
ALTER TABLE recorder.students ADD COLUMN IF NOT EXISTS schedule jsonb;

-- Ngày bắt đầu áp dụng lịch hiện tại (đổi lịch → đặt lại). Không sinh buổi trước ngày này.
ALTER TABLE recorder.students ADD COLUMN IF NOT EXISTS schedule_from date;

-- Mỗi học sinh không có hai buổi cùng giờ bắt đầu → sinh lịch lặp lại không bị trùng.
CREATE UNIQUE INDEX IF NOT EXISTS uq_recorder_lessons_student_slot
    ON recorder.lessons (student_id, scheduled_start)
    WHERE student_id IS NOT NULL AND scheduled_start IS NOT NULL;

COMMIT;
