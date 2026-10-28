-- Одна таблица значений на все справочники. Секция справочника создаётся вместе с паспортом.
--
-- psql -d dictionaries -f "docs/1. functional/specification/v2 - 1 таблица/db/init.sql"

CREATE TABLE dictionary (
    dictionary_id   uuid         PRIMARY KEY,
    dictionary_code varchar(63)  NOT NULL,
    dictionary_name varchar(200) NOT NULL,
    owner           varchar(200) NULL,
    status          varchar(16)  NOT NULL DEFAULT 'active',
    fields          jsonb        NOT NULL,
    CONSTRAINT dictionary_status_check
        CHECK (status IN ('active', 'inactive'))
);

CREATE UNIQUE INDEX ux_dictionary_code
    ON dictionary (lower(dictionary_code));

CREATE TABLE dictionary_row (
    dictionary_code varchar(63)  NOT NULL,
    value_code      varchar(64)  NOT NULL,
    valid_from      timestamptz  NOT NULL,
    valid_to        timestamptz  NULL,
    data            jsonb        NOT NULL,
    PRIMARY KEY (dictionary_code, value_code, valid_from)
) PARTITION BY LIST (dictionary_code);

CREATE INDEX ix_dictionary_row_code
    ON dictionary_row (dictionary_code, value_code, valid_from);
