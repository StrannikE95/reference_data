# Функционал

| Проект | Контроллер | Метод | Описание |
| --- | --- | --- | --- |
| admin | MetadataController | `POST /admin/passports` | [Создать справочник](api/management.api/MetadataController/post_passports.md) |
| admin | MetadataController | `PATCH /admin/passports/{dictionary_code}` | [Изменить паспорт](api/management.api/MetadataController/patch_passport.md) |
| admin | DataController | `GET /admin/passports` | [Получить список паспортов](api/management.api/DataController/get_passports.md) |
| admin | DataController | `GET /admin/passports/{dictionary_code}` | [Получить паспорт и версии](api/management.api/DataController/get_passport.md) |
| admin | DataController | `POST /admin/dictionaries/{dictionary_code}/versions` | [Создать черновик набора таблиц](api/management.api/DataController/post_versions.md) |
| admin | DataController | `GET /admin/dictionaries/{dictionary_code}/versions` | [Список редакций набора таблиц](api/management.api/DataController/get_versions.md) |
| admin | DataController | `GET /admin/dictionaries/{dictionary_code}/versions/{version_id}` | [Строки редакции](api/management.api/DataController/get_version.md) |
| admin | DataController | `PATCH /admin/dictionaries/{dictionary_code}/versions/{version_id}` | [Опубликовать черновик](api/management.api/DataController/patch_version.md) |
| admin | DataController | `DELETE /admin/dictionaries/{dictionary_code}/versions/{version_id}` | [Удалить черновик или раннюю редакцию](api/management.api/DataController/delete_version.md) |
| admin | DataController | `POST /admin/dictionaries/{dictionary_code}/rows` | [Записать состояние строки одной таблицы](api/management.api/DataController/post_rows.md) |
| admin | DataController | `GET /admin/dictionaries/{dictionary_code}/rows` | [Страница строк одной таблицы](api/management.api/DataController/get_rows.md) |
| service | DataController | `GET /dictionaries/{dictionary_code}/values/{value_code}` | [Одно значение на дату или редакцию](api/service.api/DataController/get_value.md) |
| service | DataController | `GET /dictionaries/{dictionary_code}/values` | [Страница значений](api/service.api/DataController/get_values.md) |
| service | DataController | `POST /dictionaries/{dictionary_code}/values/check` | [Проверить коды](api/service.api/DataController/post_check.md) |

# Коды ответов

|Код состояния (status)|Код ошибки (code)|Расшифровка|Описание|
|-|-|-|-|
|400|`invalid_request`|Некорректный запрос|-|
|400|`version_selector_conflict`|Одновременно переданы `on_date` и `version_id`|-|
|404|`dictionary_not_found`|Справочник не найден|-|
|404|`version_not_found`|Версия не найдена|-|
|404|`no_active_version`|Нет действующей версии|-|
|404|`date_out_of_range`|Дата не попала ни в один период|-|
|404|`table_not_found`|Таблицы нет в модели этой редакции|-|
|409|`dictionary_already_exists`|Код справочника уже занят|-|
|409|`dictionary_inactive`|Справочник не принимает изменения|-|
|409|`invalid_valid_from`|Дата начала действия не проходит проверку|-|
|409|`version_not_published`|Версия ещё черновик|-|
|409|`storage_mismatch`|Метод не относится к хранению этого справочника|-|
|409|`draft_exists`|Черновик уже есть|-|
|409|`schema_replaced`|Выпуск заменяет справочник целиком|-|
|409|`version_not_oldest`|Удалить можно только самую раннюю закрытую редакцию|-|
|413|`file_too_large`|Файл больше 100 МБ|-|
|415|`unsupported_media_type`|Допустим JSON|-|
|422|`empty_file`|Файл пуст|-|
|422|`invalid_file`|Файл не разобран|-|
|422|`unknown_field`|Колонка не описана в модели|-|
|422|`missing_field`|Нет колонки первичного ключа|-|
|422|`duplicate_value_code`|Повтор ключа строки|-|
|422|`invalid_value`|Значение не соответствует типу или обязательности|-|
|500|`script_failed`|SQL не выполнен|-|
