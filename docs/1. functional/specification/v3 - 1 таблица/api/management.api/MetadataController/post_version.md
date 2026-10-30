# [На главную](../../../API.md)

# Описание

POST **/admin/dictionaries/{dictionary_code}/versions**
Открыть следующую версию схемы со статусом `draft`, чтобы менять схему действующего справочника. `scheme` копируется из активной версии. Строки и версия данных не создаются. Запись значения этот метод не заменяет. Если черновик схемы уже есть, второй не создаётся.

# Входные параметры

+ `X-Caller-Id`:
    + header
    + Обязательно
    + uuid

+ `dictionary_code`:
    + path
    + Обязательно
    + `^[a-z][a-z0-9_]{0,62}$`

Тела нет.

# Поля ответа

+ `dictionary_code`
+ `scheme_version_id`
+ `status` = `draft`
+ `valid_from` — пусто
+ `valid_to` — пусто
+ `scheme` — копия схемы активной версии:
    + `field_id`
    + `code`
    + `name`
    + `type`
    + `dictionary_code` — у колонки `reference`
    + `default`
    + `required`

# Алгоритм работы

## Валидация

1. Прочитать заголовок `X-Caller-Id`: uuid. Нет заголовка или значение не uuid:
    + Код: `invalid_request`
    + Статус: `400`
2. Найти `caller` по этому идентификатору. Строки нет либо роль не `admin`:
    + Код: `forbidden`
    + Статус: `403`
3. Проверить путь.
    + Нарушение:
        + Код: `invalid_request`
        + Статус: `400`
4. Найти паспорт по коду без учёта регистра.
    + Нет:
        + Код: `dictionary_not_found`
        + Статус: `404`

## Бизнес-логика

1. Версия `draft` уже есть:
    + Код: `draft_already_exists`
    + Статус: `409`
2. Версии `active` нет:
    + Код: `no_active_version`
    + Статус: `404`
3. Вставить версию схемы: новый `scheme_version_id`, `status = draft`, `valid_from` и `valid_to` пустые, `scheme` — схема активной версии, с теми же `field_id`. Версию данных и строки `dictionary_row` не создавать.
4. Вернуть статус `201`.

```mermaid
flowchart TD
  hasDraft{"Черновик уже есть"} -->|да| dup["409 draft_already_exists"]
  hasDraft -->|нет| hasActive{"Есть active"}
  hasActive -->|нет| noAct["404 no_active_version"]
  hasActive -->|да| ins["INSERT версия draft, копия scheme"]
  ins --> ok["201"]
```

Первый черновик схемы создаёт [post_dictionaries.md](post_dictionaries.md), а не этот метод. Колонки черновика меняются в [patch_version.md](patch_version.md). Копия содержит колонку `name`: `type = string`, `required = true`, `default = true`. Активная схема и опубликованные строки остаются. Запись значения открывает версию данных в [post_row.md](../DataController/post_row.md) и эту схему не закрывает. Полный набор данных включается только сменой состава или установкой обязательности. Деактивированный справочник черновик схемы открыть может.
