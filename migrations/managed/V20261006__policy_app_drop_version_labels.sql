-- =====================================================
-- V20261006 — Bỏ nhãn phiên bản ("v1") khỏi nội dung privacy-app, sửa câu cụt.
--   * privacy-app: bỏ "Phiên bản: v1" ở đầu trang và ví dụ "v1" của văn bản đồng ý; sửa câu
--     "các phiên bản cũ được lưu tại." (thiếu link) (VI + EN).
--   * data-deletion: câu về bản ghi đồng ý thiếu thời hạn ("trong, …") (VI + EN).
--   * Chỉ thay đúng các cụm này bằng replace(); chạy lại không đổi gì thêm.
-- =====================================================
BEGIN;

UPDATE policy_documents
   SET content_markdown =
       replace(replace(replace(replace(replace(replace(content_markdown,
           '**Phiên bản:** v1 · **Ngày hiệu lực:**', '**Ngày hiệu lực:**'),
           '**Version:** v1 · **Effective date:**', '**Effective date:**'),
           'phiên bản văn bản đồng ý (ví dụ "v1"),', 'phiên bản văn bản đồng ý,'),
           'the consent text version (e.g. "v1"),', 'the consent text version,'),
           'Mỗi phiên bản ghi rõ số phiên bản và ngày hiệu lực; các phiên bản cũ được lưu tại.',
           'Mỗi lần cập nhật đều ghi rõ ngày hiệu lực.'),
           'Each version shows a version number and effective date; previous versions are archived at.',
           'Each update states its effective date.')
 WHERE slug = 'privacy-app';

UPDATE policy_documents
   SET content_markdown =
       replace(replace(content_markdown,
           'giữ làm bằng chứng tuân thủ trong, không dùng',
           'giữ làm bằng chứng tuân thủ trong thời hạn pháp luật yêu cầu, không dùng'),
           'kept as compliance proof for, not used',
           'kept as compliance proof for the period required by law, not used')
 WHERE slug = 'data-deletion';

COMMIT;
