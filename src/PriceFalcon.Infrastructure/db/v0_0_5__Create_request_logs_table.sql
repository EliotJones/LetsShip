CREATE TABLE request_logs (
  id SERIAL CONSTRAINT pk_request_logs PRIMARY KEY,
  ip_address VARCHAR(52) NOT NULL,
  url TEXT NOT NULL,
  elapsed_milliseconds INT8 NOT NULL,
  status_code INT NULL,
  method VARCHAR(10) NOT NULL,
  created TIMESTAMPTZ NOT NULL DEFAULT (current_timestamp AT TIME ZONE 'UTC')
);