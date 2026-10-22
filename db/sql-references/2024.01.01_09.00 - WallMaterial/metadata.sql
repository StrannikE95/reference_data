-- Материал стен: паспорт, версия 1 и секционированная таблица.

CREATE TABLE wall_material (
    version_id       integer NOT NULL,
    wall_material_id uuid    NOT NULL,
    value_code       varchar NOT NULL,
    name             varchar NOT NULL,
    PRIMARY KEY (version_id, wall_material_id),
    UNIQUE (version_id, value_code)
) PARTITION BY LIST (version_id);

CREATE TABLE wall_material_1
    PARTITION OF wall_material FOR VALUES IN (1);

INSERT INTO dictionary (dictionary_id, dictionary_code, dictionary_name, owner, source, status)
VALUES ('6b1f3c2a-1d4e-4a7b-9c31-0a5e8f2b7d10', 'WallMaterial', 'Материал стен', 'Имущество', 'Ведомственный перечень', 'active');

INSERT INTO dictionary_version (
    dictionary_version_id, dictionary_id, version_id, valid_from, valid_to, status, table_name, schema_name)
VALUES ('c2a91e40-7b55-4d1a-8e66-15f0c9a4b812', '6b1f3c2a-1d4e-4a7b-9c31-0a5e8f2b7d10', 1, '2024-01-01', NULL, 'active', 'wall_material', NULL);
