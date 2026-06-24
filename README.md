# HealthChecker

HealthChecker — учебный микросервис на C# .NET 8 для мониторинга API и веб-сервисов.

Сервис по расписанию проверяет URL-адреса из `config.yaml`, анализирует HTTP-статус, время ответа и SSL-сертификат. При обнаружении проблемы сервис пишет событие в лог и отправляет уведомление в Telegram.

## Возможности

- чтение целей мониторинга из YAML-конфига;
- проверка HTTP-статуса;
- измерение времени ответа;
- проверка срока действия SSL-сертификата;
- Telegram-уведомления при ошибках;
- логирование через NLog в консоль и файл;
- запуск локально или через Docker Compose.

## Стек

- C# .NET 8;
- .NET Worker Service;
- HttpClient;
- YamlDotNet;
- Telegram.Bot;
- NLog;
- StyleCop Analyzers;
- Docker / Docker Compose.

## Структура проекта

```text
HealthChecker/
├── Models/
│   ├── AppConfig.cs
│   ├── CheckResult.cs
│   ├── SslCheckResult.cs
│   ├── TargetConfig.cs
│   └── TelegramConfig.cs
├── Services/
│   ├── EndpointChecker.cs
│   ├── HealthCheckWorker.cs
│   ├── SslChecker.cs
│   └── TelegramNotifier.cs
├── Utils/
│   ├── ConfigLoader.cs
│   └── IConfigLoader.cs
├── Program.cs
├── HealthChecker.csproj
├── config.yaml
├── nlog.config
├── Dockerfile
├── docker-compose.yml
└── README.md
```

## Настройка config.yaml

```yaml
check_interval_seconds: 60
timeout_seconds: 5
ssl_expire_days: 7
response_time_warning_ms: 2000

telegram:
  bot_token: ""
  chat_id: ""

targets:
  - name: "Google"
    url: "https://google.com"
    success_codes: [200, 301, 302]
    max_response_time_ms: 2000
    enabled: true
```

### Пояснение параметров

- `check_interval_seconds` — интервал проверки в секундах;
- `timeout_seconds` — максимальное время ожидания ответа;
- `ssl_expire_days` — за сколько дней до истечения SSL отправлять предупреждение;
- `response_time_warning_ms` — общий лимит времени ответа;
- `targets` — список сайтов и API для проверки.

## Telegram

Создайте Telegram-бота через BotFather и получите токен.

Далее можно указать данные .

### : через config.yaml

```yaml
telegram:
  bot_token: "123456789:YOUR_TOKEN"
  chat_id: "123456789"
```


## Запуск локально

```bash
dotnet restore
dotnet run
```

## Запуск через Docker Compose

1. Скопируйте `.env.example` в `.env`.
2. Укажите токен Telegram-бота и chat_id.
3. Запустите проект:

```bash
docker compose up --build
```

## Пример логов

```text
2026-06-22 12:00:00 | INFO | HealthChecker.Services.HealthCheckWorker | [OK] Google | https://google.com | status: 200 | response: 154 ms | SSL days: 72
2026-06-22 12:01:00 | WARN | HealthChecker.Services.HealthCheckWorker | [ALERT] Example | https://example.invalid | status: no response | response: 5000 ms | problem: Request timeout after 5 seconds.
```

## Пример Telegram-уведомления

```text
⚠️ Предупреждение HealthChecker

Сервис: Example
URL: https://example.invalid
Статус: нет ответа
Время ответа: 5000 мс
SSL осталось дней: не проверено
Проблема: Таймаут запроса: сервис не ответил за заданное время.
Проверено UTC: 2026-06-22 12:01:00
```

