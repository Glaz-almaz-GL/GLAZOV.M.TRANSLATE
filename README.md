# GLAZOV.M.TRANSLATE

Библиотека .NET для работы с переводчиками через единый интерфейс: Google, Microsoft, Bing, Yandex, Baidu, а также облачные API Google Cloud и Yandex Cloud. Тексты, разметка, изображения, транслитерация, озвучивание, словари — каждый провайдер объявляет, что он умеет.

Кроме переводчиков, библиотека содержит единую лингвистическую систему: языки, культуры, регионы, письменности и их коды (ISO 639, ISO 3166, ISO 15924, BCP 47).

## Быстрый старт

```
dotnet build GLAZOV.M.TRANSLATE.slnx
dotnet test GLAZOV.M.TRANSLATE.slnx
```

## Структура

- `src/GLAZOV.M.TRANSLATE.Abstractions` — общие контракты и базовые типы
- `src/GLAZOV.M.TRANSLATE.Domain` — лингвистическая система
- `src/GLAZOV.M.TRANSLATE.Providers.*` — провайдеры переводчиков
- `tests/` — тесты; проверки на живых сервисах пропускаются
- `tools/` — генераторы таблиц языков

## Требования

- .NET SDK 10

Лицензия — см. `LICENSE.txt`.