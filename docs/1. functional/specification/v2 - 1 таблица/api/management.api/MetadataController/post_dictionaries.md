# [На главную](../../../API.md)

# Описание

POST **/admin/dictionaries**
Создать паспорт, пустую секцию и версию `1` со статусом `draft`. Активной версии нет. Строки значений не создаются.

# Входные параметры

+ `dictionary_code`:
    + body
    + Обязательно
    + `^[a-z][a-z0-9_]{0,62}$`
    + Уникален без учёта регистра
    + После создания не меняется
+ `dictionary_name`:
    + body
    + Обязательно
    + От 1 до 200 символов
+ `owner`:
    + body
    + Не обязательно
    + До 200 символов, по умолчанию пусто
+ `fields`:
    + body
    + Обязательно
    + От 1 до 50 колонок
    + Ровно одна колонка с `default = true`
    + Одинаковые `code` нельзя
+ `fields[].code`:
    + Обязательно
    + `^[a-z][a-z0-9_]{0,62}$`
    + Не `value_code`, `valid_from`, `valid_to`
+ `fields[].name`:
    + Обязательно
    + От 1 до 200 символов
+ `fields[].type`:
    + Обязательно
    + `string`, `integer`, `number`, `boolean`
+ `fields[].default`:
    + Обязательно
    + `true` или `false`

# Поля ответа

+ `dictionary_id`
+ `dictionary_code`
+ `dictionary_name`
+ `owner`
+ `status` = `active`
+ `version_id` = `1`
+ `version_status` = `draft`
+ `fields` — с назначенными `field_id`

# Алгоритм работы

## Валидация

1. Проверить тело: шаблон `dictionary_code`, длины, состав `fields`, шаблон `code`, запрещённые имена, уникальность `code`, ровно один `default = true`, не больше 50 колонок, неизвестных полей нет.
    + Нарушение:
        + Код: `invalid_request`
        + Статус: `400`

## Бизнес-логика

1. Проверить уникальность `dictionary_code` без учёта регистра.
    + Код занят:
        + Код: `dictionary_already_exists`
        + Статус: `409`
2. Начать транзакцию.
3. Вставить строку в `dictionary`: новый `dictionary_id`, `status = active`.
4. Вставить `dictionary_version`: `version_id = 1`, `status = draft`, `valid_from` и `valid_to` пустые. Каждой колонке назначить `field_id`.
5. Создать секцию `dictionary_row_{dictionary_code}` для `FOR VALUES IN (dictionary_code)`.
    + Ошибка выполнения:
        + Код: `script_failed`
        + Статус: `500`
        + Транзакция откатывается
6. Зафиксировать транзакцию. Вернуть статус `201`.

```mermaid
flowchart TD
  uniq{"Код свободен"} -->|нет| exists["409 dictionary_already_exists"]
  uniq -->|да| row["INSERT dictionary и версия draft"]
  row --> part["CREATE PARTITION"]
  part -->|ошибка| bad["500 script_failed"]
  part -->|да| ok["COMMIT и 201"]
```

Секция создаётся пустой. Колонки описаны в черновике и ещё не действуют для запросов без `draft`. Активной схема становится через [post_publish.md](post_publish.md).
