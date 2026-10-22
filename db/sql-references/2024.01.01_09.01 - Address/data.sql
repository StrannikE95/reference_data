-- Строки версии 1 адреса.

INSERT INTO address.addr_object (version_id, addr_object_id, name)
VALUES
    (1, 'b1111111-1111-4111-8111-111111111111', 'Центральный'),
    (1, 'b2222222-2222-4222-8222-222222222222', 'Северный');

INSERT INTO address.addr_house (version_id, addr_house_id, addr_object_id, house_number)
VALUES
    (1, 'c1111111-1111-4111-8111-111111111111', 'b1111111-1111-4111-8111-111111111111', '1'),
    (1, 'c2222222-2222-4222-8222-222222222222', 'b2222222-2222-4222-8222-222222222222', '2');
