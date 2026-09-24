-- Rollback V20260929: thu hẹp lại 90 ký tự (cắt bớt giá trị dài hơn).
BEGIN;

ALTER TABLE recorder.lessons ALTER COLUMN zalo_content  TYPE varchar(90) USING left(zalo_content, 90);
ALTER TABLE recorder.lessons ALTER COLUMN zalo_homework TYPE varchar(90) USING left(zalo_homework, 90);
ALTER TABLE recorder.lessons ALTER COLUMN zalo_notes    TYPE varchar(90) USING left(zalo_notes, 90);

COMMIT;
