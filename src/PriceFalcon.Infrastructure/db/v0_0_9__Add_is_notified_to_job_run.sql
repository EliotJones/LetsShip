ALTER TABLE job_runs ADD COLUMN is_notified BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE job_runs ADD COLUMN email_id int NULL;
