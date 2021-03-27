-- In order to keep locking while allowing child inserts we need to remove the constraint :(
ALTER TABLE draft_job_logs DROP CONSTRAINT fk_draft_job_log_draft_job;