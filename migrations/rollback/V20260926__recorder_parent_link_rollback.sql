-- Rollback V20260926 — gỡ liên kết phụ huynh. Chỉ đụng schema recorder.
BEGIN;
ALTER TABLE recorder.students DROP CONSTRAINT IF EXISTS recorder_students_parent_fkey;
DROP INDEX IF EXISTS recorder.idx_recorder_students_parent;
ALTER TABLE recorder.students DROP COLUMN IF EXISTS parent_linked_at;
ALTER TABLE recorder.students DROP COLUMN IF EXISTS parent_id;
DROP TABLE IF EXISTS recorder.consent_events;
DROP TABLE IF EXISTS recorder.parent_invites;
DROP TABLE IF EXISTS recorder.parents;
COMMIT;
