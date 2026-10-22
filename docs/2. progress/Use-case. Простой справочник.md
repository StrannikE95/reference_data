# Простой справочник

Справочники в базу не попадают при старте. Каталог `dictionary` и `dictionary_version` создаёт `management.api` из `db/sql-init/001_schema.sql` и оставляет его пустым. Каждый справочник лежит отдельно в `db/sql-references` и выполняется вручную по соединению из раздела «Подключение через DBeaver» в `Use-case. Первичный запуск.md`.

## 1. Папка справочника

Один справочник — одна подпапка:

`db/sql-references/YYYY.MM.DD_HH.mm - name`

`YYYY.MM.DD_HH.mm` — момент добавления, по нему папки идут по порядку. `name` — код справочника, как `dictionary_code`.

В папке два файла, и только они:

| Файл | Что в нём |
| --- | --- |
| `metadata.sql` | Паспорт в `dictionary`, строка `dictionary_version`, родительская таблица `PARTITION BY LIST (version_id)` и пустая секция этой версии. Для набора таблиц — схема и все её таблицы |
| `data.sql` | Строки этой версии. Для набора сначала родитель, потом зависимая таблица |

Уже лежат:

- `2024.01.01_09.00 - WallMaterial` — простой список `wall_material`, коды `BRICK` и `WOOD`
- `2024.01.01_09.01 - Address` — схема `address`, таблицы `addr_object` и `addr_house`

## 2. Как выполнить готовый справочник

База уже запущена, каталог пуст. В DBeaver для папки справочника, по порядку имени папки:

1. Открыть `metadata.sql` → Execute SQL Script.
2. Открыть `data.sql` → Execute SQL Script.

`data.sql` без `metadata.sql` не выполняется: таблиц ещё нет.

Проверка `WallMaterial`:

`GET http://localhost:8080/admin/dictionaries/WallMaterial`

Паспорт `active`, `table_name = wall_material`, одна версия `1` со статусом `active`.

`GET http://localhost:8082/dictionaries/WallMaterial/values/BRICK`

`version_id = 1`, `result = present`, `name = Кирпич`.

## 3. Как добавить новый справочник

Пример — простой список `OwnershipType`, таблица `ownership_type`. Код в уже добавленных папках не занят. Имя папки — текущие дата и время, например `2026.10.22_14.00 - OwnershipType`.

1. Создать `db/sql-references/2026.10.22_14.00 - OwnershipType`.
2. Положить `metadata.sql`:

```sql
CREATE TABLE ownership_type (
    version_id         integer NOT NULL,
    ownership_type_id  uuid    NOT NULL,
    value_code         varchar NOT NULL,
    name               varchar NOT NULL,
    PRIMARY KEY (version_id, ownership_type_id),
    UNIQUE (version_id, value_code)
) PARTITION BY LIST (version_id);

CREATE TABLE ownership_type_1
    PARTITION OF ownership_type FOR VALUES IN (1);

INSERT INTO dictionary (dictionary_id, dictionary_code, dictionary_name, owner, source, status)
VALUES ('8d3f5e4c-3f60-4c9d-9e53-2c709b4d9f32', 'OwnershipType', 'Вид собственности', 'Имущество', 'Ведомственный перечень', 'active');

INSERT INTO dictionary_version (
    dictionary_version_id, dictionary_id, version_id, valid_from, valid_to, status, table_name, schema_name)
VALUES ('e4c13f62-9d77-4f3c-a088-37b2e1c6d034', '8d3f5e4c-3f60-4c9d-9e53-2c709b4d9f32', 1, '2024-01-01', NULL, 'active', 'ownership_type', NULL);
```

3. Положить `data.sql`:

```sql
INSERT INTO ownership_type (version_id, ownership_type_id, value_code, name)
VALUES
    (1, 'd1111111-1111-4111-8111-111111111111', 'PRIVATE', 'Частная'),
    (1, 'd2222222-2222-4222-8222-222222222222', 'STATE', 'Государственная');
```

4. Выполнить `metadata.sql`, затем `data.sql`.

Проверка:

`GET http://localhost:8082/dictionaries/OwnershipType/values/PRIVATE`

`version_id = 1`, `result = present`, `name = Частная`.

`GET http://localhost:8082/dictionaries/OwnershipType/values?limit=100`

В `items` две строки: `PRIVATE` / «Частная», `STATE` / «Государственная». `next_cursor` пуст.

Следующая версия того же справочника — новая папка с более поздним временем, секция `ownership_type_2` и `version_id = 2`. Предыдущую папку не переписывают.
