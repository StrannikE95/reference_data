-- По две строки в каждую таблицу. У каждого справочника одна действующая версия 1.

INSERT INTO dictionary (dictionary_id, dictionary_code, dictionary_name, owner, source, status)
VALUES
    ('6b1f3c2a-1d4e-4a7b-9c31-0a5e8f2b7d10', 'WallMaterial', 'Материал стен', 'Имущество', 'Ведомственный перечень', 'active'),
    ('7c2e4d3b-2e5f-4b8c-8d42-1b6f9a3c8e21', 'Address', 'Адрес', 'Адреса', 'ФИАС', 'active');

INSERT INTO dictionary_version (
    dictionary_version_id, dictionary_id, version_id, valid_from, valid_to, status, table_name, schema_name)
VALUES
    ('c2a91e40-7b55-4d1a-8e66-15f0c9a4b812', '6b1f3c2a-1d4e-4a7b-9c31-0a5e8f2b7d10', 1, '2024-01-01', NULL, 'active', 'wall_material', NULL),
    ('d3b02e51-8c66-4e2b-9f77-26a1d0b5c923', '7c2e4d3b-2e5f-4b8c-8d42-1b6f9a3c8e21', 1, '2024-01-01', NULL, 'active', NULL, 'address');

INSERT INTO wall_material (version_id, wall_material_id, value_code, name)
VALUES
    (1, 'a1111111-1111-4111-8111-111111111111', 'BRICK', 'Кирпич'),
    (1, 'a2222222-2222-4222-8222-222222222222', 'WOOD', 'Дерево');

INSERT INTO address.addr_object (version_id, addr_object_id, name)
VALUES
    (1, 'b1111111-1111-4111-8111-111111111111', 'Центральный'),
    (1, 'b2222222-2222-4222-8222-222222222222', 'Северный');

INSERT INTO address.addr_house (version_id, addr_house_id, addr_object_id, house_number)
VALUES
    (1, 'c1111111-1111-4111-8111-111111111111', 'b1111111-1111-4111-8111-111111111111', '1'),
    (1, 'c2222222-2222-4222-8222-222222222222', 'b2222222-2222-4222-8222-222222222222', '2');
