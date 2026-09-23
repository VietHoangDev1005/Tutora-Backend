-- =====================================================
-- V20260924 — Ngày kết thúc thời khoá biểu của học sinh ngoài nền tảng.
-- Không có ngày kết thúc thì lịch lặp vô hạn (sinh tới ~6 tháng tới).
-- Chỉ đụng schema recorder.
-- =====================================================
BEGIN;

ALTER TABLE recorder.students ADD COLUMN IF NOT EXISTS schedule_until date;

COMMIT;
