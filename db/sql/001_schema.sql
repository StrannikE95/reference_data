-- Каталог и две поставки, как после регистрации: простой список и схема из двух таблиц.
-- Скрипт можно выполнить повторно: старые таблицы снимаются.

DROP SCHEMA IF EXISTS address CASCADE;
DROP TABLE IF EXISTS wall_material CASCADE;
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

CREATE TABLE wall_material (
    version_id        integer NOT NULL,
    wall_material_id  uuid    NOT NULL,
    value_code        varchar NOT NULL,
    name              varchar NOT NULL,
    PRIMARY KEY (version_id, wall_material_id),
    UNIQUE (version_id, value_code)
) PARTITION BY LIST (version_id);

CREATE TABLE wall_material_1
    PARTITION OF wall_material FOR VALUES IN (1);

CREATE SCHEMA address;

CREATE TABLE address.addr_object (
    version_id     integer NOT NULL,
    addr_object_id uuid    NOT NULL,
    name           varchar NOT NULL,
    PRIMARY KEY (version_id, addr_object_id)
) PARTITION BY LIST (version_id);

CREATE TABLE address.addr_house (
    version_id     integer NOT NULL,
    addr_house_id  uuid    NOT NULL,
    addr_object_id uuid    NOT NULL,
    house_number   varchar NOT NULL,
    PRIMARY KEY (version_id, addr_house_id),
    FOREIGN KEY (version_id, addr_object_id)
        REFERENCES address.addr_object (version_id, addr_object_id)
) PARTITION BY LIST (version_id);

CREATE TABLE address.addr_object_1
    PARTITION OF address.addr_object FOR VALUES IN (1);

CREATE TABLE address.addr_house_1
    PARTITION OF address.addr_house FOR VALUES IN (1);
