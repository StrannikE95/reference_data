# Функционал

| Контроллер | Метод | Описание |
| --- | --- | --- |
| MetadataController | `POST /admin/passports` | [Создать справочник и пустую таблицу](management.api/MetadataController/post_passports.md) |
| DataController | `GET /admin/passports` | [Получить список паспортов](management.api/DataController/get_passports.md) |
| DataController | `GET /admin/passports/{dictionary_code}` | [Получить паспорт, поля и версии](management.api/DataController/get_passport.md) |
| DataController | `POST /admin/dictionaries/{dictionary_code}/versions` | [Загрузить файл, оставить черновик](management.api/DataController/post_versions.md) |
| DataController | `POST /admin/dictionaries/{dictionary_code}/versions/{version_id}/publish` | [Опубликовать черновик](management.api/DataController/post_publish.md) |
| DataController | `GET /admin/dictionaries/{dictionary_code}/versions/{version_id}` | [Версия JSON-файлом](management.api/DataController/get_version.md) |
| DataController | `POST /admin/dictionaries/{dictionary_code}/versions/{version_id}/archive` | [Отсоединить секцию закрытой версии](management.api/DataController/post_archive.md) |
| DataController | `GET /dictionaries/{dictionary_code}/values/{value_code}` | [Одно значение выбранной редакции](service.api/DataController/get_value.md) |
| DataController | `GET /dictionaries/{dictionary_code}/values` | [Страница значений той же редакции](service.api/DataController/get_values.md) |
| DataController | `POST /dictionaries/{dictionary_code}/values/check` | [Проверить коды в указанной версии](service.api/DataController/post_check.md) |

# Коды ответов

|Код состояния (status)|Код ошибки (code)|Расшифровка|Описание|
|-|-|-|-|
|400|`invalid_request`|Некорректный запрос|-|
|400|`version_selector_conflict`|Одновременно переданы `on_date` и `version_id`|-|
|404|`dictionary_not_found`|Справочник не найден|-|
|404|`version_not_found`|Версия не найдена|-|
|404|`no_active_version`|Нет действующей версии|-|
|404|`date_out_of_range`|Дата не попала ни в один период|-|
|409|`dictionary_already_exists`|Код справочника уже занят|-|
|409|`dictionary_inactive`|Справочник не принимает новые версии|-|
|409|`invalid_valid_from`|Дата начала действия не проходит проверку|-|
|409|`version_not_published`|Версия ещё черновик|-|
|409|`version_not_ready`|Версию ещё нельзя скачать|-|
|409|`version_not_closed`|Версия не закрыта|-|
|410|`version_archived`|Секция версии отсоединена|-|
|413|`file_too_large`|Файл больше 100 МБ|-|
|415|`unsupported_media_type`|Допустимы CSV и JSON|-|
|422|`empty_file`|Файл пуст|-|
|422|`invalid_file`|Файл не разобран|-|
|422|`unknown_field`|Колонка не описана в полях справочника|-|
|422|`missing_field`|Нет обязательной колонки|-|
|422|`duplicate_value_code`|Повтор `value_code` внутри версии|-|
|422|`invalid_value`|Значение не соответствует типу или обязательности|-|
|500|`script_failed`|SQL не выполнен|-|
|500|`archive_failed`|Секцию не удалось отсоединить|-|
