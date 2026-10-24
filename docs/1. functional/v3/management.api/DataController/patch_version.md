# [На главную](../../API.md)

# Описание

PATCH **/admin/dictionaries/{dictionary_code}/versions/{version_id}**
Сменить статус версии. Допустимые переходы: `draft` → `active`, `active` → `closed`, `closed` → `archived`.

# Входные параметры

+ `dictionary_code`:
    + path
    + Обязательно
+ `version_id`:
    + path
    + Обязательно
+ `status`:
    + body
    + Обязательно
    + `active`, `closed` или `archived`
+ `valid_to`:
    + body
    + Обязательно при `status = closed`
    + Дата `YYYY-MM-DD`
    + При `active` и `archived` не передаётся

# Параметры ответа

Переход в `active`:

+ `version_id`
+ `status` = `active`
+ `valid_from`
+ `valid_to` — пусто
+ `closed_version_id` — пусто, если закрывать было нечего
+ `closed_valid_to` — пусто, если закрывать было нечего

Переход в `closed`:

+ `version_id`
+ `status` = `closed`
+ `valid_from`
+ `valid_to` — дата из тела

Переход в `archived`:

+ `version_id`
+ `status` = `archived`
+ `valid_from`
+ `valid_to`

# Алгоритм работы

1. Проверить тело.
    + `status` не из трёх значений, лишнее поле или нет `valid_to` при `closed`:
        + Код: `invalid_request`
        + Статус: `400`
2. Начать транзакцию. Заблокировать строку `dictionary` через `SELECT ... FOR UPDATE`.
3. Нет паспорта:
    + Код: `dictionary_not_found`
    + Статус: `404`
4. `status` паспорта не `active`:
    + Код: `dictionary_inactive`
    + Статус: `409`
5. Найти `dictionary_version` по `dictionary_id` и `version_id`.
    + Нет строки:
        + Код: `version_not_found`
        + Статус: `404`
6. `status = active` в теле. Версия не `draft`:
    + Статус: `409`
7. `status = active` в теле. Есть другая версия со `status = active`:
    + Закрыть её в этой транзакции: `status = closed`, `valid_to = valid_from` публикуемой версии минус 1 день
    + Её номер — `closed_version_id`, дата — `closed_valid_to`
    + Действующей нет — оба поля ответа пустые
8. `status = active` в теле. Перевести черновик в `active`. `valid_from` не менять. `valid_to` оставить пустым. Секцию и строки не переписывать.
9. `status = closed` в теле. Версия не `active`:
    + Статус: `409`
10. `status = closed` в теле. Записать `status = closed` и `valid_to` из тела. Секцию не отсоединять. Следующей версии нет: дата конца приходит от клиента.
11. `status = archived` в теле. Версия не `closed`:
    + Код: `version_not_closed`
    + Статус: `409`
12. `status = archived` в теле. Записать `status = archived` и выполнить `ALTER TABLE "{table_name}" DETACH PARTITION "{table_name}_{version_id}"`.
    + Ошибка `DETACH`:
        + Код: `archive_failed`
        + Статус: `500`
        + Транзакция откатывается, статус остаётся `closed`
13. Зафиксировать транзакцию. Вернуть статус `200`. Свой SQL-файл не создавать.

```mermaid
flowchart TD
  body{"status в теле"} -->|нет| bad["400 invalid_request"]
  body -->|да| lock["Транзакция и FOR UPDATE"]
  lock --> pas{"Паспорт и версия"}
  pas -->|нет паспорта| d404["404 dictionary_not_found"]
  pas -->|паспорт не active| ina["409 dictionary_inactive"]
  pas -->|нет версии| v404["404 version_not_found"]
  pas -->|да| to{"Целевой status"}
  to -->|active| fromD{"Сейчас draft"}
  fromD -->|нет| d409["409"]
  fromD -->|да| close["Прежнюю active закрыть"]
  close --> act["Эта версия active"]
  to -->|closed| fromA{"Сейчас active"}
  fromA -->|нет| c409["409"]
  fromA -->|да| shut["status closed и valid_to из тела"]
  to -->|archived| fromC{"Сейчас closed"}
  fromC -->|нет| nc["409 version_not_closed"]
  fromC -->|да| det["status archived и DETACH"]
  det -->|ошибка| fail["500 archive_failed"]
  act --> ok["COMMIT и 200"]
  shut --> ok
  det -->|да| ok
```

В `active` переводится только черновик. Прежняя действующая закрывается в той же транзакции, дату конца считает сервер. `closed` без новой версии выключает справочник для новых заявок, история по номеру и дате остаётся. `archived` пишется в каталог и одновременно отсоединяет секцию. Повторный переход из того же статуса — `409`.
