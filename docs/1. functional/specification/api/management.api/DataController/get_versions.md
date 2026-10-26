# [На главную](../../../API.md)

# Описание

GET **/admin/dictionaries/{dictionary_code}/versions**
Список версий справочника. Строк значений нет. Фильтр передаётся параметром запроса.

# Входные параметры

+ `dictionary_code`:
    + path
    + Обязательно
+ `status`:
    + query
    + Не обязательно
    + `draft`, `active`, `closed` или `archived`
    + Без параметра возвращаются все версии

# Поля ответа

+ `items`:
    + Массив, по `version_id` по убыванию
    + `dictionary_version_id`
    + `version_id`
    + `status`
    + `valid_from`
    + `valid_to`
    + `archived` — `true`, когда `status = archived`

# Алгоритм работы

## Валидация

1. Найти паспорт по переданному коду без учёта регистра.
    + Нет строки:
        + Код: `dictionary_not_found`
        + Статус: `404`
2. `status`, если передан, входит в список `draft`, `active`, `closed`, `archived`.
    + Нет в списке:
        + Код: `invalid_request`
        + Статус: `400`

## Бизнес-логика

1. Прочитать `dictionary_version` этого `dictionary_id`. При переданном `status` оставить только его. Сортировка `version_id` по убыванию.
2. Для каждой строки `archived = (status = archived)`.
3. Вернуть статус `200`. Секции не читать.

```mermaid
flowchart TD
  flt{"status передан"} -->|нет| all["Все версии"]
  flt -->|да| one["Версии этого status"]
  all --> ok["200"]
  one --> ok
```

`archived` берётся из записанного статуса, а не из наличия секции.
