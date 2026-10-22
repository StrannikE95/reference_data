-- Адрес: паспорт, версия 1 и схема из двух секционированных таблиц.

INSERT INTO dictionary (dictionary_id, dictionary_code, dictionary_name, owner, source, status)
VALUES ('7c2e4d3b-2e5f-4b8c-8d42-1b6f9a3c8e21', 'Address', 'Адрес', 'Адреса', 'ФИАС', 'active');

INSERT INTO dictionary_version (
    dictionary_version_id, dictionary_id, version_id, valid_from, valid_to, status, table_name, schema_name)
VALUES ('d3b02e51-8c66-4e2b-9f77-26a1d0b5c923', '7c2e4d3b-2e5f-4b8c-8d42-1b6f9a3c8e21', 1, '2024-01-01', NULL, 'active', NULL, 'address');

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
