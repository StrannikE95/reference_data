CREATE TABLE oktmo_1
    PARTITION OF oktmo FOR VALUES IN (1);

INSERT INTO oktmo (version_id, simple_dictionary_table_id, value_code, section, name, parent_code, change_number, introduced_on) VALUES
    (1, gen_random_uuid(), '01512000101', '2', 'с Залесово', NULL, '493', DATE '2022-01-01');

