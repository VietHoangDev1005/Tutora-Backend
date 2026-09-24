-- Rollback V20261002: gỡ 2 văn bản của app gia sư (chỉ khi chưa có ai đồng ý theo slug này).
BEGIN;
DELETE FROM policy_documents
 WHERE slug IN ('privacy-app', 'data-deletion')
   AND NOT EXISTS (SELECT 1 FROM user_policy_acceptances a WHERE a.policy_slug = policy_documents.slug);
COMMIT;
