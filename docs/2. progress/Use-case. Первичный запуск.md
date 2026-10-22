# Первичный запуск и простой справочник

Администратор — `management.api`, http://localhost:8080. Чтение — `service.api`, http://localhost:8082. Дата в запросе — календарный день `YYYY-MM-DD`.

## 1. Первичный запуск

База пересоздаётся, скрипты инициализации накатываются на пустой том, оба API поднимаются после неё.

Из корня репозитория:

```powershell
docker compose -f docker-compose/docker-compose.yml -f docker-compose/docker-compose.override.yml down -v
docker compose -f docker-compose/docker-compose.yml -f docker-compose/docker-compose.override.yml up -d --build
```

`down -v` удаляет том `pgdata`. Новый контейнер Postgres выполняет `db/sql/001_schema.sql`, затем `db/sql/002_seed.sql`. `management.api` и `service.api` стартуют, когда база принимает соединения.

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
