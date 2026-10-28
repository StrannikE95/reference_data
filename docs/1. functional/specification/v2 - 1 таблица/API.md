# Функционал

| Проект | Контроллер | Метод | Описание |
| --- | --- | --- | --- |
| admin | MetadataController | `POST /admin/dictionaries` | [Создать справочник](api/management.api/MetadataController/post_dictionaries.md) |
| admin | MetadataController | `PATCH /admin/dictionaries/{dictionary_code}` | [Изменить паспорт и состав полей](api/management.api/MetadataController/patch_dictionary.md) |
| admin | MetadataController | `POST /admin/dictionaries/{dictionary_code}/deactivate` | [Деактивировать справочник](api/management.api/MetadataController/post_deactivate.md) |
| admin | DataController | `GET /admin/dictionaries` | [Список справочников](api/management.api/DataController/get_dictionaries.md) |
| admin | DataController | `GET /admin/dictionaries/{dictionary_code}` | [Паспорт и поля](api/management.api/DataController/get_dictionary.md) |
| admin | DataController | `GET /admin/dictionaries/{dictionary_code}/rows` | [Состояния значений](api/management.api/DataController/get_rows.md) |
| admin | DataController | `POST /admin/dictionaries/{dictionary_code}/rows` | [Записать новое состояние](api/management.api/DataController/post_row.md) |
| service | CatalogController | `GET /dictionaries` | [Активные справочники](api/service.api/CatalogController/get_dictionaries.md) |
| service | CatalogController | `GET /dictionaries/{dictionary_code}/columns` | [Названия и типы колонок](api/service.api/CatalogController/get_columns.md) |
| service | DataController | `GET /dictionaries/{dictionary_code}/values/{value_code}` | [Значение по коду](api/service.api/DataController/get_value.md) |

# Коды ответов

|Код состояния (status)|Код ошибки (code)|Расшифровка|Описание|
|-|-|-|-|
|400|`invalid_request`|Некорректный запрос|-|
|404|`dictionary_not_found`|Справочник не найден|-|
|409|`dictionary_already_exists`|Код справочника уже занят|-|
|409|`dictionary_inactive`|Справочник деактивирован|-|
|409|`invalid_valid_from`|Дата начала не позже открытого состояния|-|
|422|`unknown_field`|Код колонки не подходит к запрошенному режиму|Актуальное чтение принимает только открытое имя. Историческое чтение принимает имя, под которым значение лежит в найденной строке|
|422|`invalid_value`|Значение не соответствует типу колонки|При чтении без момента так выходит, если в открытой строке ещё значение прежнего типа|
|500|`script_failed`|Секцию не удалось создать|-|
