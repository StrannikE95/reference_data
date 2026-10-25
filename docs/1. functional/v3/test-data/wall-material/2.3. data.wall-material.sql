CREATE TABLE wall_material_1
    PARTITION OF wall_material FOR VALUES IN (1);

INSERT INTO wall_material (version_id, simple_dictionary_table_id, value_code, name, parent_code) VALUES
    (1, gen_random_uuid(), '061001001000', 'каменные', NULL),
    (1, gen_random_uuid(), '061001001001', 'кирпичные', '061001001000');

