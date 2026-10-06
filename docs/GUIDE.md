# GUIDE — GLAZOV.M.TRANSLATE

Version 0.1 — 2026-10-01

## User guide

Библиотека подключается в проект .NET 10 ссылкой на проекты `GLAZOV.M.TRANSLATE.Abstractions`, `GLAZOV.M.TRANSLATE.Domain` и нужные `GLAZOV.M.TRANSLATE.Providers.*`. Языки задаются доменными объектами из `Domain`, а не строками; провайдер создаётся один раз и вызывается через интерфейс нужной возможности (перевод текста, разметки, изображений, транслитерация, озвучивание). Для облачных провайдеров (Google Cloud, Yandex Cloud) нужны ключи владельца; в репозиторий их не класть.

## Developer guide

### Prerequisites
- .NET SDK 10

### Build & run
Проверено 2026-10-01 в новом расположении: 0 ошибок, 0 предупреждений.
```
dotnet build GLAZOV.M.TRANSLATE.slnx
```
Запускать нечего — это библиотека; генераторы данных — по требованию:
```
dotnet run --project tools/GLAZOV.M.TRANSLATE.Domain.Generator
```
(генераторы пишут в `src/GLAZOV.M.TRANSLATE.Domain` и `src/GLAZOV.M.TRANSLATE.Providers.*`; результат смотреть в `git diff`)

### Tests
Проверено 2026-10-01: все проекты зелёные, 0 упавших; 15 проверок пропущены — это пробы на живых сервисах (Google, Microsoft, Bing, Yandex), они требуют сети.
```
dotnet test GLAZOV.M.TRANSLATE.slnx
```

### Debugging
Внутренняя часть каждого провайдера лежит в `Internal/` его проекта; ошибки сервиса приходят как `ProviderException`.
