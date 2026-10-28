# UI администратора

Экраны администратора справочников в одной общей таблице.

<style>
.screen { font: 14px/1.4 sans-serif; color: #222; background: #ececec; border: 1px solid #bbb; padding: 16px 18px; margin: 8px 0 20px; }
.screen h1 { font-size: 18px; margin: 0 0 12px; }
.screen h2 { font-size: 16px; margin: 16px 0 8px; }
.screen table { border-collapse: collapse; width: 100%; margin: 12px 0; }
.screen th, .screen td { border: 1px solid #999; padding: 6px 8px; text-align: left; vertical-align: top; }
.screen button { margin: 0 8px 8px 0; }
.screen label { display: block; margin: 10px 0; }
.screen input, .screen select { margin-left: 8px; }
.screen .empty, .screen .dialog, .screen .error, .screen .head { border: 1px solid #999; padding: 12px; margin-top: 16px; }
.screen .head { margin-top: 0; margin-bottom: 12px; }
.screen .dialog { max-width: 420px; }
.screen .error { max-width: 520px; }
.screen .hint { color: #333; }
</style>

## Общие правила

- Справочник описывается полями «Код», «Название» и «Владелец».
- Код после создания не редактируется.
- Колонка задаётся кодом, названием, типом и признаком «По умолчанию». Такой признак стоит ровно у одной колонки.
- Состояния на экранах показываются как `ДД.ММ.ГГГГ ЧЧ:ММ`.
- Открытое состояние в колонке «Конец» показывает «—».
- Ошибка показывается текстом сообщения.

## Экраны

### Экран «Справочники»

<div class="screen">
  <h1>Справочники</h1>
  <table>
    <thead>
      <tr>
        <th>Код</th>
        <th>Название</th>
        <th>Владелец</th>
        <th>Состояние</th>
        <th></th>
      </tr>
    </thead>
    <tbody>
      <tr>
        <td>wall_material</td>
        <td>Материал стен</td>
        <td>Имущество</td>
        <td>активен</td>
        <td><button>Паспорт</button></td>
      </tr>
      <tr>
        <td>wall_kind</td>
        <td>Вид стен</td>
        <td>Имущество</td>
        <td>неактивен</td>
        <td><button>Паспорт</button></td>
      </tr>
    </tbody>
  </table>
  <button>Создать новый справочник</button>
  <button>Загрузить ещё</button>
  <p>Выбор строки открывает экран состояний.</p>
  <div class="empty">Справочников пока нет</div>
</div>

- Выбрать строку — открыть экран состояний.
- «Паспорт» — открыть паспорт справочника.
- «Создать новый справочник» — открыть форму нового справочника.
- «Загрузить ещё» — показать следующую страницу, если она есть.

### Экран «Новый справочник»

<div class="screen">
  <h1>Новый справочник</h1>
  <label>Код <input value="wall_kind"></label>
  <label>Название <input value="Вид стен"></label>
  <label>Владелец <input value="Имущество"></label>
  <h2>Колонки</h2>
  <table>
    <thead>
      <tr>
        <th>Код</th>
        <th>Название</th>
        <th>Тип</th>
        <th>По умолчанию</th>
      </tr>
    </thead>
    <tbody>
      <tr>
        <td><input value="name"></td>
        <td><input value="Название"></td>
        <td>
          <select>
            <option>string</option>
            <option>integer</option>
            <option>number</option>
            <option>boolean</option>
          </select>
        </td>
        <td><input type="radio" checked></td>
      </tr>
    </tbody>
  </table>
  <button>Добавить колонку</button>
  <button>Создать</button>
  <button>Назад</button>
  <div class="error">Код справочника уже занят</div>
</div>

- «Добавить колонку» — добавить пустую строку колонки.
- «Создать» — сохранить справочник и открыть его паспорт. Пока колонка по умолчанию не одна, кнопка оставляет на форме сообщение.
- «Назад» — вернуться к списку без сохранения.

### Экран «Паспорт»

<div class="screen">
  <h1>Паспорт</h1>
  <div class="head">
    <div>Код: wall_material</div>
    <div>Версия 2: черновик. Активна версия 1 с 01.01.2024 00:00</div>
  </div>
  <label>Название <input value="Материал стен"></label>
  <label>Владелец <input value="Имущество"></label>
  <h2>Колонки</h2>
  <table>
    <thead>
      <tr>
        <th>Код</th>
        <th>Название</th>
        <th>Тип</th>
        <th>По умолчанию</th>
        <th></th>
      </tr>
    </thead>
    <tbody>
      <tr>
        <td><input value="name"></td>
        <td><input value="Название"></td>
        <td>
          <select>
            <option>string</option>
          </select>
        </td>
        <td><input type="radio" checked></td>
        <td></td>
      </tr>
      <tr>
        <td><input value="kind_code"></td>
        <td><input value="Вид"></td>
        <td>
          <select>
            <option>string</option>
          </select>
        </td>
        <td><input type="radio"></td>
        <td><button>Удалить</button></td>
      </tr>
      <tr>
        <td><input value="is_bearing"></td>
        <td><input value="Несущая"></td>
        <td>
          <select>
            <option>boolean</option>
          </select>
        </td>
        <td><input type="radio"></td>
        <td><button>Удалить</button></td>
      </tr>
      <tr>
        <td><input value="density"></td>
        <td><input value="Плотность"></td>
        <td>
          <select>
            <option>number</option>
          </select>
        </td>
        <td><input type="radio"></td>
        <td><button>Удалить</button></td>
      </tr>
    </tbody>
  </table>
  <button>Добавить колонку</button>
  <button>Сохранить черновик</button>
  <button>Сделать активной</button>
  <button>Состояния</button>
  <button>Деактивировать</button>
  <button>Назад</button>
  <div class="dialog">
    <p>Справочник wall_material перестанет отдаваться пользователю. Записанные состояния сохранятся.</p>
    <button>Деактивировать</button>
    <button>Отмена</button>
  </div>
</div>

- «Добавить колонку» — добавить пустую строку колонки.
- «Удалить» — закрыть колонку. Уже записанные документы не меняются.
- Изменение кода сохранённой колонки — переименование. Изменение типа — смена типа. Документы при сохранении не переписываются.
- «Сохранить черновик» — записать название, владельца и колонки. Если черновика ещё нет, он создаётся. Активная версия остаётся прежней.
- «Сделать активной» — опубликовать черновик. После этого запросы без `draft` читают его.
- «Состояния» — открыть экран состояний.
- «Деактивировать» — открыть подтверждение. Подтверждение ставит состояние «неактивен».
- «Отмена» — закрыть подтверждение.
- «Назад» — вернуться к списку.

### Экран «Состояния»

<div class="screen">
  <h1>Состояния</h1>
  <div class="head">
    <div>wall_material — Материал стен</div>
    <div>Состояние справочника: активен</div>
  </div>
  <label>Отбор
    <select>
      <option>Только открытые</option>
      <option>Черновик</option>
      <option>На момент</option>
    </select>
  </label>
  <label>Момент <input type="datetime-local"></label>
  <label>Код <input value="BRICK"></label>
  <button>Показать</button>
  <table>
    <thead>
      <tr>
        <th>Код</th>
        <th>Название</th>
        <th>Вид</th>
        <th>Несущая</th>
        <th>Плотность</th>
        <th>Начало</th>
        <th>Конец</th>
      </tr>
    </thead>
    <tbody>
      <tr>
        <td>BRICK</td>
        <td>Кирпич керамический</td>
        <td>STONE</td>
        <td>да</td>
        <td>1800</td>
        <td>20.10.2026 00:00</td>
        <td>—</td>
      </tr>
      <tr>
        <td>TIMBER</td>
        <td>Брус</td>
        <td>WOOD</td>
        <td>нет</td>
        <td>500</td>
        <td>01.01.2024 00:00</td>
        <td>—</td>
      </tr>
      <tr>
        <td>STEEL</td>
        <td>Сталь</td>
        <td>METAL</td>
        <td>да</td>
        <td>7850</td>
        <td>20.10.2026 00:00</td>
        <td>—</td>
      </tr>
    </tbody>
  </table>
  <button>Загрузить ещё</button>
  <h2>Новое состояние</h2>
  <label>Код <input value="BRICK"></label>
  <label>Начало <input type="datetime-local" value="2026-10-20T00:00"></label>
  <label>Название <input value="Кирпич керамический"></label>
  <label>Вид <input value="STONE"></label>
  <label>Несущая
    <select>
      <option>да</option>
      <option>нет</option>
    </select>
  </label>
  <label>Плотность <input value="1800"></label>
  <button>Записать</button>
  <button>К паспорту</button>
  <div class="error">Начало не позже открытого состояния этого кода</div>
  <div class="empty">Открытых состояний нет</div>
</div>

- «Показать» — оставить строки по отбору и коду. «Черновик» показывает ещё не опубликованные строки. Пустой код показывает все коды страницы.
- «Загрузить ещё» — показать следующую страницу, если она есть.
- «Записать» — сохранить новое состояние. Прежняя открытая строка этого кода получает конец, равный началу новой.
- «К паспорту» — вернуться к паспорту.
- У неактивного справочника форма нового состояния не показывается. Таблица состояний остаётся.
