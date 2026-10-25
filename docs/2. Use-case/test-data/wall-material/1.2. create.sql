CREATE TABLE wall_material (
    version_id integer NOT NULL,
    simple_dictionary_table_id uuid NOT NULL,
    value_code varchar(12) NOT NULL,
    name varchar(500) NOT NULL,
    parent_code varchar(12) NULL,
    PRIMARY KEY (version_id, simple_dictionary_table_id),
    UNIQUE (version_id, value_code),
    FOREIGN KEY (version_id, parent_code)
        REFERENCES wall_material (version_id, value_code)
) PARTITION BY LIST (version_id);

