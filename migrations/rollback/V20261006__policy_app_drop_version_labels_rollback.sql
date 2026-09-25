-- Rollback V20261006 — trả lại các cụm đã thay.
BEGIN;

UPDATE policy_documents
   SET content_markdown =
       replace(replace(replace(replace(replace(replace(content_markdown,
           '**Ngày hiệu lực:** 01/10/2026 · **Áp dụng cho:**', '**Phiên bản:** v1 · **Ngày hiệu lực:** 01/10/2026 · **Áp dụng cho:**'),
           '**Effective date:** 01/10/2026 · **Applies to:**', '**Version:** v1 · **Effective date:** 01/10/2026 · **Applies to:**'),
           'phiên bản văn bản đồng ý,', 'phiên bản văn bản đồng ý (ví dụ "v1"),'),
           'the consent text version,', 'the consent text version (e.g. "v1"),'),
           'Mỗi lần cập nhật đều ghi rõ ngày hiệu lực.',
           'Mỗi phiên bản ghi rõ số phiên bản và ngày hiệu lực; các phiên bản cũ được lưu tại.'),
           'Each update states its effective date.',
           'Each version shows a version number and effective date; previous versions are archived at.')
 WHERE slug = 'privacy-app';

UPDATE policy_documents
   SET content_markdown =
       replace(replace(content_markdown,
           'trong thời hạn pháp luật yêu cầu, không dùng', 'trong, không dùng'),
           'for the period required by law, not used', 'for, not used')
 WHERE slug = 'data-deletion';

COMMIT;
