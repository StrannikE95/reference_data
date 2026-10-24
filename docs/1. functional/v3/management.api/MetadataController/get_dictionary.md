# [На главную](../../API.md)

# Описание

GET **/admin/dictionaries/{dictionary_code}**
Паспорт, описание полей и версии справочника. Строк значений нет.

# Входные параметры

+ `dictionary_code`:
    + path
    + Обязательно

# Параметры ответа

+ `dictionary_id`
+ `dictionary_code`
+ `dictionary_name`
+ `owner` — может быть пустым
+ `source` — может быть пустым
+ `status`
+ `table_name`
+ `schema_name` — для простого списка пусто
+ `fields`:
    + Массив
    + `code`
    + `name`
    + `type`
    + `length`
    + `nullable`
+ `versions`:
    + Массив, по `version_id` по убыванию
    + `dictionary_version_id`
    + `version_id`
    + `status`
    + `valid_from` — у черновика может быть пустым
    + `valid_to` — заполнен только у закрытой версии
    + `archived`

# Алгоритм работы

1. Найти `dictionary` по коду без учёта регистра.
    + Нет строки:
        + Код: `dictionary_not_found`
        + Статус: `404`
2. Собрать `fields`, `table_name` и `schema_name` из `dictionary.JSON_metadata`.
3. Прочитать `dictionary_version` этого `dictionary_id`, сортировка `version_id` по убыванию.
4. Для каждой версии вычислить `archived`: секция `public.{table_name}_{version_id}` отсутствует.
5. Вернуть статус `200`. Секцию не читать.

```mermaid
flowchart TD
  find{"Паспорт по коду"} -->|нет| nf["404 dictionary_not_found"]
  find -->|да| meta["fields из JSON_metadata"]
  meta --> vers["Версии по version_id"]
  vers --> arch["archived по секции"]
  arch --> ok["200"]
```

`archived` считается по отсутствию секции `public.{table_name}_{version_id}`. Строки секции не читаются.
