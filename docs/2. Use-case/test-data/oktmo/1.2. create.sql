CREATE TABLE oktmo (
    version_id integer NOT NULL,
    simple_dictionary_table_id uuid NOT NULL,
    value_code varchar(11) NOT NULL,
    section varchar(1) NOT NULL,
    name varchar(500) NOT NULL,
    parent_code varchar(11) NULL,
    change_number varchar(10) NOT NULL,
    introduced_on date NOT NULL,
    PRIMARY KEY (version_id, simple_dictionary_table_id),
    UNIQUE (version_id, value_code, section),
    FOREIGN KEY (version_id, parent_code, section)
        REFERENCES oktmo (version_id, value_code, section)
) PARTITION BY LIST (version_id);

