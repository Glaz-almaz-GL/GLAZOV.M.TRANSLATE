# ARCHITECTURE — GLAZOV.M.TRANSLATE

Version 0.1 — 2026-10-01 (составлено по ссылкам проектов и коду на момент переезда)

## Module map

Код — `src/`, тесты — `tests/`, генераторы — `tools/`. Пространство имён всех проектов — `GLAZOV.M.TRANSLATE.*`.

| Module | Responsibility | Depends on |
|---|---|---|
| GLAZOV.M.TRANSLATE.Abstractions | Контракты и базовые типы: `ValueObject`, `StringValueObject`, `CodeSet`, `EntitySet`, `ICode`, `IIdentifiable`, `IRegistry`; идентификаторы и коды языков; провайдер и его возможности (`IProvider`, `IProviderCapability`); запросы/результаты и интерфейсы перевода текста, разметки, изображений, документов, аудио; транслитерация; озвучивание | — |
| GLAZOV.M.TRANSLATE.Domain | Лингвистическая система: языки, культуры, регионы, письменности; неизменяемые реестры (`ImmutableRegistry`); сгенерированные данные `*.g.cs` | Abstractions |
| GLAZOV.M.TRANSLATE.Providers.Common | Общая основа провайдеров: базовые классы текста, разметки, изображений, транслитерации, `ProviderEngine`, `CredentialedEngine` (общий аккаунт и отказ), `LanguageCodeResolver` | Abstractions, Domain |
| GLAZOV.M.TRANSLATE.Providers.Google | Google Translate (веб): перевод текста, разметки, транслитерация, озвучивание; таблица языков сгенерирована | Abstractions, Domain, Providers.Common |
| GLAZOV.M.TRANSLATE.Providers.GoogleCloud | Google Cloud Translation (официальный API) | то же |
| GLAZOV.M.TRANSLATE.Providers.Microsoft | Microsoft Translator; таблица написания языков сгенерирована | то же |
| GLAZOV.M.TRANSLATE.Providers.Bing | Bing Translator | то же |
| GLAZOV.M.TRANSLATE.Providers.Yandex | Yandex Translate (веб) | то же |
| GLAZOV.M.TRANSLATE.Providers.YandexCloud | Yandex Cloud: официальные API, включая SpeechKit | то же |
| GLAZOV.M.TRANSLATE.Providers.Baidu | Baidu Fanyi | то же |
| tools/GLAZOV.M.TRANSLATE.Domain.Generator | Создаёт `*.g.cs` данных домена (языки, регионы, письменности, BCP 47) | — (запуск вручную, пишет в `src/GLAZOV.M.TRANSLATE.Domain`) |
| tools/GLAZOV.M.TRANSLATE.Providers.Google.Generator | Создаёт таблицу языков Google | Providers.Google |
| tools/GLAZOV.M.TRANSLATE.Providers.Microsoft.Generator | Создаёт таблицу языков Microsoft | Providers.Microsoft |
| tests/*.Tests | По одному проекту на модуль (кроме Abstractions, у него отдельных тестов нет) | тестируемый модуль |

Нарушений изоляции в ссылках проектов не найдено: провайдеры ссылаются только на Abstractions, Domain и Common и не знают друг о друге; Domain не знает о провайдерах.

## Main scenarios

### Перевод текста
Клиентский код получает `ITextTranslationProvider` (реализация — класс провайдера), строит `TextTranslationRequest` из доменных языков, вызывает `ExecuteAsync`. Базовый класс из Providers.Common переводит доменные языки в коды провайдера (`LanguageCodeResolver`), вызывает внутренний `Engine` провайдера (`Internal/`), и возвращает неизменяемый `TextTranslationResult`; отказ сервиса — `ProviderException`.

### Что умеет провайдер
Провайдер реализует набор интерфейсов возможностей (`IProviderCapability`): текст, разметка, изображение, документ, аудио, транслитерация, озвучивание. Клиент спрашивает у провайдера возможность, а не знает провайдера по имени.

### Обновление данных языков
Запуск генератора из `tools/` перезаписывает `*.g.cs` в `src/`; результат фиксируется коммитом.
