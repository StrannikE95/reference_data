-- Одна таблица значений на все справочники. Секция справочника создаётся вместе с паспортом.
--
-- psql -d dictionaries -f "docs/1. functional/specification/v3 - 1 таблица/db/init.sql"

CREATE TABLE dictionary (
    dictionary_id   uuid         PRIMARY KEY,
    dictionary_code varchar(63)  NOT NULL,
    dictionary_name varchar(200) NOT NULL,
    owner           varchar(200) NULL,
    status          varchar(16)  NOT NULL DEFAULT 'active',
    CONSTRAINT dictionary_status_check
        CHECK (status IN ('active', 'inactive'))
);

CREATE TABLE scheme_version (
    dictionary_id uuid        NOT NULL
        REFERENCES dictionary (dictionary_id),
    scheme_version_id integer NOT NULL,
    status        varchar(16) NOT NULL,
    valid_from    timestamptz NULL,
    valid_to      timestamptz NULL,
    scheme        jsonb       NOT NULL,
    PRIMARY KEY (dictionary_id, scheme_version_id),
    CONSTRAINT scheme_version_status_check
        CHECK (status IN ('draft', 'active', 'closed'))
);

CREATE TABLE data_version (
    dictionary_id     uuid        NOT NULL
        REFERENCES dictionary (dictionary_id),
    data_version_id   integer     NOT NULL,
    schema_version_id integer     NOT NULL,
    status            varchar(16) NOT NULL,
    type              varchar(8)  NOT NULL,
    published_at      timestamptz NULL,
    PRIMARY KEY (dictionary_id, data_version_id),
    FOREIGN KEY (dictionary_id, schema_version_id)
        REFERENCES scheme_version (dictionary_id, scheme_version_id),
    CONSTRAINT data_version_status_check
        CHECK (status IN ('draft', 'published')),
    CONSTRAINT data_version_type_check
        CHECK (type IN ('delta', 'full'))
);

CREATE UNIQUE INDEX ux_dictionary_code
    ON dictionary (lower(dictionary_code));

CREATE TABLE dictionary_row (
    dictionary_code varchar(63)  NOT NULL,
    value_code      varchar(64)  NOT NULL,
    data_version_id integer      NOT NULL,
    status          varchar(16)  NOT NULL,
    valid_from      timestamptz  NOT NULL,
    valid_to        timestamptz  NULL,
    data            jsonb        NOT NULL,
    PRIMARY KEY (dictionary_code, value_code, data_version_id, valid_from),
    CONSTRAINT dictionary_row_status_check
        CHECK (status IN ('draft', 'published'))
) PARTITION BY LIST (dictionary_code);

CREATE INDEX ix_dictionary_row_code
    ON dictionary_row (dictionary_code, value_code, valid_from);
