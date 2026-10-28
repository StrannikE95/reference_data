# Функционал

| Проект | Контроллер | Метод | Описание |
| --- | --- | --- | --- |
| admin | MetadataController | `POST /admin/dictionaries` | [Создать справочник](api/management.api/MetadataController/post_dictionaries.md) |
| admin | MetadataController | `PATCH /admin/dictionaries/{dictionary_code}` | [Изменить название и владельца](api/management.api/MetadataController/patch_dictionary.md) |
| admin | MetadataController | `POST /admin/dictionaries/{dictionary_code}/versions` | [Создать черновик](api/management.api/MetadataController/post_version.md) |
| admin | MetadataController | `PATCH /admin/dictionaries/{dictionary_code}/versions/{version_id}` | [Изменить колонки черновика](api/management.api/MetadataController/patch_version.md) |
| admin | MetadataController | `POST /admin/dictionaries/{dictionary_code}/versions/{version_id}/publish` | [Опубликовать черновик](api/management.api/MetadataController/post_publish.md) |
| admin | MetadataController | `DELETE /admin/dictionaries/{dictionary_code}/versions/{version_id}` | [Удалить черновик](api/management.api/MetadataController/delete_version.md) |
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
|400|`selector_conflict`|Вместе переданы `draft` и `at`|-|
|404|`dictionary_not_found`|Справочник не найден|-|
|404|`no_active_version`|Нет активной версии|-|
|404|`draft_not_found`|Черновика нет|-|
|404|`version_not_found`|Версия не найдена|-|
|409|`dictionary_already_exists`|Код справочника уже занят|-|
|409|`draft_already_exists`|Черновик у справочника уже есть|-|
|409|`version_not_draft`|Действие доступно только для черновика|-|
|409|`only_draft`|Нельзя удалить черновик, пока нет активной версии|-|
|409|`reload_required`|Справочник не перезалит|После смены состава атрибутов или после того, как атрибут стал обязательным|
|409|`dictionary_inactive`|Справочник деактивирован|-|
|409|`invalid_valid_from`|Начало не позже открытого состояния|Новое начало не позже начала открытой строки. При публикации так же, если момент публикации не позже начала строки, которую публикация закрывает|
|422|`unknown_field`|Код колонки не входит в схему режима|Актуальное чтение и запись принимают код выбранной схемы. Чтение на момент принимает текущий код колонки и код этой колонки в схеме на этот момент|
|422|`invalid_value`|Значение не годится для колонки|На записи тип не совпал или обязательное значение пустое. На чтении без момента обязательный атрибут в строке пуст или значение не совпадает с типом выбранной схемы|
|500|`script_failed`|Секцию не удалось создать|-|
