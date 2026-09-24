-- Rollback V20261003.
BEGIN;
DELETE FROM recorder.consent_events WHERE action IN ('granted', 'withdrawn');
ALTER TABLE recorder.consent_events DROP CONSTRAINT IF EXISTS recorder_consent_events_action_check;
ALTER TABLE recorder.consent_events ADD CONSTRAINT recorder_consent_events_action_check
    CHECK (action IN ('linked', 'unlinked', 'declined'));
ALTER TABLE recorder.students DROP COLUMN IF EXISTS consent_version;
COMMIT;
