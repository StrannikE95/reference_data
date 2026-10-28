# [На главную](../../../API.md)

# Описание

PATCH **/admin/passports/{dictionary_code}**
Изменить название и владельца паспорта. Код, поля и таблица данных не меняются.

# Входные параметры

+ `dictionary_code`:
    + path
    + Обязательно
+ `dictionary_name`:
    + body
    + Обязательно
    + От 1 до 200 символов
+ `owner`:
    + body
    + Не обязательно
    + До 200 символов. Пустое значение очищает владельца

# Поля ответа

+ `dictionary_id`
+ `dictionary_code`
+ `dictionary_name`
+ `owner`
+ `status`

# Алгоритм работы

## Валидация

1. Проверить тело: `dictionary_name` передан, длина от 1 до 200, `owner` не длиннее 200, неизвестных полей нет.
    + Нарушение:
        + Код: `invalid_request`
        + Статус: `400`
2. Найти `dictionary` по переданному коду без учёта регистра.
    + Нет строки:
        + Код: `dictionary_not_found`
        + Статус: `404`

## Бизнес-логика

1. Записать `dictionary_name` и `owner`. `dictionary_code`, `status` и `JSON_metadata` не менять.
2. Вернуть статус `200`.

```mermaid
flowchart TD
  save["UPDATE dictionary_name и owner"] --> ok["200"]
```

Код справочника берётся из пути и остаётся прежним. Версии и секции этот вызов не трогает.
