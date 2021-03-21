ALTER TABLE draft_jobs 
ADD COLUMN monitoring_token_id INT NULL,
ADD CONSTRAINT fk_draft_job_token FOREIGN KEY (monitoring_token_id) REFERENCES tokens(id) ON DELETE CASCADE;