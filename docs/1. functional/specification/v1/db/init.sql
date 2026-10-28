-- Каталог справочников для пустой базы PostgreSQL.
-- Таблицы значений здесь не создаются: их пишет создание паспорта.
--
-- psql -d dictionaries -f "docs/1. functional/v3/db/init.sql"

CREATE TABLE dictionary (
    dictionary_id   uuid         PRIMARY KEY,
    dictionary_code varchar(63)  NOT NULL,
    dictionary_name varchar(200) NOT NULL,
    owner           varchar(200) NULL,
    status          varchar(16)  NOT NULL DEFAULT 'active',
    "JSON_metadata" varchar      NOT NULL,
    CONSTRAINT dictionary_status_check
        CHECK (status IN ('active', 'inactive'))
);

CREATE UNIQUE INDEX ux_dictionary_code
    ON dictionary (lower(dictionary_code));

CREATE TABLE dictionary_version (
    dictionary_version_id uuid        PRIMARY KEY,
    dictionary_id         uuid        NOT NULL
        REFERENCES dictionary (dictionary_id),
    version_id            integer     NOT NULL,
    valid_from            date        NULL,
    valid_to              date        NULL,
    status                varchar(16) NOT NULL,
    table_name            varchar(63) NULL,
    schema_name           varchar(63) NULL,
    CONSTRAINT dictionary_version_pk
        UNIQUE (dictionary_id, version_id),
    CONSTRAINT dictionary_version_status_check
        CHECK (status IN ('draft', 'active', 'closed', 'archived')),
    CONSTRAINT dictionary_version_valid_from_check
        CHECK (valid_from IS NOT NULL OR status = 'draft'),
    CONSTRAINT dictionary_version_draft_valid_to_check
        CHECK (status <> 'draft' OR valid_to IS NULL),
    CONSTRAINT dictionary_version_active_valid_to_check
        CHECK (status <> 'active' OR valid_to IS NULL),
    CONSTRAINT dictionary_version_closed_valid_to_check
        CHECK (status NOT IN ('closed', 'archived') OR valid_to IS NOT NULL),
    CONSTRAINT dictionary_version_placement_check
        CHECK ((table_name IS NOT NULL) <> (schema_name IS NOT NULL))
);

CREATE UNIQUE INDEX ux_dictionary_version_active
    ON dictionary_version (dictionary_id)
    WHERE status = 'active';

CREATE UNIQUE INDEX ux_dictionary_version_draft
    ON dictionary_version (dictionary_id)
    WHERE status = 'draft';
