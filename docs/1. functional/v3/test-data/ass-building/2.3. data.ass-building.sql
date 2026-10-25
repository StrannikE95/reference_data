CREATE TABLE ass_building_1
    PARTITION OF ass_building FOR VALUES IN (1);

INSERT INTO ass_building (version_id, simple_dictionary_table_id, value_code, name) VALUES
    (1, gen_random_uuid(), '204001000000', 'Нежилое'),
    (1, gen_random_uuid(), '204002000000', 'Жилой дом'),
    (1, gen_random_uuid(), '204003000000', 'Многоквартирный дом'),
    (1, gen_random_uuid(), '204004000000', 'Жилое строение');

