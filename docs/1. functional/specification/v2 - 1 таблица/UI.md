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
- Колонка задаётся кодом, названием, типом, признаком «По умолчанию» и признаком «Обязательное». Признак «По умолчанию» стоит ровно у одной колонки. У необязательной колонки значение элемента можно оставить пустым.
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
  <button>Назад</button>
  <button>Вперед</button>
  <p>Выбор строки открывает экран состояний.</p>
  <div class="empty">Справочников пока нет</div>
</div>

- Выбрать строку — открыть экран состояний.
- «Паспорт» — открыть паспорт справочника.
- «Создать новый справочник» — открыть форму нового справочника.
- «Назад» — показать предыдущую страницу, если она есть.
- «Вперед» — показать следующую страницу, если она есть.

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
        <th>Тип</th>
        <th>Код</th>
        <th>Название</th>
        <th>По умолчанию</th>
        <th>Обязательное</th>
        <th>Действия</th>
      </tr>
    </thead>
    <tbody>
      <tr>
        <td>
          <select>
            <option>string</option>
            <option>integer</option>
            <option>number</option>
            <option>boolean</option>
          </select>
        </td>
        <td><input value="name"></td>
        <td><input value="Название"></td>
        <td><input type="radio" checked></td>
        <td><input type="checkbox" checked></td>
        <td></td>
      </tr>
      <tr>
        <td></td>
        <td></td>
        <td></td>
        <td></td>
        <td></td>
        <td><button>Добавить колонку</button></td>
      </tr>
    </tbody>
  </table>
  <button>Создать</button>
  <button>Назад</button>
  <div class="error">Код справочника уже занят</div>
</div>

- «Добавить колонку» — кнопка в нижней пустой строке, в столбце «Действия». Добавляет пустую строку колонки.
- «Создать» — сохранить справочник и открыть его паспорт. Пока колонка по умолчанию не одна, кнопка оставляет на форме сообщение.
- «Назад» — вернуться к списку без сохранения.

### Экран «Паспорт»

<div class="screen">
  <h1>Паспорт</h1>
  <div class="head">
    <div>Код: wall_material</div>
    <div>Версия 2: черновик. Активна версия 1 с 01.01.2024 00:00</div>
    <div>Состав атрибутов изменился или атрибут стал обязательным. Данные справочника нужно загрузить заново.</div>
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
        <th>Обязательное</th>
        <th>Действия</th>
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
        <td><input type="checkbox" checked></td>
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
        <td><input type="checkbox" checked></td>
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
        <td><input type="checkbox" checked></td>
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
        <td><input type="checkbox"></td>
        <td><button>Удалить</button></td>
      </tr>
      <tr>
        <td></td>
        <td></td>
        <td></td>
        <td></td>
        <td></td>
        <td><button>Добавить колонку</button></td>
      </tr>
    </tbody>
  </table>
  <button>Сохранить черновик</button>
  <button>Удалить черновик</button>
  <button>Опубликовать черновик</button>
  <button>Редактировать элементы черновика</button>
  <div class="error">Сначала загрузите данные справочника заново</div>
</div>

- «Добавить колонку» — кнопка в нижней пустой строке, в столбце «Действия». Добавляет пустую строку колонки.
- «Удалить» у колонки — убрать её из черновика. Опубликованные строки не меняются.
- Изменение кода сохранённой колонки — переименование. Изменение типа — смена типа.
- «Сохранить черновик» — записать название, владельца и состав колонок. Если черновика ещё нет, он открывается этой кнопкой. Для правки значений элементов кнопка не нужна. Если изменились состав, код или тип, уже записанные черновые значения сбрасываются. Если атрибут стал обязательным, записанные черновые значения остаются, но без перезаливки справочника публикация не выполняется. Смена названия колонки, признака по умолчанию и снятие обязательности перезаливку не требуют.
- «Удалить черновик» — убрать черновик и его значения. Активная версия остаётся. У нового справочника, у которого активной версии ещё нет, кнопка оставляет сообщение и черновик не удаляет.
- «Опубликовать черновик» — сделать черновик активной версией. Если состав атрибутов изменился или атрибут стал обязательным, а справочник не загружен заново, на экране сообщение и публикация не выполняется. После публикации пользователи видят новую схему. Отбор черновика показывает, что черновика нет.
- «Редактировать элементы черновика» — открыть экран состояний.

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
  <button>Назад</button>
  <button>Вперед</button>
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

- «Показать» — оставить строки по отбору и коду. «Черновик» показывает ещё не опубликованные строки. После публикации этот отбор сообщает, что черновика нет, и не показывает открытые строки. Пустой код показывает все коды страницы.
- «Назад» — показать предыдущую страницу, если она есть.
- «Вперед» — показать следующую страницу, если она есть.
- «Записать» — сохранить состояние элемента. Черновик для этой правки не открывается. Пока черновик метаданных уже открыт, состояние пишется в него, и действующая строка не закрывается: опубликованный справочник параллельно не правится. Если черновика нет, прежняя действующая строка получает конец, равный началу новой. Обязательные атрибуты заполнены, необязательный можно оставить пустым.
- «К паспорту» — вернуться к паспорту.
- У неактивного справочника форма нового состояния не показывается. Таблица состояний остаётся.
