# Простой справочник

## 2. Простой справочник

Новый список `OwnershipType`, одна таблица `ownership_type`. Код в начальном наполнении не занят. Поставка — полный дамп, `delivery = full`.

### 2.1. Добавление нового справочника

Паспорт в каталог пишется только на завершении регистрации. До этого таблиц нет.

1. Открыть регистрацию.

`POST http://localhost:8080/admin/registrations`

```json
{
  "dictionary_code": "OwnershipType",
  "dictionary_name": "Вид собственности",
  "owner": "Имущество",
  "source": "Ведомственный перечень"
}
```

В ответе `registration_id` и пустой `files`. Пока регистрация открыта, тот же `dictionary_code` второй раз не занимается.

2. Начать запись файла профиля. `size` — размер файла в байтах.

`POST http://localhost:8080/admin/registrations/{registration_id}/files`

```json
{
  "files": [
    { "name": "ownership_type.sql", "size": 81 }
  ]
}
```

Файл — создание таблицы в исходном виде, без `version_id`:

```sql
CREATE TABLE ownership_type (value_code varchar NOT NULL, name varchar NOT NULL);
```

3. Записать это содержимое по `upload_url` из ответа. Состояние файла — `uploading`.

4. Подтвердить запись.

`POST http://localhost:8080/admin/registrations/{registration_id}/files/{file_id}/complete`

Ответ: `state = valid`. Пока есть `uploading` или `invalid`, завершение регистрации недоступно.

5. Завершить регистрацию.

`POST http://localhost:8080/admin/registrations/{registration_id}/commit`

Ответ — паспорт: `status = active`, `table_name = ownership_type`, `schema_name = null`, `versions` пуст. Строка версии не создаётся. Регистрация закрыта.

Проверка: `GET http://localhost:8080/admin/dictionaries/OwnershipType` возвращает этот паспорт.

### 2.2. Добавление данных

Черновик, заливка и публикация. До публикации сервис чтения эти строки не отдаёт.

1. Открыть черновик.

`POST http://localhost:8080/admin/dictionaries/OwnershipType/draft`

```json
{
  "delivery": "full"
}
```

Ответ: `version_id = 1`, `version_status = draft`, `files` пуст. Вместе с черновиком создаётся пустая секция этого номера.

2. Начать запись файла заполнения.

`POST http://localhost:8080/admin/dictionaries/OwnershipType/draft/files`

```json
{
  "files": [
    { "name": "ownership_type.sql", "size": 128 }
  ]
}
```

Файл заполняет исходную таблицу профиля:

```sql
INSERT INTO ownership_type (value_code, name) VALUES ('PRIVATE', 'Частная'), ('STATE', 'Государственная');
```

3. Записать содержимое по `upload_url`.

4. Подтвердить запись.

`POST http://localhost:8080/admin/dictionaries/OwnershipType/draft/files/{file_id}/complete`

Ответ: `state = valid`.

5. Завершить заливку.

`POST http://localhost:8080/admin/dictionaries/OwnershipType/draft/apply`

Файл переходит в `loaded`: строки лежат в секции черновика. `GET http://localhost:8080/admin/dictionaries/OwnershipType/draft` показывает этот статус.

6. Опубликовать.

`POST http://localhost:8080/admin/dictionaries/OwnershipType/draft/publish`

```json
{
  "valid_from": "2024-01-01"
}
```

Ответ: `version_status = active`, `valid_from = 2024-01-01`, `valid_to = null`. Это первая версия, поэтому `closed_version_id` и `closed_valid_to` пусты.

Проверка каталога: `GET http://localhost:8080/admin/dictionaries/OwnershipType` — одна версия, статус `active`.

### 2.3. Получение данных справочника

Читает `service.api`. Черновик не возвращается. В ответ входят `value_code` и `name` опубликованной версии.

Одно значение действующей версии:

`GET http://localhost:8082/dictionaries/OwnershipType/values/PRIVATE`

`version_id = 1`, `version_status = active`, `result = present`, `name = Частная`.

Код, которого в версии нет:

`GET http://localhost:8082/dictionaries/OwnershipType/values/PAPER`

`result = absent`, `name = null`. Тот же `version_id`.

Страница действующей версии, по возрастанию `value_code`:

`GET http://localhost:8082/dictionaries/OwnershipType/values?limit=100`

В `items` две строки: `PRIVATE` / «Частная», `STATE` / «Государственная». Конец списка — пустой `next_cursor`.

Та же редакция на дату внутри периода:

`GET http://localhost:8082/dictionaries/OwnershipType/values/PRIVATE?on_date=2024-06-01`

Снова версия `1` и `result = present`.

Дата раньше первой редакции:

`GET http://localhost:8082/dictionaries/OwnershipType/values/PRIVATE?on_date=2023-12-31`

`404`, `error = date_out_of_range`. Версия `1` не подставляется.

Проверка ссылок по номеру версии. Повтор кода возвращается один раз.

`POST http://localhost:8082/dictionaries/OwnershipType/values/check`

```json
{
  "version_id": 1,
  "value_codes": ["PRIVATE", "PAPER", "PRIVATE"]
}
```

В `items` две строки: `PRIVATE` — `present`, `PAPER` — `absent`.
