# ARCHITECTURE — GLAZOV.M.TRANSLATE

Version 0.1 — 2026-10-01 (составлено по ссылкам проектов и коду на момент переезда)

## Module map

Код — `src/`, тесты — `tests/`, генераторы — `tools/`. Пространство имён всех проектов — `GLTranslate.*`.

| Module | Responsibility | Depends on |
|---|---|---|
| GLTranslate.Abstractions | Контракты и базовые типы: `ValueObject`, `StringValueObject`, `CodeSet`, `EntitySet`, `ICode`, `IIdentifiable`, `IRegistry`; идентификаторы и коды языков; провайдер и его возможности (`IProvider`, `IProviderCapability`); запросы/результаты и интерфейсы перевода текста, разметки, изображений, документов, аудио; транслитерация; озвучивание | — |
| GLTranslate.Domain | Лингвистическая система: языки, культуры, регионы, письменности; неизменяемые реестры (`ImmutableRegistry`); сгенерированные данные `*.g.cs` | Abstractions |
| GLTranslate.Providers.Common | Общая основа провайдеров: базовые классы текста, разметки, изображений, транслитерации, `ProviderEngine`, `CredentialedEngine` (общий аккаунт и отказ), `LanguageCodeResolver` | Abstractions, Domain |
| GLTranslate.Providers.Google | Google Translate (веб): перевод текста, разметки, транслитерация, озвучивание; таблица языков сгенерирована | Abstractions, Domain, Providers.Common |
| GLTranslate.Providers.GoogleCloud | Google Cloud Translation (официальный API) | то же |
| GLTranslate.Providers.Microsoft | Microsoft Translator; таблица написания языков сгенерирована | то же |
| GLTranslate.Providers.Bing | Bing Translator | то же |
| GLTranslate.Providers.Yandex | Yandex Translate (веб) | то же |
| GLTranslate.Providers.YandexCloud | Yandex Cloud: официальные API, включая SpeechKit | то же |
| GLTranslate.Providers.Baidu | Baidu Fanyi | то же |
| tools/GLTranslate.Domain.Generator | Создаёт `*.g.cs` данных домена (языки, регионы, письменности, BCP 47) | — (запуск вручную, пишет в `src/GLTranslate.Domain`) |
| tools/GLTranslate.Providers.Google.Generator | Создаёт таблицу языков Google | Providers.Google |
| tools/GLTranslate.Providers.Microsoft.Generator | Создаёт таблицу языков Microsoft | Providers.Microsoft |
| tests/*.Tests | По одному проекту на модуль (кроме Abstractions, у него отдельных тестов нет) | тестируемый модуль |

Нарушений изоляции в ссылках проектов не найдено: провайдеры ссылаются только на Abstractions, Domain и Common и не знают друг о друге; Domain не знает о провайдерах.

## Main scenarios

### Перевод текста
Клиентский код получает `ITextTranslationProvider` (реализация — класс провайдера), строит `TextTranslationRequest` из доменных языков, вызывает `ExecuteAsync`. Базовый класс из Providers.Common переводит доменные языки в коды провайдера (`LanguageCodeResolver`), вызывает внутренний `Engine` провайдера (`Internal/`), и возвращает неизменяемый `TextTranslationResult`; отказ сервиса — `ProviderException`.

### Что умеет провайдер
Провайдер реализует набор интерфейсов возможностей (`IProviderCapability`): текст, разметка, изображение, документ, аудио, транслитерация, озвучивание. Клиент спрашивает у провайдера возможность, а не знает провайдера по имени.

### Обновление данных языков
Запуск генератора из `tools/` перезаписывает `*.g.cs` в `src/`; результат фиксируется коммитом.
