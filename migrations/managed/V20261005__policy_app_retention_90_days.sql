-- =====================================================
-- V20261005 — Thời hạn lưu bản ghi âm và bản chép lời: 180 → 90 ngày (quyết định 2026-09-24,
-- khớp RecorderRetention.AudioDays và GoogleGemini:TranscriptRetentionDays).
--   * Sửa nội dung privacy-app và data-deletion (VI + EN) đã chèn ở V20261002.
--   * Chỉ thay đúng các câu nói về 180 ngày bằng replace(), để các chỗ admin đã chỉnh qua CMS
--     được giữ nguyên. Chạy lại không đổi gì thêm.
--   * Không nâng version chính sách: app chưa phát hành.
-- =====================================================
BEGIN;

UPDATE policy_documents
   SET content_markdown =
       replace(replace(replace(replace(content_markdown,
           'Tự động xoá **180 ngày** sau ngày ghi.', 'Tự động xoá **90 ngày** sau ngày ghi.'),
           '| Bản chép lời đã ẩn danh | **180 ngày** sau ngày ghi, sau đó xoá |',
           '| Bản chép lời đã ẩn danh | **90 ngày** sau ngày ghi, sau đó xoá |'),
           'Automatically deleted **180 days** after recording.', 'Automatically deleted **90 days** after recording.'),
           '| Anonymized transcript | **180 days** after recording, then deleted |',
           '| Anonymized transcript | **90 days** after recording, then deleted |')
 WHERE slug = 'privacy-app';

UPDATE policy_documents
   SET content_markdown =
       replace(replace(content_markdown,
           '| Bản ghi âm còn lưu (tối đa 180 ngày) |', '| Bản ghi âm còn lưu (tối đa 90 ngày) |'),
           '| Remaining audio (kept at most 180 days) |', '| Remaining audio (kept at most 90 days) |')
 WHERE slug = 'data-deletion';

COMMIT;
