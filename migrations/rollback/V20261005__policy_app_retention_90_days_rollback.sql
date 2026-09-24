-- Rollback V20261005.
BEGIN;

UPDATE policy_documents
   SET content_markdown =
       replace(replace(replace(replace(content_markdown,
           'Tự động xoá **90 ngày** sau ngày ghi.', 'Tự động xoá **180 ngày** sau ngày ghi.'),
           '| Bản chép lời đã ẩn danh | **90 ngày** sau ngày ghi, sau đó xoá |',
           '| Bản chép lời đã ẩn danh | **180 ngày** sau ngày ghi, sau đó xoá |'),
           'Automatically deleted **90 days** after recording.', 'Automatically deleted **180 days** after recording.'),
           '| Anonymized transcript | **90 days** after recording, then deleted |',
           '| Anonymized transcript | **180 days** after recording, then deleted |')
 WHERE slug = 'privacy-app';

UPDATE policy_documents
   SET content_markdown =
       replace(replace(content_markdown,
           '| Bản ghi âm còn lưu (tối đa 90 ngày) |', '| Bản ghi âm còn lưu (tối đa 180 ngày) |'),
           '| Remaining audio (kept at most 90 days) |', '| Remaining audio (kept at most 180 days) |')
 WHERE slug = 'data-deletion';

COMMIT;
