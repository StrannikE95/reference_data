CREATE SCHEMA gar;

CREATE TABLE gar.object_level (
    version_id integer NOT NULL,
    level_code varchar(10) NOT NULL,
    name varchar(250) NOT NULL,
    short_name varchar(50) NOT NULL,
    PRIMARY KEY (version_id, level_code)
) PARTITION BY LIST (version_id);

CREATE TABLE gar.addr_obj (
    version_id integer NOT NULL,
    object_id bigint NOT NULL,
    object_guid uuid NOT NULL,
    name varchar(250) NOT NULL,
    type_name varchar(50) NOT NULL,
    level_code varchar(10) NOT NULL,
    start_date date NOT NULL,
    end_date date NOT NULL,
    PRIMARY KEY (version_id, object_id),
    FOREIGN KEY (version_id, level_code)
        REFERENCES gar.object_level (version_id, level_code)
) PARTITION BY LIST (version_id);

