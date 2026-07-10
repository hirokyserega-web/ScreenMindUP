# ScreenMind

ScreenMind — персональный AI-помощник для Windows 10/11. По явному действию пользователя приложение захватывает активное окно, монитор или выделенную область, обрабатывает изображение только в памяти, отправляет его выбранной мультимодальной модели и показывает потоковый ответ в компактном оверлее.

## Принципы приватности

- Захват выполняется только после явного действия пользователя.
- Снимки, запросы, ответы и история не сохраняются между запусками и не попадают в логи.
- API-ключи и токены не входят в JSON-конфигурацию: Windows-сборка хранит их в Windows Credential Manager через `ISecretStore`.
- Значение секрета возвращается как immutable `string`: CLR не позволяет гарантированно очистить его память. Реализация не кэширует секреты и минимизирует время их жизни; вызывающий код обязан удерживать значение только на время запроса.
- Поддерживается только официальная API/OAuth-аутентификация; cookies, пароли и браузерные сессии не используются.
- Исключение собственных окон из захвата через `WDA_EXCLUDEFROMCAPTURE` будет best-effort, а не гарантией абсолютной невидимости.

## Стек и архитектура

C# / .NET 8 LTS, Avalonia UI 11 и MVVM, `Microsoft.Extensions.*`, HttpClientFactory, `System.Text.Json`, xUnit, FluentAssertions и WinAPI/P/Invoke. `ScreenMind.Core` не зависит от UI, Windows, файловой системы, HTTP SDK или провайдеров; UI не вызывает WinAPI напрямую.

## Структура

- `src/ScreenMind.Core` — модели, контракты, настройки и state machine.
- `src/ScreenMind.Infrastructure` — версионированная JSON-конфигурация, миграции и recovery.
- `src/ScreenMind.Platform.Windows` — Windows Credential Manager и последующие WinAPI-адаптеры.
- `src/ScreenMind.UI`, `src/ScreenMind.App` — Avalonia UI и composition root.
- `src/ScreenMind.AI`, `src/ScreenMind.Providers.*` — будущие оркестратор и провайдеры.
- `tests/*` — unit, integration и Windows integration tests.

## Сборка и проверки

Требуется .NET SDK 8.0.

```bash
dotnet restore
dotnet format --verify-no-changes
dotnet build -c Release --no-restore
dotnet test -c Release --no-build
```

Если форматирование требует изменений:

```bash
dotnet format
dotnet format --verify-no-changes
```

## Конфигурация

Несекретные настройки находятся в `%LOCALAPPDATA%\ScreenMind\settings.json`; рядом поддерживаются валидный `settings.json.bak`, уникальные временные файлы и quarantine-копии повреждённого primary. Schema version — `2`, миграция с version 1 сохраняет старые пользовательские значения и добавляет новые поля профиля и hotkeys.

`Esc` — контекстная команда отмены в активном UI/selection overlay, а не системная global hotkey.

## Статус

- Фаза 01 — каркас: завершена.
- Фаза 02 — Core: завершена после добавления non-streaming и selection-failure переходов.
- Корректирующая фаза 03A — настройки, recovery, Windows secret store, DI и тесты: реализована; приёмка требует успешных Release build и полного test run, включая Windows integration tests на Windows.
- Текущая следующая фаза после приёмки 03A — фаза 04 (global hotkeys и tray).
- Фазы 04–18 ещё не реализованы.

Текущие ограничения: capture, hotkeys, AI-провайдеры, overlay/chat, CI workflow, installer, releases и updater пока отсутствуют. GitHub Actions CI ещё не добавлен (запланирован на фазу 16). Windows Credential Manager integration tests выполняют реальный round-trip только на Windows и безопасно пропускают OS-вызовы на других платформах.
