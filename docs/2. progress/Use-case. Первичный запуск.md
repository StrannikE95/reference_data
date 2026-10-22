# Первичный запуск и простой справочник

Администратор — `management.api`, http://localhost:8080. Чтение — `service.api`, http://localhost:8082. Дата в запросе — календарный день `YYYY-MM-DD`.

## 1. Первичный запуск

F5 на `management.api` в pre-build event сносит контейнер Postgres вместе с томом и поднимает новый пустой экземпляр. После старта процесса `management.api` сам выполняет `db/sql/001_schema.sql` и `db/sql/002_seed.sql`. `service.api` схему не создаёт.

Команда лежит в pre-build event проекта `management.api` (свойства проекта → Build → Events). Отдельный скрипт не нужен.

После старта каталог содержит два справочника начального наполнения.

`GET http://localhost:8080/admin/dictionaries`

В `items` есть `WallMaterial` (`table_name = wall_material`) и `Address` (`schema_name = address`).

`GET http://localhost:8082/dictionaries/WallMaterial/values/BRICK`

Ответ: версия `1`, `result = present`, `name = Кирпич`.

## 2. Подключение через DBeaver

База слушает порт хоста. В DBeaver это отдельное соединение, не адрес контейнера `postgres`.

1. Database → New Database Connection → PostgreSQL → Next.
2. Поля главного окна:

| Поле | Значение |
| --- | --- |
| Host | `localhost` |
| Port | `5432` |
| Database | `reference_data` |
| Username | `app` |
| Password | `app` |

3. Test Connection. Если драйвера PostgreSQL ещё нет, DBeaver предложит его скачать. Finish.

В дереве соединения видны схемы `public` и `address`. В `public`: `dictionary`, `dictionary_version`, секционированная `wall_material` и секция `wall_material_1`. В `address`: `addr_object`, `addr_house` и их секции версии `1`.
