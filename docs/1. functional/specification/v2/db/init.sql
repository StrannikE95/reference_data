-- Каталог справочников v2 для пустой базы PostgreSQL.
-- Живые таблицы хранения tables здесь не создаются: их пишет создание паспорта.
--
-- psql -d dictionaries -f "docs/1. functional/specification/v2/db/init.sql"

CREATE TABLE dictionary (
    dictionary_id   uuid         PRIMARY KEY,
    dictionary_code varchar(63)  NOT NULL,
    dictionary_name varchar(200) NOT NULL,
    owner           varchar(200) NULL,
    status          varchar(16)  NOT NULL DEFAULT 'active',
    storage         varchar(16)  NOT NULL,
    model           jsonb        NOT NULL,
    CONSTRAINT dictionary_status_check
        CHECK (status IN ('active', 'inactive')),
    CONSTRAINT dictionary_storage_check
        CHECK (storage IN ('row', 'tables'))
);

CREATE UNIQUE INDEX ux_dictionary_code
    ON dictionary (lower(dictionary_code));

CREATE TABLE dictionary_version (
    dictionary_version_id uuid        PRIMARY KEY,
    dictionary_id         uuid        NOT NULL
        REFERENCES dictionary (dictionary_id),
    version_id            integer     NOT NULL,
    valid_from            date        NOT NULL,
    valid_to              date        NULL,
    status                varchar(16) NOT NULL,
    CONSTRAINT dictionary_version_pk
        UNIQUE (dictionary_id, version_id),
    CONSTRAINT dictionary_version_status_check
        CHECK (status IN ('draft', 'active', 'closed')),
    CONSTRAINT dictionary_version_open_valid_to_check
        CHECK (status = 'closed' OR valid_to IS NULL),
    CONSTRAINT dictionary_version_closed_valid_to_check
        CHECK (status <> 'closed' OR valid_to IS NOT NULL)
);

CREATE UNIQUE INDEX ux_dictionary_version_active
    ON dictionary_version (dictionary_id)
    WHERE status = 'active';

CREATE UNIQUE INDEX ux_dictionary_version_draft
    ON dictionary_version (dictionary_id)
    WHERE status = 'draft';

CREATE TABLE dictionary_schema (
    dictionary_version_id uuid  PRIMARY KEY
        REFERENCES dictionary_version (dictionary_version_id),
    definition            jsonb NOT NULL
);

CREATE TABLE dictionary_history (
    dictionary_id         uuid         NOT NULL
        REFERENCES dictionary (dictionary_id),
    dictionary_version_id uuid         NOT NULL
        REFERENCES dictionary_version (dictionary_version_id),
    table_name            varchar(63)  NOT NULL,
    row_key               varchar(512) NOT NULL,
    valid_from            date         NOT NULL,
    valid_to              date         NOT NULL,
    data                  jsonb        NOT NULL,
    CONSTRAINT dictionary_history_pk
        PRIMARY KEY (dictionary_id, table_name, row_key, valid_from),
    CONSTRAINT dictionary_history_period_check
        CHECK (valid_to >= valid_from)
);

CREATE INDEX ix_dictionary_history_period
    ON dictionary_history (dictionary_id, table_name, valid_from, valid_to);

CREATE TABLE dictionary_draft_row (
    dictionary_version_id uuid         NOT NULL
        REFERENCES dictionary_version (dictionary_version_id),
    table_name            varchar(63)  NOT NULL,
    row_key               varchar(512) NOT NULL,
    data                  jsonb        NOT NULL,
    CONSTRAINT dictionary_draft_row_pk
        PRIMARY KEY (dictionary_version_id, table_name, row_key)
);

CREATE TABLE dictionary_row (
    dictionary_id uuid         NOT NULL
        REFERENCES dictionary (dictionary_id),
    value_code    varchar(64)  NOT NULL,
    parent_code   varchar(64)  NULL,
    valid_from    date         NOT NULL,
    valid_to      date         NULL,
    data          jsonb        NOT NULL,
    CONSTRAINT dictionary_row_pk
        PRIMARY KEY (dictionary_id, value_code, valid_from),
    CONSTRAINT dictionary_row_period_check
        CHECK (valid_to IS NULL OR valid_to >= valid_from)
) PARTITION BY LIST (dictionary_id);
