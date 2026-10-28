# UI администратора

Экраны администратора справочников. У справочника одно хранение: «Одна таблица» или «Набор таблиц».

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

- Справочник описывается полями «Код», «Название», «Владелец» и «Хранение».
- Хранение выбирается при создании и дальше не меняется.
- «Одна таблица» хранит значения интервалами. Список полей меняется в паспорте и не создаёт колонки. Значение поля базой не проверяется.
- «Набор таблиц» хранит текущие строки в своих таблицах. Прошлая форма справочника — модель редакции. В историю попадает строка, которую заменили или удалили.
- Код после создания не редактируется.
- Даты на экранах показываются как `ДД.ММ.ГГГГ`.
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
        <th>Хранение</th>
        <th>Редакция</th>
        <th></th>
      </tr>
    </thead>
    <tbody>
      <tr>
        <td>WallMaterial</td>
        <td>Материал стен</td>
        <td>Имущество</td>
        <td>Набор таблиц</td>
        <td>2</td>
        <td><button>Паспорт</button></td>
      </tr>
      <tr>
        <td>WallKind</td>
        <td>Вид стен</td>
        <td>Имущество</td>
        <td>Одна таблица</td>
        <td>—</td>
        <td><button>Паспорт</button></td>
      </tr>
    </tbody>
  </table>
  <button>Создать новый справочник</button>
  <button>Загрузить ещё</button>
  <p>Выбор строки открывает редакции набора таблиц или строки одной таблицы.</p>
  <div class="empty">Справочников пока нет</div>
</div>

- Выбрать «Набор таблиц» — открыть экран редакций.
- Выбрать «Одна таблица» — открыть экран строк.
- «Паспорт» — открыть окно паспорта.
- «Создать новый справочник» — открыть форму нового справочника.
- «Загрузить ещё» — показать следующую страницу, если она есть.

### Экран «Новый справочник»

<div class="screen">
  <h1>Новый справочник</h1>
  <label>Название * <input value="Материал стен"></label>
  <label>Код * <input value="WallMaterial"></label>
  <label>Владелец <input value="Имущество"></label>
  <label>Хранение *
    <select>
      <option>Набор таблиц</option>
      <option>Одна таблица</option>
    </select>
  </label>
  <h2>Таблицы</h2>
  <table>
    <thead>
      <tr>
        <th>Таблица</th>
        <th>Колонка</th>
        <th>Тип</th>
        <th>Ключ</th>
        <th></th>
      </tr>
    </thead>
    <tbody>
      <tr>
        <td>wall_kind</td>
        <td>kind_code</td>
        <td>Строка</td>
        <td>да</td>
        <td><button>Удалить</button></td>
      </tr>
      <tr>
        <td>wall_kind</td>
        <td>name</td>
        <td>Строка</td>
        <td></td>
        <td><button>Удалить</button></td>
      </tr>
      <tr>
        <td>wall_material</td>
        <td>value_code</td>
        <td>Строка</td>
        <td>да</td>
        <td><button>Удалить</button></td>
      </tr>
      <tr>
        <td>wall_material</td>
        <td>name</td>
        <td>Строка</td>
        <td></td>
        <td><button>Удалить</button></td>
      </tr>
    </tbody>
  </table>
  <button>Добавить колонку</button>
  <h2>Связь</h2>
  <p class="hint">wall_material.kind_code → wall_kind.kind_code</p>
  <button>Добавить связь</button>
  <div>
    <button>Создать</button>
    <button>Отмена</button>
  </div>
</div>

- «Одна таблица» заменяет блок таблиц и связей списком полей. Поля описывают документ значения и не становятся колонками.
- «Добавить колонку» — пустая строка выбранной таблицы.
- «Добавить связь» — пара таблиц и список колонок. Составная связь — несколько колонок в одной связи.
- «Удалить» — убрать колонку до создания.
- «Создать» — создать паспорт. Для набора таблиц создаются пустые таблицы, редакции ещё нет. Для одной таблицы создаётся секция общей таблицы.
- «Отмена» — вернуться к списку без сохранения.

### Экран «Редакции справочника»

<div class="screen">
  <h1>Редакции справочника</h1>
  <div class="head">
    <div>Название: Материал стен</div>
    <div>Код: WallMaterial</div>
    <div>Хранение: Набор таблиц</div>
  </div>
  <button>Загрузить новую редакцию</button>
  <button>Паспорт</button>
  <button>К списку</button>
  <table>
    <thead>
      <tr>
        <th>№</th>
        <th>Статус</th>
        <th>Дата С</th>
        <th>Дата ПО</th>
        <th>Действия</th>
      </tr>
    </thead>
    <tbody>
      <tr>
        <td>3</td>
        <td>Черновик</td>
        <td>01.11.2026</td>
        <td>—</td>
        <td>
          <button>Опубликовать</button>
          <button>Удалить</button>
          <button>Найти</button>
        </td>
      </tr>
      <tr>
        <td>2</td>
        <td>Действует</td>
        <td>20.10.2026</td>
        <td>—</td>
        <td><button>Найти</button></td>
      </tr>
      <tr>
        <td>1</td>
        <td>Закрыта</td>
        <td>01.01.2024</td>
        <td>19.10.2026</td>
        <td>
          <button>Найти</button>
          <button>Удалить редакцию</button>
        </td>
      </tr>
    </tbody>
  </table>
  <div class="dialog">
    <h2>Удалить черновик 3?</h2>
    <button>Удалить</button>
    <button>Отмена</button>
  </div>
  <div class="error">Этот выпуск заменяет справочник целиком. Создайте новый справочник.</div>
</div>

- «Загрузить новую редакцию» — диалог загрузки. Пока черновик есть, файл не отправляется.
- «Опубликовать» — только черновик. Он становится действующим, прежняя действующая редакция закрывается по 19.10.2026 в примере редакции 2. Неизменившиеся строки остаются в живых таблицах.
- Сообщение о замене справочника целиком оставляет черновик на месте.
- «Удалить» — только черновик, после подтверждения.
- «Удалить редакцию» — только самая ранняя закрытая. Живые таблицы не меняются, даты этой редакции больше не читаются.
- «Найти» — экран строк этой редакции.
- «Паспорт» — окно паспорта.
- «К списку» — вернуться к справочникам.

### Окно «Паспорт»

<div class="screen">
  <h1>Паспорт</h1>
  <label>Код <input value="WallKind" readonly></label>
  <label>Название <input value="Вид стен"></label>
  <label>Владелец <input value="Имущество"></label>
  <label>Хранение <input value="Одна таблица" readonly></label>
  <h2>Поля значения</h2>
  <table>
    <thead>
      <tr>
        <th>Код</th>
        <th>Название</th>
        <th>Тип</th>
        <th></th>
      </tr>
    </thead>
    <tbody>
      <tr>
        <td><input value="name"></td>
        <td><input value="Название"></td>
        <td>
          <select>
            <option>Строка</option>
          </select>
        </td>
        <td><button>Удалить</button></td>
      </tr>
    </tbody>
  </table>
  <button>Добавить поле</button>
  <div>
    <button>Сохранить</button>
    <button>Отмена</button>
  </div>
</div>

Открывается действием «Паспорт». Для набора таблиц списка полей нет: название и владелец сохраняются, таблицы и связи меняются новой редакцией.

- «Добавить поле» — пустая строка списка. Появляется только у хранения «Одна таблица».
- «Сохранить» — записать название, владельца и, для одной таблицы, список полей. Уже записанные значения не перепроверяются.
- «Отмена» — закрыть окно без сохранения.

### Диалог «Загрузка новой редакции»

<div class="screen">
  <h1>Загрузка новой редакции</h1>
  <label>Дата начала * <input type="date" value="2026-11-01"></label>
  <label>Файл модели и строк * <input type="file" accept=".json,application/json"></label>
  <p class="hint">В файле полная модель и строки таблиц. Таблица без строк в файле не меняется. Пустой список удаляет строки таблицы.</p>
  <div>
    <button>Загрузить</button>
    <button>Отмена</button>
  </div>
  <div class="error">Черновик уже есть. Удалите его, затем загрузите файл снова.</div>
</div>

- «Загрузить» — создать черновик. Живые таблицы в этот момент не меняются.
- «Отмена» — закрыть диалог.
- После успеха у новой строки есть «Опубликовать», «Удалить» и «Найти».

### Экран «Строки редакции»

<div class="screen">
  <h1>Редакция 2 · Действует · с 20.10.2026</h1>
  <div>Материал стен, WallMaterial</div>
  <label>Таблица
    <select>
      <option>wall_material</option>
      <option>wall_kind</option>
    </select>
  </label>
  <p>
    <input placeholder="код или подпись" value="кир">
    <button>Найти</button>
    <button>Очистить</button>
  </p>
  <table>
    <thead>
      <tr>
        <th>Код</th>
        <th>Подпись</th>
        <th>Вид</th>
        <th>Несущая</th>
        <th>Плотность</th>
      </tr>
    </thead>
    <tbody>
      <tr>
        <td>BRICK</td>
        <td>Кирпич керамический</td>
        <td>STONE</td>
        <td>да</td>
        <td>1800</td>
      </tr>
      <tr>
        <td>TIMBER</td>
        <td>Брус</td>
        <td>WOOD</td>
        <td>нет</td>
        <td>500</td>
      </tr>
      <tr>
        <td>STEEL</td>
        <td>Сталь</td>
        <td>METAL</td>
        <td>да</td>
        <td>7850</td>
      </tr>
    </tbody>
  </table>
  <button>Загрузить ещё</button>
  <button>Назад</button>
</div>

Открывается действием «Найти». Для редакции 1 на 01.01.2024 у кирпича подпись «Кирпич», колонки «Вид» нет, стали и металла нет. Брус в обеих редакциях один.

- «Найти» — оставить строки, где код или подпись содержит текст.
- «Очистить» — страница без фильтра.
- Смена таблицы показывает её строки на ту же редакцию.
- «Назад» — вернуться к редакциям.

### Экран «Строки справочника»

<div class="screen">
  <h1>Вид стен</h1>
  <div class="head">
    <div>Код: WallKind</div>
    <div>Хранение: Одна таблица</div>
  </div>
  <label>На дату <input type="date" value="2026-10-20"></label>
  <button>Показать</button>
  <button>Новое состояние</button>
  <button>Паспорт</button>
  <table>
    <thead>
      <tr>
        <th>Код</th>
        <th>Название</th>
        <th>Родитель</th>
        <th>Дата С</th>
        <th>Дата ПО</th>
      </tr>
    </thead>
    <tbody>
      <tr>
        <td>STONE</td>
        <td>Камень</td>
        <td>—</td>
        <td>01.01.2024</td>
        <td>—</td>
      </tr>
      <tr>
        <td>WOOD</td>
        <td>Дерево</td>
        <td>—</td>
        <td>01.01.2024</td>
        <td>—</td>
      </tr>
      <tr>
        <td>METAL</td>
        <td>Металл</td>
        <td>—</td>
        <td>20.10.2026</td>
        <td>—</td>
      </tr>
    </tbody>
  </table>
  <button>К списку</button>
</div>

- «Показать» — состояния, действующие на выбранную дату. Пустая дата показывает открытые состояния.
- «Новое состояние» — диалог записи. Прежнее открытое состояние этого кода закрывается предыдущим днём.
- «Паспорт» — окно паспорта со списком полей.
- «К списку» — вернуться к справочникам.

### Диалог «Новое состояние»

<div class="screen">
  <h1>Новое состояние</h1>
  <label>Код * <input value="CONCRETE"></label>
  <label>Родитель <input placeholder="код в общей таблице"></label>
  <label>Дата начала * <input type="date" value="2026-10-20"></label>
  <label>Название <input value="Бетон"></label>
  <div>
    <button>Записать</button>
    <button>Отмена</button>
  </div>
</div>

- «Записать» — открыть состояние с этой даты. Родитель не проверяется: его имя на ту же дату ищется по коду в общей таблице.
- «Отмена» — закрыть диалог без записи.
