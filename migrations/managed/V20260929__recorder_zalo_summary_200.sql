-- =====================================================
-- V20260929 — Nới tóm tắt Zalo từ 90 lên 200 ký tự.
--   * Template ZBS 640496 (đã duyệt 24/09/2026) khai báo content/homework/note
--     là loại "Tên sản phẩm / Thương hiệu" (200 ký tự); gửi thử 148 ký tự hiển
--     thị đầy đủ. Gemini nhắm ≤ 180, code cắt cứng ở 200.
-- Chỉ đụng schema recorder. Nới độ dài varchar không làm mất dữ liệu.
-- =====================================================
BEGIN;

ALTER TABLE recorder.lessons ALTER COLUMN zalo_content  TYPE varchar(200);
ALTER TABLE recorder.lessons ALTER COLUMN zalo_homework TYPE varchar(200);
ALTER TABLE recorder.lessons ALTER COLUMN zalo_notes    TYPE varchar(200);

COMMIT;
