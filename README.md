# ZoneClan

Портфолио-проект системы управления игровым кланом на базе Discord.

![.NET 10](https://img.shields.io/badge/.NET-10-512BD4)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-8.0-512BD4)
![MySQL](https://img.shields.io/badge/MySQL-8.0-4479A1)
![Blazor](https://img.shields.io/badge/Blazor-8.0-512BD4)

## О проекте

ZoneClan — это full-stack приложение для управления игровым кланом, включающее:

- **Web интерфейс** — Blazor Server для управления игроками, статистикой и матчаими
- **Discord бот** — автоматизация уведомлений и сбор статистики через Discord
- **API сервер** — ASP.NET Core Web API с MySQL для хранения данных
- **OCR** — анализ скриншотов для автоматического импорта статистики

## Технологии

- **Backend:** .NET 10, ASP.NET Core, C#
- **Database:** MySQL 8.0, Dapper
- **Frontend:** Blazor Server, Semantic UI
- **Bot:** NetCord, C#
- **Image Processing:** ImageSharp (OCR)

## Быстрый старт

### 1. Установить зависимости

```bash
dotnet restore
```

### 2. Настроить базу данных

Создать MySQL базу `clan_hub`:

```sql
CREATE DATABASE clan_hub;
```

### 3. Запустить приложение

```bash
# Сервер с демо-данными
dotnet run --project ZoneClan.Server

# Web интерфейс (в отдельном терминале)
dotnet run --project ZoneClan.Web
```

## Структура проекта

```
ZoneClan/
├── ZoneClan.Core/      # Общие модели и бизнес-логика
├── ZoneClan.Server/    # ASP.NET Core Web API
├── ZoneClan.Web/       # Blazor Server интерфейс
├── ZoneClan.Bot/       # Discord бот
├── Tools/
│   └── AnonymizationTool/  # Анонимизация реальных данных
└── Табы/               # Скриншоты для OCR (не включены в репозиторий)
```

## Demo режим

При включении `DemoMode` приложение автоматически создает:

- 50 фейковых игроков с Discord ID
- 20 матчей с игровой статистикой
- Геймерские ники (ShadowHunter, IronWolf87 и т.д.)

Все данные генерируются программно — не содержит реальной информации.

## Анонимизация данных

Для работы с реальными данными клана используйте инструмент анонимизации:

```bash
cd Tools/AnonymizationTool
dotnet run -- "connection_string" --dry-run
dotnet run -- "connection_string"
```

Это заменит реальные Discord ID на безопасные значения для публикации.

## Лицензия

MIT License - свободно для использования в портфолио и обучении.
