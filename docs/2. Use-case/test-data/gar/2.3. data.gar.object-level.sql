CREATE TABLE gar.object_level_1
    PARTITION OF gar.object_level FOR VALUES IN (1);

INSERT INTO gar.object_level (version_id, level_code, name, short_name) VALUES
    (1, '8', 'Элемент улично-дорожной сети', 'Улица');

