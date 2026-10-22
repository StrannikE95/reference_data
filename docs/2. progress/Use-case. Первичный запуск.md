# Первичный запуск и простой справочник

Администратор — `management.api`, http://localhost:8080. Чтение — `service.api`, http://localhost:8082. Дата в запросе — календарный день `YYYY-MM-DD`.

## 1. Первичный запуск

F5 на `management.api` в pre-build event сносит контейнер Postgres вместе с томом и поднимает новый пустой экземпляр. После старта процесса `management.api` сам выполняет `db/sql-init/001_schema.sql`. Скрипт создаёт пустые `dictionary` и `dictionary_version`. Справочники из `db/sql-references` при старте не заливаются. `service.api` схему не создаёт.

Команда лежит в pre-build event проекта `management.api` (свойства проекта → Build → Events). Отдельный скрипт не нужен.

После старта каталог пуст.

`GET http://localhost:8080/admin/dictionaries`

`items` пуст, `next_cursor` пуст.

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

В дереве соединения видна схема `public`: пустые `dictionary` и `dictionary_version`. Таблицы справочников появляются после скриптов из `Use-case. Простой справочник.md`.
