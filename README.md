# GM Mod Config Editor

Редактор конфигурационных файлов для серверных модов DayZ. Позволяет редактировать рецепты крафта, квесты, NPC, пресеты лута, настройки медицины, еды и майнинга без ручного редактирования JSON.

![Version](https://img.shields.io/badge/version-1.5.0-teal)
![Platform](https://img.shields.io/badge/platform-Windows-blue)
![Framework](https://img.shields.io/badge/.NET-8.0-purple)

---

## Возможности

### Крафт
- Редактор рецептов с автодополнением из справочника предметов
- Поддержка инструментов, наборов, спец. предметов, чертежей
- Импорт classnames из Workshop модов (автоматический парсер PBO)

### Квест система
- Редактор квестов: цели, награды, репутация, расписание
- Редактор NPC: позиции, диалоги, квесты, экипировка
- Пресеты лута: схроны, тиры, триггеры, маппинг

### Конфиги модов
- **GM Config** — категории крафтстолов, доступ, список админов
- **Medicine Config** — настройки болезней, навыков, алкоголя
- **Ability Config** — система навыков и очков
- **Liquid Config** — баффы/дебаффы от еды и жидкостей
- **Food Config** — рецепты готовки, FryingPan, IRP предметы
- **MineRock Config** — добыча ресурсов из камней
- **Rock Points** — позиции точек добычи

### Инструменты
- 🔍 **Глобальный поиск** (`Ctrl+F`) по всем разделам
- ✓ **Валидация** (`Ctrl+E`) — проверка ошибок с экспортом в TXT
- ↩️ **Undo/Redo** (`Ctrl+Z` / `Ctrl+Y`)
- 🗺️ **Интерактивная карта** DayZ с маркерами NPC, триггеров и камней
- 💾 **Автосохранение** с debounce
- 🌙 **Тёмная и светлая тема**
- 🌐 **Локализация** — русский и английский интерфейс

---

## Установка

### Готовый exe (рекомендуется)

1. Скачайте последний релиз из [Releases](../../releases)
2. Распакуйте архив в любую папку
3. Запустите `GMCraftTableEditor.exe`
4. При первом запуске укажите папку вашего DayZ проекта (где лежит `profiles` или папка с `GM_Config`)

> Python и скрипты для парсера Workshop распаковываются автоматически при первом запуске.

### Сборка из исходников

```bash
git clone https://github.com/YOUR_USERNAME/YOUR_REPO.git
cd YOUR_REPO
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o "./publish"
```

**Требования:** .NET 8 SDK, Windows 10/11

---

## Быстрый старт

1. Запустите приложение
2. Нажмите **📁** или при первом запуске появится окно выбора пути
3. Укажите папку проекта — приложение автоматически найдёт `GM_Config` и загрузит все файлы
4. Редактируйте нужные разделы в левом меню
5. Сохраняйте через кнопки в верхней панели или `Ctrl+S`

### Структура GM_Config

```
profiles/
  GM_Config/
    CraftTable/
      GM_CRAFTTABLE_CONFIG.json
      GM_ACCESS_CONFIG.json
      Recipes/
        craft_recipes.json
        ...
    QuestSystem/
      GM_QuestSystemCFG.json
      GM_PRESET_CONFIG.json
      Quests/
        id_1.json
        ...
    Ability/
      GM_ABILITY_CONFIG.json
      GM_MEDICINE_CONFIG.json
    Food/
      GM_FoodConfig.json
      GM_LiquidConfig.json
    MineRock/
      GM_MineRockConfig.json
      ROCK_POINTS.json
```

---

## Импорт classnames из Workshop

1. Перейдите в **Справочник предметов**
2. Нажмите **Импорт classnames из Workshop**
3. Укажите папку `!Workshop` (например `DayZ\!Workshop`)
4. Выберите нужные моды и нажмите **Сканировать выбранные**

Сканер автоматически читает PBO файлы и извлекает classnames с `scope=2`.

---

## Интерактивная карта

Подробная документация: [README_MAP.md](README_MAP.md)

Требуется **Microsoft Edge WebView2 Runtime** (обычно уже установлен в Windows 10/11).

---

## Поддерживаемые моды

Редактор работает с конфигурационными файлами следующих модов:
- **GM_CraftTable** — система крафта
- **GM_QuestSystem** — квесты и NPC
- **GM_Ability / GM_Medicine** — система навыков и медицины
- **GM_Food / GM_Liquid** — еда и жидкости
- **GM_MineRock** — добыча ресурсов

---

## Технологии

- **WPF** (.NET 8, C#)
- **Python** (embeddable) — парсер Workshop PBO файлов
- **Leaflet.js** — интерактивная карта
- **Microsoft Edge WebView2** — отображение карты

---

## Changelog

Подробный список изменений: [CHANGELOG.md](CHANGELOG.md)

---

## Лицензия

Проект распространяется для использования администраторами DayZ серверов.
