CREATE TABLE gar.addr_obj_1
    PARTITION OF gar.addr_obj FOR VALUES IN (1);

INSERT INTO gar.addr_obj (version_id, object_id, object_guid, name, type_name, level_code, start_date, end_date) VALUES
    (1, 597041, '00000000-0000-0000-0000-000000000000', 'Тверская', 'ул', '8', DATE '2014-01-01', DATE '2079-06-06');

