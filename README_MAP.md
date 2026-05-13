# Карта DayZ [WIP]

> ⚠️ Функция в разработке. Базовый функционал работает.

## Требования

- **Microsoft Edge WebView2 Runtime** — обычно уже установлен в Windows 10/11.
  Скачать: https://developer.microsoft.com/microsoft-edge/webview2/
- Тайлы карты (не включены в репозиторий из-за размера)

## Структура файлов

```
assets/
  map.html          ← встроенная карта (в репо)
  leaflet.js        ← Leaflet локально (в репо)
  leaflet.css       ← Leaflet стили (в репо)
  maps/
    Namalsk-Top/    ← папка карты (НЕ в репо — добавь сам)
      map.json      ← конфиг карты
      tiles/
        7/
          0/0.webp
          ...
```

## Формат map.json

```json
{
  "name": "Namalsk Topographic",
  "id": "Namalsk-Top",
  "tileUrl": "tiles/{z}/{x}/{y}.webp",
  "minZoom": 0,
  "maxZoom": 7,
  "defaultZoom": 3,
  "worldSize": 12800,
  "tileSize": 256,
  "invertY": true
}
```

| Поле | Описание |
|------|----------|
| `worldSize` | Размер карты в единицах DayZ (Chernarus=15360, Namalsk=12800, Livonia=8192) |
| `maxZoom` | Максимальный зум (обычно 7) |
| `invertY` | TMS формат тайлов (y=0 снизу). Обычно `true` |
| `tileUrl` | Путь к тайлам относительно папки карты |

## Добавление новой карты

1. Создай папку `assets/maps/НазваниеКарты/`
2. Положи туда `map.json` с параметрами карты
3. Положи тайлы в `tiles/{z}/{x}/{y}.webp`
4. Скопируй в `bin/Debug/net8.0-windows/assets/maps/` (один раз)
5. Перезапусти приложение — карта появится в ComboBox

## Что планируется

- [ ] Клик для копирования координат
- [ ] Маркеры Stash/Loot позиций
- [ ] Поиск по координатам
- [ ] Сохранение пользовательских маркеров
