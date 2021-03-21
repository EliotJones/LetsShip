CREATE TABLE emails (
  id SERIAL CONSTRAINT pk_emails PRIMARY KEY,
  body TEXT NOT NULL,
  subject TEXT NOT NULL,
  recipient VARCHAR(320) NOT NULL,
  user_id INT NULL,
  created TIMESTAMPTZ NOT NULL DEFAULT (current_timestamp AT TIME ZONE 'UTC'),
  CONSTRAINT fk_email_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);