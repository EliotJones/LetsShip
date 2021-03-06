CREATE TABLE users (
  id SERIAL CONSTRAINT pk_users PRIMARY KEY,
  email VARCHAR(320) NOT NULL,
  is_verified BOOLEAN NOT NULL DEFAULT FALSE,
  created TIMESTAMPTZ NOT NULL DEFAULT (current_timestamp AT TIME ZONE 'UTC'),
  CONSTRAINT uq_user_email UNIQUE (email)
);

CREATE TABLE tokens (
  id SERIAL CONSTRAINT pk_tokens PRIMARY KEY,
  user_id INT NOT NULL,
  value VARCHAR(256) NOT NULL,
  is_used BOOLEAN NOT NULL DEFAULT FALSE,
  purpose INT NOT NULL,
  expiry TIMESTAMPTZ NOT NULL,
  created TIMESTAMPTZ NOT NULL DEFAULT (current_timestamp AT TIME ZONE 'UTC'),
  CONSTRAINT uq_token_value UNIQUE (value),
  CONSTRAINT fk_token_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);