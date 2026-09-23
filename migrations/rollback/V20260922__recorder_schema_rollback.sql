-- Rollback V20260922 — xoá toàn bộ khu recorder.
-- CẢNH BÁO: mất danh bạ học sinh, nhật ký buổi dạy và báo cáo đã duyệt của
-- luồng ghi âm app. File âm thanh trên kho (bucket lesson-recordings) KHÔNG bị
-- xoá — dọn riêng nếu cần. Bảng public cũ không bị đụng tới.
BEGIN;
DROP SCHEMA IF EXISTS recorder CASCADE;
DELETE FROM app_schema_migrations WHERE version = 'V20260922__recorder_schema.sql';
COMMIT;
