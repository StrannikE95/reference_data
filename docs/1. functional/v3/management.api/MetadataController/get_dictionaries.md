# [На главную](../../API.md)

# Описание

GET **/admin/dictionaries**
Страница паспортов. Строк значений нет.

# Входные параметры

+ `limit`:
    + query
    + Не обязательно
    + integer, от 1 до 1000, по умолчанию 100
+ `cursor`:
    + query
    + Не обязательно
    + Непрозрачная строка предыдущего ответа

# Параметры ответа

+ `items`:
    + Массив
    + `dictionary_id`
    + `dictionary_code`
    + `dictionary_name`
    + `owner` — может быть пустым
    + `source` — может быть пустым
    + `status`
    + `active_version_id` — номер версии со статусом `active` или пусто
+ `next_cursor` — пусто, если страниц больше нет

# Алгоритм работы

1. Проверить `limit` и `cursor`.
    + Если `limit` вне 1…1000 или `cursor` не разбирается:
        + Код: `invalid_request`
        + Статус: `400`
2. Прочитать страницу `dictionary` в порядке `dictionary_code`.
3. Для каждой строки найти версию со `status = active`.
    + Есть — отдать её `version_id` как `active_version_id`
    + Нет — `active_version_id` пустой
4. Если страниц больше нет, `next_cursor` пустой.
5. Вернуть статус `200`.
