-- Каталог справочников. Таблиц списков и строк здесь нет.
-- Скрипт можно выполнить повторно: старые таблицы каталога снимаются.

DROP TABLE IF EXISTS dictionary_version CASCADE;
DROP TABLE IF EXISTS dictionary CASCADE;

CREATE TABLE dictionary (
    dictionary_id   uuid PRIMARY KEY,
    dictionary_code varchar NOT NULL UNIQUE,
    dictionary_name varchar NOT NULL,
    owner           varchar NOT NULL,
    source          varchar NOT NULL,
    status          varchar NOT NULL
);

CREATE TABLE dictionary_version (
    dictionary_version_id uuid PRIMARY KEY,
    dictionary_id         uuid NOT NULL REFERENCES dictionary (dictionary_id),
    version_id            integer NOT NULL,
    valid_from            date,
    valid_to              date,
    status                varchar NOT NULL,
    table_name            varchar,
    schema_name           varchar,
    UNIQUE (dictionary_id, version_id)
);
