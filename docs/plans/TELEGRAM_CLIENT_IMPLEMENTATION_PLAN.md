# Telegram как полноценный клиент FoodDiary

Дата: 2026-09-12. Статус: план реализации, код фичи ещё не изменён.

## 1. Результат и принятые решения

Пользователь может зарегистрироваться через Telegram, пользоваться одним аккаунтом в браузере и Mini App, отправлять фото еды в личный чат бота, автоматически получать запись в дневнике, отменять её и смотреть сводки.

Подтверждено пользователем:

- В первую версию входят привязка, вход и регистрация через Telegram.
- После успешного распознавания еда сохраняется автоматически; доступна отмена. Предварительное подтверждение не требуется.
- Нужен законченный план для выполнения одним стартовым поручением, включая проверки и подготовку выпуска.

Рекомендация по результатам исследования, выбранная как базовый вариант плана: **email необязателен для регистрации и ежедневного использования через Telegram**. Предлагать добавить подтверждённый email позже; запрашивать его перед возможностями, которым он действительно нужен. Пользователь поручил исследовать этот выбор, а не подтвердил конкретный вариант самостоятельно.

Остальные продуктовые значения по умолчанию:

- Гибрид: быстрые действия в чате, редактирование и подробные графики в существующем Mini App.
- Только личные чаты. В группах не показывать персональные данные и не выполнять записи.
- Один Telegram ID связан максимум с одним аккаунтом; у аккаунта одна активная связь Telegram.
- Меню: «Добавить еду», «Сегодня», «7 дней», «Вода», «Открыть дневник», «Настройки», «Помощь».
- «7 дней» означает сегодня и шесть предыдущих календарных дней в выбранном часовом поясе.
- Одна фотография — один новый приём пищи. Альбомы в первой версии отклоняются целиком с просьбой отправить одну фотографию; не создавать частичные записи.
- Принимаются Telegram photo и изображения как document после проверки формата/размера. Голос, видео, OCR этикеток, свободный диалог с агентом и распознавание только по тексту отложены.
- Подпись к фото — описание для существующего распознавания. Время записи берётся из времени исходного сообщения. Не пытаться свободно интерпретировать «вчера в 8»; время можно исправить в Mini App.
- Бесплатные/платные возможности и согласие на AI совпадают с приложением. Telegram не получает обход Premium и квот.
- Проактивные напоминания, рассылки, Telegram Payments/Stars и объединение двух существующих дневников не входят в эту версию.

## 2. Email и регистрация: обоснование

Telegram Login поддерживает OIDC и идентификатор пользователя; email среди документированных данных нет. Мини-приложение передаёт отдельно проверяемые сервером initData. Это два способа доказать владение Telegram, а не два разных аккаунта FoodDiary. [Telegram Login](https://core.telegram.org/bots/telegram-login), [проверка Mini App](https://core.telegram.org/bots/webapps#validating-data-received-via-the-mini-app).

Отсутствие email совместимо с социальным входом: Auth0 документирует такие профили и отдельно отмечает необходимость адреса для почтовой коммуникации и восстановления. Это подтверждает техническую возможность, но не доказывает, что все Telegram-приложения выбирают одинаковую политику. [Профили без email](https://support.auth0.com/center/s/article/Users-registered-with-Facebook-sometimes-have-no-e-mail-address), [использование подтверждённой почты](https://auth0.com/docs/manage-users/user-accounts/user-profiles/verified-email-usage).

Для FoodDiary рекомендуемый UX:

1. «Продолжить с Telegram» → проверка Telegram → выбор «Создать аккаунт» или «Подключить существующий» при неизвестной связи.
2. Новый аккаунт: язык, подтверждение часового пояса, действующий onboarding/согласия. Email и пароль не обязательны.
3. Сразу доступны дневник, статистика и функции согласно тарифу. AI требует отдельного существующего согласия.
4. В настройках ненавязчиво предложить добавить резервный способ входа: подтверждённый email и пароль либо Google.
5. Перед текущим внешним checkout требовать подтверждённый email, если существующий контракт провайдера его использует. Покупка не является условием входа или просмотра дневника. Не внедрять платежи внутри Telegram в рамках этого плана; отдельно проверить допустимый способ перехода к оплате перед его включением.
6. При отсутствии резервного способа потеря Telegram означает отсутствие восстановления через FoodDiary email. Сказать это в настройках безопасности и запретить отключение последнего работоспособного способа входа.

Не создавать фиктивные адреса вида telegram-ID@example и не выставлять IsEmailConfirmed=true при отсутствии email. Не требовать номер телефона: он не нужен для этой фичи. Подтверждённый контакт сам по себе не считается способом входа, пока не реализован и проверен соответствующий механизм.

Привязка совпавших профилей не выполняется по имени, username или совпадению email автоматически. Для существующего аккаунта требуется его аутентификация. [Рекомендации по связыванию аккаунтов](https://auth0.com/docs/manage-users/user-accounts/user-account-linking).

## 3. Проверенная база кода

Пути ниже проверены в текущем checkout; это не подтверждение состояния production. Перед реализацией перечитать изменившиеся источники.

| Область | Источник | Что уже есть / пробел |
| --- | --- | --- |
| Бот | `FoodDiary.Telegram.Bot/TelegramBotWorker.cs` | Long polling, /start, вода, Mini App; сообщения без Text сейчас игнорируются |
| Настройки бота | `FoodDiary.Telegram.Bot/TelegramBotOptions.cs`, `docker-compose.yml` | Token, WebAppUrl, ApiBaseUrl, ApiSecret; существующий deployable |
| Telegram HTTP | `Modules/Identity/Presentation/Features/Auth/AuthTelegramController.cs` | verify, login-widget, link, bot/auth; проверить фактическое версионирование URL клиента |
| Привязка | `Modules/Identity/Application/Authentication/Commands/LinkTelegram/LinkTelegramCommandHandler.cs` | Проверка initData и защита от повторного использования |
| Идентичности | `Modules/Users/Application/Services/UserAuthenticationIdentityService.cs` | Вход/привязка Telegram, Google; Telegram без связи сейчас не регистрируется |
| Пользователь | `Modules/Users/Domain/Entities/Users/User.cs` | Email обязателен в Create; HasPassword, TelegramUserId, SecurityVersion |
| EF | `Modules/Users/Infrastructure/Model/Persistence/Configurations/Users/UserConfiguration.cs` | Уникальные email и Telegram ID |
| JWT | `Modules/Identity/Infrastructure/Authentication/JwtTokenGenerator.cs` | Email claim создаётся и извлекается как обязательный |
| Frontend auth | `FoodDiary.Web.Client/src/app/services/auth.service.ts`, `src/app/guards/auth.guard.ts` | Автопривязка initData; guard блокирует неподтверждённую почту |
| Профиль | `FoodDiary.Web.Client/src/app/features/profile/` | Существующие настройки/безопасность, типы предполагают email |
| Изображения | `Modules/Images/Presentation/Features/Images/ImagesController.cs` | upload-url → загрузка → confirm; принадлежность image asset |
| Распознавание | `Modules/Ai/Presentation/Features/Ai/FoodRecognitionController.cs`, `docs/backend/AI_RECOGNITION_JOBS.md` | Устойчивые задачи, UUID дедупликации, Premium, consent, квоты, JobManager |
| Запись еды | `Modules/Meals/Presentation/Features/Meals/MealsController.cs` | POST с EnableIdempotency, PATCH, DELETE; AI-сессии в DTO |
| Срок HTTP-дедупликации | `FoodDiary.Presentation.Api/Filters/IdempotencyFilterOptions.cs` | ResponseTtl 24 часа; недостаточно для вечной гарантии отсутствия повторной еды |
| Сводки | `Modules/Statistics/Presentation/Features/Statistics/StatisticsController.cs`, `Modules/Dashboard/Presentation/Features/Dashboard/DashboardController.cs` | statistics/summary и dashboard; у dashboard есть TimeZoneOffsetMinutes |
| Email в оплате | `Modules/Users/Application/Services/UserBillingService.cs`, `Modules/Billing/Application/Commands/CreateCheckoutSession/CreateCheckoutSessionCommandHandler.cs` | Email передаётся платёжным адаптерам |
| Проверки бота | `tests/FoodDiary.Telegram.Bot.Tests/` | Имеется тестовый проект, расширить вместо дублирования |

Wiki research по Identity завершился с discovery=high, но не проверяет всю будущую область изменения. В checkout есть чужие изменения persistence/lockfiles/wiki; их не включать в реализацию или коммит. Текущая задача меняет только этот документ и индекс планов.

## 4. Архитектура и владение

Бот остаётся отдельным транспортным клиентом API. Не получает ссылок на Domain/Application/Infrastructure основного backend и не обращается к его таблицам напрямую. AI вызывается только существующим backend-потоком. Расчёты питания и правила записи не переносятся в Telegram handlers.

- **Identity:** OIDC, Mini App validation, ограниченные одноразовые onboarding/link intents, сессии, отзыв Telegram-доступа, технический журнал входящих Telegram-операций за узкими API/портами.
- **Users:** наличие/отсутствие email, способы входа, уникальность Telegram ID и email, профиль/часовой пояс, операции над пользователем.
- **Images:** загрузка и жизненный цикл принадлежащих пользователю файлов.
- **Ai:** задачи распознавания, согласие, Premium, квоты и результат; не создаёт Meal.
- **Meals:** создание из результата распознавания, идемпотентность бизнес-операции, отмена своей записи; не зависит от Telegram SDK.
- **Statistics/Dashboard:** показатели и календарные границы; бот только форматирует ответ.
- **Bot:** получение обновлений, скачивание Telegram-файла, вызовы API, возобновление транспортной последовательности, доставка сообщений.

Предлагаемое новое backend-состояние интеграции — техническая persistence Identity, за отдельным узким портом и без ссылок на агрегаты Meals/Ai. Поля с чужими идентификаторами не дают права читать/писать чужие таблицы. До кода оформить ADR и проверить dependency matrix: если текущие правила не допускают такое размещение, выбрать отдельный владелец технической интеграции через `wiki.ps1 decision`, а не ослаблять матрицу ради компиляции.

Новый generic consumer contract Meals допустим только при реальном потребителе и подтверждённой ацикличности. Базовый вариант: бот вызывает Meals HTTP; Meals через узкий контракт Ai получает принадлежащий пользователю результат. Сохраняются применимые AGENTS.md, feature-first slices, K&R и CancellationToken.

## 5. Вход, привязка, отключение

### 5.1 Сайт

- Новый OIDC Authorization Code flow с PKCE; server-side state/nonce, привязка попытки к браузеру, срок действия и одноразовое потребление.
- Сервер проверяет подпись, issuer, audience, срок токена и nonce. Использовать проверенную библиотеку и ротацию JWKS, без собственной криптографии.
- Не считать OIDC sub равным Bot API user ID: канонический Telegram ID брать из проверенного claim `id`, сохраняя issuer/subject согласно контракту провайдера. Сверить это при реализации с актуальной документацией.
- Неизвестная идентичность получает ограниченный onboarding intent, а не полноценную пользовательскую сессию до завершения регистрации.
- Callback не переносит JWT/refresh token в query string; использовать существующий безопасный обмен/refresh-cookie lifecycle.
- Не ломать старые verify/login-widget/bot/auth контракты без явной миграции и snapshot-тестов.

### 5.2 Mini App и бот

- Mini App проверяется через подписанный initData, а не initDataUnsafe или ID из браузера.
- Убрать скрытую автоматическую привязку после произвольного входа. Показать, какой аккаунт связывается, и потребовать осознанное действие.
- Защиту от replay сохранять. Не переиспользовать один и тот же initData последовательно для verify и link: после проверки выдать purpose-bound серверный intent, который завершает регистрацию либо явную привязку.
- Бот без связи предлагает Mini App для регистрации/входа; сам /start не создаёт аккаунт. До настройки не запускать платное распознавание присланного фото.
- В настройках сайта использовать тот же Telegram Login для привязки. Отдельный механизм /start с кодами не обязателен для v1, чтобы не плодить протоколы.

### 5.3 Отзыв

- Отключение требует свежего подтверждения доступа и наличия альтернативного входа.
- Отзывать выданные Telegram-сессии и доступ бота, отменять незавершённые операции интеграции. Каждая отложенная операция проверяет актуальную связь и её поколение.
- Не переносить старые операции на другой аккаунт после перепривязки.
- Проверять текущую активность пользователя и SecurityVersion при API-вызовах. Смена связи и блокировка должны действовать также на уже выданный bot token.
- Заблокированный, удалённый или архивированный аккаунт не воссоздаётся автоматически при входе Telegram.
- Старые связи Telegram сохраняются; неизвестный новый OIDC идентификатор не должен создавать дубликат уже связанного пользователя.

## 6. Аккаунт без email: обязательный объём

Это отдельная первая фаза, не локальная правка валидатора регистрации.

1. В Users разрешить отсутствие email у внешней идентичности; обычная email/password регистрация продолжает требовать адрес. Сохранить инвариант минимум одного способа входа.
2. Nullable email в EF, DTO, projections и frontend; уникальность реальных нормализованных адресов и конкурентного назначения. Отсутствие адреса — null, не пустая строка.
3. В JWT основной идентификатор — UserId; email claim факультативен. Проверить выдачу, refresh, sessions, аудит и impersonation/admin projections.
4. Отделить «может войти» от IsEmailConfirmed. Старые email/password аккаунты по-прежнему проходят требуемое подтверждение; Telegram-only не попадает в бесконечный redirect на email verification.
5. Добавление адреса: pending email → одноразовое подтверждение → атомарное назначение; не использовать неподтверждённый адрес для восстановления. Конфликт адреса не объединяет аккаунты.
6. Добавление пароля после подтверждения email и свежей аутентификации; привязка Google для Telegram-only должна учитывать отсутствие текущего email и существующие конфликты.
7. Проверить почтовые задания/уведомления: отсутствие адреса означает предсказуемый пропуск, без бесконечных retry и invalid recipient.
8. Проверить Billing checkout, Dietologist invitations/search, Admin search/display, экспорт, удаление/восстановление, initial admin, marketing и trial: отсутствие email не вызывает исключение и не даёт повторный trial.
9. Для операций, которым адрес нужен, возвращать понятную domain error и ссылку на добавление email, сохраняя доступ к остальному приложению.
10. Миграция сохраняет существующие адреса/связи. Разрешение null меняет контракт: обновить всех прямых потребителей, snapshots и порядок выпуска. После создания email-less пользователей откат старого API с обязательным email небезопасен.

## 7. Фото → запись → отмена

### 7.1 Последовательность

1. Проверить личный чат, отправителя, актуальную связь, завершённый onboarding; зарегистрировать update в durable inbox до подтверждения получения.
2. Сообщить «Фото получено, распознаю». Фиксировать message ID ответа для дальнейшего редактирования.
3. Скачать ограниченным потоком крупнейший подходящий photo либо валидный image document. Проверять фактический MIME/signature, разрешение и размер по существующим ограничениям Images/AI. Не скачивать URL из подписи.
4. Получить upload URL, загрузить, подтвердить image asset от имени пользователя. Сохранить asset ID перед следующим шагом.
5. Создать существующую FoodRecognitionJob со стабильным UUID и подписью. Повтор HTTP использует тот же UUID и тот же payload.
6. Опрашивать owner-scoped GET с backoff до завершения; после рестарта возобновлять GET, не запускать AI заново.
7. При полном успешном результате автоматически вызвать proposed `POST /api/v1/meals/from-recognition` со стабильным operation ID, job ID, временем исходного сообщения и метаданными. Точное имя зафиксировать в контракте перед реализацией.
8. Meals проверяет владельца job/image, состояние результата и создаёт те же AI sessions/items, что существующий web flow. Не создавать фиктивные Products. Не доверять присланным ботом итогам вместо серверного результата.
9. Вернуть «Добавлено: …» с калориями/БЖУ, временем и кнопками «Изменить», «Отменить добавление», «Сегодня».

Пустой/непищевой результат либо частичный nutrition failure не сохранять как нулевую еду. Объяснить причину и предложить открыть результат/ручное добавление. Не вводить выдуманный порог confidence, если провайдер его не предоставляет.

### 7.2 Durable processing

Предлагаемая запись интеграции: bot identity, update ID, chat/message ID, FoodDiary user ID, поколение связи, received timestamp, operation/job/image/meal IDs, состояние, lease/version, retry time, безопасный error code, message ID ответа. Секреты, токены, raw initData и бинарные фото в журнале не хранить.

Состояния: Received → Uploaded → Recognizing → Saving → Saved; дополнительно Rejected, Failed, Cancelled, Undone. Delivery state отделён от результата записи. Lease/fencing защищает от двух обработчиков; задания переживают перезапуск.

- Оставить long polling для v1 с единственным активным получателем на bot token. Проверить реальное поведение Telegram.Bot receiver; если он подтверждает offset до durable записи, заменить получение явным getUpdates loop.
- Offset продвигается только после надёжного принятия либо сохранённого отклонения update. Не использовать drop_pending_updates при обычном выпуске.
- Telegram хранит входящие обновления ограниченное время; нельзя обещать восстановление фото после сколь угодно долгого простоя. [Bot API](https://core.telegram.org/bots/api#getupdates).
- Unique `(bot identity, update ID)` плюс стабильный business operation ID. Не дедуплицировать намеренную повторную отправку фото только по file_unique_id.
- При неизвестном исходе POST проверять ту же операцию, не создавать новый ключ.
- В Meals хранить уникальный `(user ID, operation ID)` и payload fingerprint атомарно с созданием Meal; повтор возвращает существующий результат, другой payload — конфликт. Связь операции сохраняется и после undo, чтобы retry не воскресил запись. HTTP cache на 24 часа этого не заменяет.
- При исходе AI с неизвестной оплатой соблюдать существующее правило: без автоматического повторного платного вызова. Явный retry пользователя создаёт новую job и объясняет повторную попытку.
- Недоступный Telegram после сохранения не отменяет Meal. Повтор доставки не повторяет запись. Не обещать exactly-once для sendMessage при потере ответа Telegram; по возможности редактировать известное сообщение.
- При отключении связи/удалении пользователя прекратить новые шаги. Уже выполненный платный вызов не обещать отменить или вернуть квоту.
- Терминальные технические записи с payload очищать через 7 дней; долгоживущий минимальный receipt Meals нужен для дедупликации до удаления аккаунта. Учесть порядок purge и существующую retention images/jobs.

### 7.3 Отмена и изменение

- «Изменить» открывает существующий meal editor через Mini App и проверяет владельца на API.
- «Отменить добавление» доступно 24 часа после сохранения; позднее предложить обычное удаление в дневнике. Это выбранное значение по умолчанию, вынесенное в настройку.
- Callback содержит непрозрачный operation/action ID, а сервер проверяет отправителя, поколение связи, владельца Meal, срок и версию записи.
- Undo атомарно помечает receipt и удаляет/отменяет только созданный этой операцией Meal через Meals. Повторный callback успешно сообщает «уже отменено».
- Если запись изменена в приложении после автосохранения, не удалять её старой кнопкой молча: открыть подтверждение актуальной записи в Mini App.
- Отмена еды не возвращает уже потраченный AI лимит. Запись уже удалена вручную — сообщить это, не создавать её повторно.
- После добавления/undo статистика обновляется по существующей политике инвалидирования кэшей; проверить фактическое поведение.

## 8. Часовой пояс, статистика и вода

- Telegram не предоставляет надёжный часовой пояс пользователя. Mini App предлагает timezone браузера и позволяет изменить его; хранить IANA zone в профиле Users после подтверждения, не только текущий UTC offset.
- Существующим связанным пользователям предложить выбрать пояс при первом новом сценарии. Не считать timezone сервера timezone пользователя.
- «Сегодня»: калории и цель, БЖУ, вода, число приёмов пищи; отсутствие записей явно отличается от ошибки API.
- «7 дней»: даты диапазона, суммы/средние с явно указанным знаменателем, дни с записями; использовать определения существующей статистики. Не добавлять медицинских рекомендаций.
- Перед запросами переводить локальные границы в UTC по существующей семантике включения DateTo. Для календарных дневных buckets проверить DST; если текущий statistics endpoint группирует UTC-сутки, добавить timezone-aware read контракт у владельца, не исправлять итоги в боте.
- Вода: +250/+500 мл, стабильный ID конкретного callback, понятный результат и предсказуемая ошибка. Два отдельных намеренных нажатия — две записи; повтор доставки того же callback — одна.
- Локаль: профиль FoodDiary, иначе поддерживаемый Telegram language_code, иначе en. Все команды, ошибки, карточки и frontend тексты — ru/en.

## 9. Контракты, конфигурация, ограничения

До реализации составить точный HTTP contract table с request/response/error codes для новых OIDC/onboarding, link/unlink/status, optional-email, integration-operation, create-from-recognition и undo сценариев. Имена выше — предложения, а не существующие маршруты.

Для каждого endpoint: auth mode, current-user binding, purpose, rate limit, idempotency и TTL, expected status codes, snapshot. Новые интеграционные API доступны только доверенному боту с проверенным пользовательским контекстом; общий service secret не заменяет проверку владельца операции.

Существующий `bot/auth` выдаёт пользовательский access token. Проверить необходимость ограниченной bot audience/scope и отзыв; не хранить долгоживущие refresh tokens в журнале бота. Закрыть user-ID injection из callback/form payload; доверять отправителю Telegram update только в доверенном транспортном процессе.

Настройки: отдельные feature flags для Telegram registration, linking/login, photo autosave; OIDC client/redirect allowlist; ограничения файлов; concurrency/polling/retry; undo TTL; cleanup; валидируемые таймауты. Значения секретов только во внешнем хранилище окружения, в repo только имена/безопасные примеры.

Логи: correlation/operation IDs, состояние, безопасный код ошибки; без подписанных URL, адресов скачивания Telegram с bot token, initData, OIDC/JWT, фото и сырых provider responses. Метрики: возраст очереди, recognition/save/undo failures, доставка, отказ авторизации, дедупликация. Не логировать полный пользовательский payload.

## 10. Этапы реализации и выходные критерии

| Этап | Работа | Критерий выхода |
| --- | --- | --- |
| 0. Baseline/design | Перечитать AGENTS, scoped sources, wiki start/research/brief, journeys/test-plan/privacy/topology/ownership/decision/rollout; зафиксировать ADR, точные contracts и planned paths | Область подтверждена кодом, зависимости ацикличны, известны ограничения окружения |
| 1. Email optional | Users/EF/contracts/JWT/guards/projections/email jobs/Billing guards, добавление email и резервного входа | Telegram-only пользователь проходит обычные read/write flows; старый email/password flow не ослаблен |
| 2. Telegram identity | OIDC, Mini App onboarding, регистрация/link/unlink/status, session revocation | Сайт и Mini App открывают один аккаунт, конфликты/повтор/удаление покрыты |
| 3. Durable transport | Typed HTTP client, inbox/state/leases, polling ack, private-chat boundary, ru/en menu | Принятые операции восстанавливаются после restart; группы не раскрывают данные |
| 4. Photos/autosave | Download/upload/confirm → job → owner-side create-from-recognition + receipt | Полный результат автоматически создаёт ровно один Meal даже при повторе/потере ответа |
| 5. Undo/editor | Version-aware undo, web links, callback isolation | Повтор отмены безопасен; изменённая запись не удаляется старой кнопкой |
| 6. Summaries/water | Timezone onboarding, today/7 days, water idempotency | Данные совпадают с приложением, включая полночь и DST |
| 7. Verification/release prep | Unit/integration/UI, snapshots, migrations, docs/runbook, feature flags | Acceptance matrix закрыта доказательствами; непроверенные live-шаги названы |
| 8. Controlled release | Тестовое окружение, real Telegram smoke, миграции и включение flags | Live readiness подтверждена отдельно; production только в явно разрешённом scope |

Зависимости: 0 → 1 → 2 → 3 → 4 → 5; 6 зависит от 1–3; 7 проверяет все этапы. Не начинать следующий этап с известными нарушениями контрактов предыдущего. Этот план не требует запуска субагентов.

## 11. Матрица приёмки

| ID | Проверка | Доказательство |
| --- | --- | --- |
| TG-01 | Новый пользователь без email регистрируется и входит с сайта и Mini App | Identity/Users integration + browser/Telegram smoke |
| TG-02 | Повторный вход и конкурентная регистрация создают один User | PostgreSQL concurrency test |
| TG-03 | Существующий пользователь явно связывает Telegram; чужая связь даёт конфликт | Application/API tests |
| TG-04 | Поддельный/просроченный/replayed OIDC/initData/state/intent отвергается | Provider/controller tests |
| TG-05 | Unlink/блокировка/удаление отзывают bot session и pending operations | API integration, race tests |
| TG-06 | Нельзя отключить последний вход; email/password/Google продолжают работать | Domain/application + UI tests |
| TG-07 | Нет email: JWT refresh, профиль, admin, почтовые jobs не падают | Focused regression tests |
| TG-08 | Добавление email требует подтверждения, конфликт не объединяет аккаунты | Integration + UI |
| TG-09 | Фото автоматически создаёт Meal с AI items, изображением и временем сообщения | Mock-provider end-to-end API test |
| TG-10 | Дубликаты update/callback, restart после каждого checkpoint, потеря POST ответа не создают второй Meal | Durable-processing integration |
| TG-11 | Retry после истечения HTTP TTL и после undo не воскрешает Meal | Meals receipt tests |
| TG-12 | Partial AI failure/non-food/нет consent/Premium/quota не создают ложную запись | Application/bot tests |
| TG-13 | Undo повторяем, чужой callback отклонён, edit-vs-undo race защищён | DB/API tests |
| TG-14 | Личные данные недоступны группе, другому пользователю и новой связи | Negative API/bot tests |
| TG-15 | Сегодня/7 дней совпадают с web; границы суток/DST/нет записей | Statistics tests + manual comparison |
| TG-16 | Два намеренных water callback дают две записи; повтор одного — одну | Bot/API tests |
| TG-17 | Большой/ложный image document, альбом, caption injection не обходят ограничения | Boundary tests |
| TG-18 | Telegram/API 429/5xx, заблокированный бот и неизвестный sendMessage outcome не повторяют бизнес-запись | Transport tests |
| TG-19 | ru/en, Mini App edit navigation, reload и account switch работают | Frontend tests + visual QA |
| TG-20 | Migration пары, snapshots, compose configuration и rollback runbook согласованы | Build/architecture/wiki checks |

Автотесты используют fake Telegram, OIDC и AI; не вызывают живого AI-провайдера. Live AI smoke разрешать отдельно в рамках правил Ai AGENTS.md, не расходовать квоту ради автоматических проверок.

Основные команды реализации (точные дополнительные проекты вывести из test-plan после финального scope):

```powershell
dotnet build FoodDiary.slnx
dotnet test tests/FoodDiary.Telegram.Bot.Tests/FoodDiary.Telegram.Bot.Tests.csproj
dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj
dotnet test tests/FoodDiary.Web.Api.IntegrationTests/FoodDiary.Web.Api.IntegrationTests.csproj
dotnet test tests/FoodDiary.Infrastructure.IntegrationTests/FoodDiary.Infrastructure.IntegrationTests.csproj
```

Дополнительно обязательны затронутые Identity/Users/Ai/Meals/Statistics/Dashboard unit и PostgreSQL integration suites, presentation/idempotency tests. Для frontend из `FoodDiary.Web.Client`: `npm run build`, `npm run verify`; focused tests по локальным AGENTS. Проверить contract snapshots под `tests/FoodDiary.Web.Api.IntegrationTests/Snapshots/`. Для изолированных .NET outputs использовать repository-level `--artifacts-path`.

Сценарии ручной проверки: новый Telegram-only аккаунт; существующий email аккаунт; вход на втором устройстве; фото и undo; ручное изменение до undo; суточная/недельная сводка; restart между recognition и save; недоступный API; unlink во время job. Прогон в настоящем Telegram отмечается отдельно от mocked доказательств.

## 12. Выпуск, откат и доступы

До live-проверки нужны отдельный тестовый bot token, действующий HTTPS Mini App/redirect URL, настроенные BotFather Allowed URLs/client ID, API+DB+Images+JobManager, доступный тестовый аккаунт. Не открывать секреты при подготовке плана. Если ресурсов нет, подготовить код и конфигурационный чеклист и явно назвать непроверенные пункты.

Порядок: совместимые nullable-email consumer changes и миграции → API/JobManager → frontend → bot → тестовый smoke → поэтапное включение registration/autosave. При несовместимости старого API с nullable email использовать согласованный rollout/окно, не допускать смешанные реплики после начала email-less registrations.

Откат: выключить admission flags, продолжить безопасное завершение/отмену принятых операций, не удалять receipts и очереди. После появления null email не откатывать схему на NOT NULL и не запускать старый API без плана совместимости; исправлять вперёд. При возврате старого бота учесть ack/offset и остановить обработку новых photos. Дневник и уже созданные Meals сохраняются.

Runbook должен содержать проверки backlog, неопределённых provider исходов, недоставленных сообщений и очистки orphan images. Production deploy, merge/push и изменение BotFather не подразумеваются самим планированием. При последующем поручении применить явно выданный scope; git hooks всегда включены.

## 13. Готовый стартовый промпт реализации

```text
Реализуй Telegram как полноценный клиент FoodDiary по
docs/plans/TELEGRAM_CLIENT_IMPLEMENTATION_PLAN.md.

Пройди план до законченной, проверенной реализации: регистрация без обязательного
email, вход и явная привязка Telegram, безопасное отключение, фото с автоматическим
сохранением и отменой, сводки за сегодня и 7 дней, вода, Mini App, ru/en.
Email optional — базовое решение плана; не подменяй его фиктивными адресами.

Сначала перечитай текущие AGENTS.md и источники плана, проверь git status и
актуальность контрактов. Сохрани чужие изменения. Выполни wiki start для этой
cross-layer feature с конкретными PlannedPath, затем research/design и остальные
обязательства, которые выведет текущий adaptive workflow. Привяжи TG-01..TG-20
к acceptance criteria и реальным journey IDs, не выдумывай IDs или test coverage.

Работай последовательно по этапам, фиксируй прогресс и доказательства в durable
task state. Переиспользуй Images, durable AI jobs и Meals; не вызывай AI из бота
напрямую. Сохраняй согласия, квоты, current-user checks и module boundaries.
Проверяй восстановление после restart и потерянных HTTP ответов, а не только happy path.

Не останавливайся после первого работающего сценария. Заверши backend, bot,
frontend, migrations, snapshots, meaningful tests и runbook. При новых фактах
корректируй план с причиной; вопросы задавай только при действительном продуктовом
блокере, продолжая независимую работу. Не запускай субагентов без отдельного поручения.

Запусти применимые проверки, wiki verify, delivery-validate и обязательный critique.
Для UI выполни визуальную проверку и проверь русский текст. Не объявляй Telegram
live smoke или production readiness пройденными без реальных доказательств.
Если тестового окружения не хватает, заверши независимую работу и перечисли
точные недостающие настройки без секретов. Не вызывай живой AI в автотестах.

Это поручение разрешает реализацию и локальные проверки. Production deploy,
публикация/merge/push и изменение внешних настроек требуют явного отдельного scope.
В конце сообщи изменения, результаты проверок, оставшиеся внешние шаги и риски.
```

## 14. Статус подготовки плана

- [x] Пользователь определил login/register/link и autosave.
- [x] Исследована возможность Telegram-only без email; рекомендован optional email.
- [x] Проверены реальные точки интеграции и основные email-зависимости.
- [x] Описаны этапы, повторные доставки, отмена, часовой пояс, выпуск и acceptance matrix.
- [ ] Перед реализацией сверить весь nullable-email consumer graph и оформить ADR/точные DTO.
- [ ] Перед live smoke проверить наличие тестового бота и HTTPS окружения.

Последние два пункта — обязательная работа исполнителя в этапах 0/7, а не утверждение о готовности к production. Блокирующих продуктовых вопросов для продолжения планирования нет.
