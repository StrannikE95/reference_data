# [На главную](../../../API.md)

# Описание

GET **/dictionaries/{dictionary_code}/columns**
Названия и типы колонок. Без `draft` это активная версия. С `draft=true` — черновик. После публикации черновика ответ тот же, что на отсутствующую версию. По этим `code` клиент собирает параметр `attribute`, в том числе для чтения значения на выбранный момент: колонка находится по `field_id`, даже если в старой строке у неё другой `code`.

# Входные параметры

+ `dictionary_code`:
    + path
    + Обязательно
    + `^[a-z][a-z0-9_]{0,62}$`
+ `draft`:
    + query
    + Не обязательно
    + `true` — колонки черновика

Тела нет.

# Поля ответа

+ `dictionary_code`
+ `columns`:
    + Массив в порядке паспорта
    + `code`
    + `name`
    + `type`
    + `default` — `true` только у колонки `code = name`
    + `required` — `true`, если значение этой колонки у элемента обязательно. У `name` всегда `true`

# Алгоритм работы

1. Проверить `dictionary_code`.
    + Нарушение:
        + Код: `invalid_request`
        + Статус: `400`
2. Найти паспорт по коду без учёта регистра.
    + Нет:
        + Код: `dictionary_not_found`
        + Статус: `404`
    + `status = inactive`:
        + Код: `dictionary_inactive`
        + Статус: `409`
3. Выбрать версию.
    + `draft=true` — версия `draft`. Её нет, в том числе после публикации:
        + Код: `draft_not_found`
        + Статус: `404`
        + Колонки активной версии в этот ответ не подставляются
    + иначе версия `active`. Её нет:
        + Код: `no_active_version`
        + Статус: `404`
4. Из `scheme` этой версии вернуть колонки как `columns`, без `field_id`. Статус `200`.

```mermaid
flowchart TD
  fmt{"Код разобран"} -->|нет| bad["400 invalid_request"]
  fmt -->|да| found{"Паспорт"}
  found -->|нет| miss["404 dictionary_not_found"]
  found -->|inactive| off["409 dictionary_inactive"]
  found -->|active| ver{"Есть нужная версия"}
  ver -->|нет| missVer["404 no_active_version или draft_not_found"]
  ver -->|да| ok["200"]
```

Метод не читает `dictionary_row` и не показывает, какие ключи уже лежат в `data`.
