# ScreenMind

ScreenMind — персональный AI-помощник для Windows 10/11. По настраиваемой глобальной горячей клавише приложение захватывает активное окно, монитор или выделенную область, обрабатывает изображение в памяти, отправляет его выбранной мультимодальной модели и показывает потоковый ответ в компактном оверлее. Оверлей разворачивается в полноценный чат с уточняющими вопросами по снимку текущей сессии.

## Ключевые принципы

- Захват выполняется только после явного действия пользователя.
- Снимки, запросы, ответы и история **не сохраняются** между запусками.
- Содержимое экрана не попадает в технические логи.
- API-ключи хранятся только в Windows Credential Manager / DPAPI, никогда — в JSON, коде или логах.
- Приложение не скрывает свой процесс и не маскируется под системный компонент.
- Исключение собственных окон из захвата — best-effort (`SetWindowDisplayAffinity`), абсолютная невидимость не гарантируется.

## Технологический стек

| Область | Технология |
| --- | --- |
| Язык / Runtime | C#, .NET 8 LTS |
| UI | Avalonia UI 11, MVVM |
| DI / Config / Logging | Microsoft.Extensions.* |
| HTTP | HttpClientFactory |
| Сериализация | System.Text.Json |
| Тесты | xUnit, FluentAssertions, NSubstitute |
| Платформенные функции | WinAPI / P/Invoke |
| CI/CD | GitHub Actions, GitHub Releases |

## Структура решения

```
src/
├── ScreenMind.App                      — входная точка, композиция DI, Avalonia bootstrap
├── ScreenMind.Core                     — доменные модели, интерфейсы, state machine (без внешних зависимостей)
├── ScreenMind.UI                       — ViewModels и Views (Avalonia), без прямых вызовов WinAPI
├── ScreenMind.Infrastructure           — настройки, конфигурация, миграции схемы
├── ScreenMind.Platform.Windows         — WinAPI: hotkeys, захват экрана, secret store, exclusion
├── ScreenMind.AI                       — оркестратор AI-запросов, маршрутизация, fallback
├── ScreenMind.Providers.OpenAI         — адаптер OpenAI
├── ScreenMind.Providers.Anthropic      — адаптер Anthropic Claude
├── ScreenMind.Providers.Gemini        — адаптер Google Gemini
├── ScreenMind.Providers.OpenAICompatible — адаптер OpenAI-совместимых API
└── ScreenMind.Providers.Ollama         — адаптер локального Ollama
tests/
├── ScreenMind.Core.Tests
├── ScreenMind.AI.Tests
├── ScreenMind.Platform.Windows.Tests
└── ScreenMind.IntegrationTests
build/          — скрипты сборки и publish
installer/      — исходники установщика (артефакты не хранятся в Git)
docs/           — документация
.github/workflows/ — CI/CD
```

## Архитектурные правила

- `Core` не зависит от Avalonia, Windows API, HTTP SDK, файловой системы и конкретных провайдеров.
- `UI` не вызывает WinAPI напрямую — только через интерфейсы `Core`.
- Каждый AI-провайдер реализует единый `IAiProvider`.
- Без Service Locator и глобального изменяемого состояния.
- HTTP-клиенты создаются через `HttpClientFactory`.
- Все длительные операции поддерживают `CancellationToken`.
- Одновременно выполняется только один основной анализ изображения.

## Сборка

Требуется .NET SDK 8.0.

```bash
dotnet restore
dotnet build -c Release --no-restore
dotnet test -c Release --no-build
```

Проверка форматирования:

```bash
dotnet format --verify-no-changes
```

## Статус разработки

Проект разрабатывается по фазам. Текущее состояние: **Фаза 01 — каркас решения** (структура проектов, central package management, базовая DI-композиция).
