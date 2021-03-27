CREATE TABLE jobs (
  id SERIAL CONSTRAINT pk_jobs PRIMARY KEY,
  draft_job_id INT NOT NULL,
  url TEXT NOT NULL,
  crawled_html TEXT NULL,
  user_id INT NOT NULL,
  selector JSONB NOT NULL,
  next_due_date TIMESTAMPTZ NOT NULL,
  token_id INT NOT NULL,
  start_price MONEY NULL,
  status INT NOT NULL,
  created TIMESTAMPTZ NOT NULL DEFAULT (current_timestamp AT TIME ZONE 'UTC'),
  CONSTRAINT fk_job_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
  CONSTRAINT fk_job_draft_job_id FOREIGN KEY (draft_job_id) REFERENCES draft_jobs(id) ON DELETE CASCADE,
  CONSTRAINT fk_job_token FOREIGN KEY (token_id) REFERENCES tokens(id) ON DELETE CASCADE
);

CREATE TABLE job_runs (
  id SERIAL CONSTRAINT pk_job_runs PRIMARY KEY,
  job_id INT NOT NULL,
  message TEXT NULL,
  price MONEY NULL,
  status INT NOT NULL,
  created TIMESTAMPTZ NOT NULL DEFAULT (current_timestamp AT TIME ZONE 'UTC'),
  CONSTRAINT fk_job_runs_job FOREIGN KEY (job_id) REFERENCES jobs(id) ON DELETE CASCADE
);