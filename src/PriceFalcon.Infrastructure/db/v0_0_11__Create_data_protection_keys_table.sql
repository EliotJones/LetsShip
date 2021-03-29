CREATE TABLE data_protection_keys (
  id SERIAL CONSTRAINT pk_data_protection_keys PRIMARY KEY,
  name TEXT NULL,
  value TEXT NOT NULL
);