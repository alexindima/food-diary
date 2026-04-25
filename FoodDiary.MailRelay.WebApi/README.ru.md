# FoodDiary.MailRelay

`FoodDiary.MailRelay` — это внутренний сервис доставки писем для FoodDiary.

Он нужен для того, чтобы:
- не отправлять verification/reset письма напрямую из `FoodDiary.Web.Api`
- иметь собственную очередь отправки
- уметь переключать режим доставки между прямым SMTP и relay
- постепенно развивать почтовую инфраструктуру до более зрелого состояния

## Как это работает

Текущая схема такая:

1. `FoodDiary.Web.Api` формирует письмо через существующие senders.
2. Если `EmailDelivery__Mode=Smtp`, API отправляет письмо напрямую через SMTP.
3. Если `EmailDelivery__Mode=Relay`, API отправляет HTTP-запрос в `FoodDiary.MailRelay`.
4. `FoodDiary.MailRelay` в одной транзакции пишет:
   - письмо в таблицу очереди
   - запись в `outbox`
5. Отдельный outbox publisher читает `outbox` и публикует `email id` в RabbitMQ.
6. Consumer внутри `FoodDiary.MailRelay` получает сообщение из RabbitMQ.
7. Перед обработкой consumer регистрирует сообщение в `inbox`, чтобы не обработать один и тот же `email id` повторно.
8. Затем relay забирает письмо из PostgreSQL и отправляет его через upstream SMTP.
9. При ошибке письмо не теряется: статус и retry управляются через PostgreSQL, а polling fallback может подобрать задачу даже если RabbitMQ временно недоступен.

## Что уже реализовано

- HTTP endpoint для постановки письма в очередь: `POST /api/email/send`
- PostgreSQL-backed очередь
- transactional outbox для публикации в RabbitMQ
- inbox deduplication на consumer-стороне
- RabbitMQ как основной trigger-механизм обработки
- publisher confirms при публикации в RabbitMQ
- broker topology: main queue, retry queue, dead-letter queue
- фоновая обработка очереди
- retry с exponential backoff
- reclaim зависших задач через `LockTimeout`
- опциональная DKIM-подпись исходящих писем перед SMTP-отправкой
- suppression list для ручной блокировки адресов
- `health` и `health/ready`
- переключение `Smtp` / `Relay` через конфиг основного API
- Docker service `mail-relay` в `docker-compose.yml`

## Что пока НЕ реализовано

Это важно понимать до запуска в production.

Пока ещё нет:
- DKIM signing
- обработки bounce / complaint
- suppression list
- direct-to-MX транспорта
- полноценной observability-панели и алертов
- административного UI
- полноценной integration-проверки всей broker topology

## Что нужно сделать, чтобы письма реально начали уходить

### Вариант 1. Самый простой: через relay, но с upstream SMTP

Это текущий рекомендуемый путь.

Нужно настроить:

1. `FoodDiary.Web.Api`:

```env
EmailDelivery__Mode=Relay
EmailDelivery__RelayBaseUrl=http://mail-relay:5088
EmailDelivery__RelayApiKey=your-internal-relay-key
```

2. `FoodDiary.MailRelay`:

```env
MailRelay__RequireApiKey=true
MailRelay__ApiKey=your-internal-relay-key

MailRelayBroker__Backend=RabbitMq
MailRelayBroker__HostName=rabbitmq
MailRelayBroker__Port=5672
MailRelayBroker__UserName=guest
MailRelayBroker__Password=guest
MailRelayBroker__VirtualHost=/
MailRelayBroker__QueueName=fooddiary.mailrelay.outbound
MailRelayBroker__OutboundExchangeName=fooddiary.mailrelay
MailRelayBroker__OutboundRoutingKey=outbound
MailRelayBroker__RetryExchangeName=fooddiary.mailrelay.retry
MailRelayBroker__RetryQueueName=fooddiary.mailrelay.outbound.retry
MailRelayBroker__RetryRoutingKey=retry
MailRelayBroker__RetryDelayMilliseconds=30000
MailRelayBroker__DeadLetterExchangeName=fooddiary.mailrelay.dead
MailRelayBroker__DeadLetterQueueName=fooddiary.mailrelay.outbound.dead
MailRelayBroker__DeadLetterRoutingKey=dead
MailRelayBroker__PrefetchCount=10
MailRelayBroker__EnablePollingFallback=true

RelaySmtp__Host=smtp.your-provider.com
RelaySmtp__Port=587
RelaySmtp__UseSsl=true
RelaySmtp__User=your-smtp-user
RelaySmtp__Password=your-smtp-password

MailRelayDkim__Enabled=true
MailRelayDkim__Domain=mail.your-domain.com
MailRelayDkim__Selector=fd1
MailRelayDkim__PrivateKeyPath=/run/secrets/fooddiary-mailrelay-dkim.pem
```

3. Базовые email-настройки API:

```env
Email__FromAddress=noreply@your-domain.com
Email__FromName=FoodDiary
Email__FrontendBaseUrl=https://your-frontend-domain.com
```

После этого:
- API будет отправлять письма не напрямую, а в relay
- relay будет складывать их в очередь
- worker будет отправлять их наружу через указанный SMTP

## Что нужно для хорошей доставляемости

Если ты хочешь, чтобы письма не улетали в спам, одного кода недостаточно.

Нужно обязательно настроить:

1. Домен отправителя
- лучше отдельный поддомен, например `mail.fooddiary.club`

2. DNS-записи
- SPF
- DKIM
- DMARC

3. `From`-адрес
- он должен совпадать с доменом, который реально авторизован на отправку

4. DKIM-ключ
- нужно сгенерировать приватный ключ
- приватный ключ хранить вне репозитория, лучше как mounted secret/file
- публичную часть опубликовать в DNS как TXT-запись для выбранного selector

4. Репутацию
- не слать резко большие объёмы
- начинать с транзакционных писем
- не смешивать verification/reset и маркетинговые письма

## Как запускать локально

### Через Docker Compose

Если нужен полный сценарий:

```powershell
docker compose --profile backend up --build postgres rabbitmq db-init mail-relay api
```

Если хочешь подключить DKIM-ключ как файл в контейнер, используй override compose:

```powershell
docker compose -f docker-compose.yml -f docker-compose.mailrelay-secrets.yml --profile backend up --build postgres rabbitmq db-init mail-relay api
```

И заранее задай:

```env
MAIL_RELAY_DKIM_PRIVATE_KEY_FILE=/absolute/path/to/fooddiary-mailrelay-dkim.pem
MailRelayDkim__PrivateKeyPath=/run/secrets/fooddiary-mailrelay-dkim.pem
```

Если хочешь запускать relay отдельно:

```powershell
dotnet run --project FoodDiary.MailRelay.WebApi
```

### Проверка relay

Health:

```powershell
curl http://localhost:5088/health
curl http://localhost:5088/health/ready
```

## Диагностика очереди

Сервис уже умеет отдавать базовую диагностику:

- `GET /api/email/queue/stats`
- `GET /api/email/messages/{id}`
- `GET /api/email/suppressions`
- `GET /api/email/events`
- `POST /api/email/suppressions`
- `POST /api/email/events`
- `POST /api/email/providers/aws-ses/sns`
- `POST /api/email/providers/mailgun/events`
- `DELETE /api/email/suppressions/{email}`

Если включён `MailRelay__RequireApiKey=true`, для этих endpoint тоже нужен:

```http
X-Relay-Api-Key: your-internal-relay-key
```

### Примеры suppression list

Добавить адрес в suppression:

```http
POST /api/email/suppressions
Content-Type: application/json
X-Relay-Api-Key: your-internal-relay-key

{
  "email": "user@example.com",
  "reason": "hard-bounce",
  "source": "manual-review"
}
```

Если адрес есть в suppression list, relay пометит письмо как `suppressed` и не будет отправлять его наружу.

### Примеры bounce / complaint ingestion

Жёсткий bounce:

```http
POST /api/email/events
Content-Type: application/json
X-Relay-Api-Key: your-internal-relay-key

{
  "eventType": "bounce",
  "email": "user@example.com",
  "source": "mailgun-webhook",
  "classification": "hard",
  "providerMessageId": "provider-123",
  "reason": "mailbox-does-not-exist"
}
```

Complaint:

```http
POST /api/email/events
Content-Type: application/json
X-Relay-Api-Key: your-internal-relay-key

{
  "eventType": "complaint",
  "email": "user@example.com",
  "source": "ses-feedback",
  "providerMessageId": "provider-456",
  "reason": "user-marked-as-spam"
}
```

Сейчас логика такая:
- `complaint` всегда добавляет адрес в suppression list
- `bounce` добавляет адрес в suppression list, если `classification=hard`
- само событие сохраняется в таблицу delivery events для аудита

### Provider adapters

Сейчас добавлены два adapter endpoint-а:

1. `POST /api/email/providers/aws-ses/sns`
- принимает SNS notification payload от Amazon SES
- поддерживает `Bounce` и `Complaint`

2. `POST /api/email/providers/mailgun/events`
- принимает Mailgun webhook payload
- поддерживает `complained`, `failed`, `bounced`

Пока это adapter layer без полноценной криптографической проверки подписи провайдера.
То есть для production лучше ставить их либо за trusted ingress/gateway, либо следующим шагом добавить signature verification отдельно.

## Что такое outbox и inbox в этой реализации

### Outbox

`Outbox` нужен для того, чтобы публикация в RabbitMQ не зависела от удачи в момент HTTP-запроса.

Сейчас логика такая:
- API вызвал relay
- relay сохранил письмо в PostgreSQL
- в той же транзакции relay сохранил запись в `mailrelay_outbox_messages`
- отдельный worker потом уже публикует это событие в RabbitMQ

Это значит, что если RabbitMQ был временно недоступен, письмо всё равно останется в системе и будет опубликовано позже.

Дополнительно сейчас outbox publisher публикует сообщения через `publisher confirms`, то есть relay дожидается broker acknowledgement на publish.

### Inbox

`Inbox` нужен для deduplication на стороне consumer.

RabbitMQ гарантирует как минимум однократную доставку, а не ровно однократную. Это значит, что одно и то же сообщение теоретически может прийти повторно.

Поэтому consumer перед обработкой пишет запись в `mailrelay_inbox_messages`.
Если такое сообщение уже было обработано, второй раз оно не выполняется.

Для обучения это очень полезный паттерн, и он здесь уже встроен в поток обработки.

## Как сейчас устроены очереди в RabbitMQ

В текущей версии используется три основных broker-сущности:

1. Основная очередь
- получает новые задачи на отправку

2. Retry queue
- получает задачи после неуспешной отправки
- держит их заданное время через TTL
- потом автоматически возвращает их обратно в основную очередь

3. Dead-letter queue
- получает задачи, которые считаются окончательно упавшими

Это полезно и для обучения, и для эксплуатации, потому что можно визуально смотреть путь сообщения в RabbitMQ Management UI.

## Что тебе нужно сделать руками перед production

Минимальный список:

1. Выбрать домен/поддомен для отправки.
2. Настроить `Email__FromAddress`.
3. Поднять и настроить upstream SMTP.
4. Включить `EmailDelivery__Mode=Relay`.
5. Включить API key между API и relay.
6. Настроить SPF/DKIM/DMARC.
7. Прогнать тестовые письма на Gmail/Outlook/Yandex.
8. Проверить, что verification и password reset реально доходят и ссылки работают.

## Почему сейчас выбран RabbitMQ, а не Kafka

В текущей реализации я перевёл relay на RabbitMQ как основной trigger-механизм, но оставил PostgreSQL как источник истины по статусам и retry.

Почему RabbitMQ здесь уместен:
- это классическая task queue / work queue задача
- нужен push-механизм к consumer-у
- хочется практики с broker-driven обработкой
- полезны durable queues, ack/nack и prefetch
- хорошо сочетается с outbox/inbox паттерном для таких сценариев

Почему не Kafka:
- для mail delivery это обычно слишком тяжёлый инструмент
- Kafka лучше подходит под event streaming и replayable event log
- operational cost выше
- для transactional email RabbitMQ обычно естественнее

Если позже понадобится совсем высокая пропускная способность или событийная платформа для многих подсистем, Kafka можно рассмотреть отдельно, но для relay я бы оставался на RabbitMQ

## Рекомендуемая эволюция

Я бы шёл так:

1. RabbitMQ + PostgreSQL status store
2. queue metrics + RabbitMQ dashboards
3. DKIM + telemetry + suppression list
4. bounce/complaint pipeline
5. integration tests для outbox/inbox/broker retry
6. direct-to-MX как экспериментальный transport

## Важный вывод

Сейчас `FoodDiary.MailRelay` уже полезен как внутренний relay и очередь доставки.

Но чтобы действительно уйти в “письма стабильно не в спаме”, решающую роль сыграют:
- DNS
- доменная репутация
- качество шаблонов
- объём и прогрев
- bounce/complaint hygiene

То есть код — это только часть задачи.
