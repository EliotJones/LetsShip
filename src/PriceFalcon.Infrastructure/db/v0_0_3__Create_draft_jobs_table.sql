CREATE TABLE draft_jobs (
  id SERIAL CONSTRAINT pk_draft_jobs PRIMARY KEY,
  url TEXT NOT NULL,
  crawled_html TEXT NULL,
  user_id INT NOT NULL,
  status INT NOT NULL,
  created TIMESTAMPTZ NOT NULL DEFAULT (current_timestamp AT TIME ZONE 'UTC'),
  CONSTRAINT fk_draft_job_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);

CREATE TABLE draft_job_logs (
  id SERIAL CONSTRAINT pk_draft_job_logs PRIMARY KEY,
  draft_job_id INT NOT NULL,
  message TEXT NULL,
  status INT NOT NULL,
  created TIMESTAMPTZ NOT NULL DEFAULT (current_timestamp AT TIME ZONE 'UTC'),
  CONSTRAINT fk_draft_job_log_draft_job FOREIGN KEY (draft_job_id) REFERENCES draft_jobs(id) ON DELETE CASCADE
);