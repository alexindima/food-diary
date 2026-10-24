# Аудит инструментов Wiki, 24 октября 2026

Аудируемый Git HEAD: `b5e40b9a25a287283f61eedd5abfb3abe39d2ab9`, ветка
`codex/ai-tooling-feedback-20261024`. Первоначальный аудит начат на чистом дереве.
По следующему запросу исправлены шесть подтверждённых дефектов FDW-01…06 и
повторён анализ на изменённом рабочем дереве. Commit, push и deploy не выполнялись.
Команды ниже относятся к корню репозитория.

Перед локальным коммитом рабочее дерево объединено с `29143cc6411b266e29a6fa93a14edcc109dbadfe`
(серверная диагностика и ротация логов). Общие catalog/review-данные сохранены с обеих
сторон. Инвентаризация и полный прогон ниже остаются историческими свидетельствами
для указанного выше HEAD; проверка интеграции выполнена отдельно на новом HEAD.

Подтверждённые сценарии потери generated-файлов, пропуска удалений, неограниченного
ожидания workers и отсутствия штатного архива закрыты. Границы исправлений и
регрессий приведены ниже. Безопасное немедленное удаление production-инструмента
по-прежнему не доказано. Следующий измеримый резерв — symbol-index `-Check`;
редкие experimental-семейства требуют данных об использовании.

## Повторный анализ после исправлений

| Находка | Изменение | Поведенческая проверка |
| --- | --- | --- |
| FDW-01, закрыта | Rollback разрешён только после публикации полного проверенного backup; ошибка первоначального копирования сохраняет generated | Ошибка второго Copy-Item в настоящем transaction try/catch, сохранены все исходные bytes и кириллица; PS7 и PS5 |
| FDW-02, закрыта | Неизменяемые versioned checkpoints, SHA-256 manifest и публикация ссылки после проверки. Recovery проверяет checkpoint, затем backup; сохраняет неисправную транзакцию | Прерванная следующая копия, повреждённый checkpoint, повреждение обоих snapshots, legacy fallback, реальное ожидание двух writers |
| FDW-03, закрыта | Pre-push использует `--no-renames --name-only` со всеми статусами и Unicode. Hook участвует в smoke selection и fingerprint | Настоящий hook prefix в Git fixture: deletion-only, оба конца rename, кириллица, новый ref, несколько refs; PS7/PS5 |
| FDW-04, закрыта | Adaptive wrapper использует общий bounded batch; graph subprocesses имеют deadline/AbortSignal/output bound; index batch останавливает начатых writers при сбое следующего запуска | Реальные child/grandchild: timeout, exit 7, startup failure, cancel, output overflow, сообщения ошибки; Node и PS7/PS5 |
| FDW-05, закрыта | `verification-telemetry-archive` сохраняет точный файл и hash-linked descriptors под общей блокировкой, открывает активное окно | Publication fault/retry, UTF-8 BOM byte hash, cap, same-size/time tamper, отсутствующий архив/manifest, одновременные record/archive; PS7/PS5 |
| FDW-06, закрыта | Существующие state и registry заменяются через File.Replace с настоящим null backup; начальная публикация идёт в отсутствующий путь | IL установленных PS7/PS5 подтверждает Delete перед MoveTo в прежнем force-provider пути; новый state заменяется целиком, sharing failure сохраняет прежние bytes; PS7/PS5 |

Новый census содержит **369 команд, 550 исполняемых файлов/проектов, 99 459 строк**,
53 policy/schema/corpus/knowledge-файла и 2805 AST call/import/dispatcher edges.
86 команд core, 81 governed, 202 experimental. Размер исходников вырос из-за
проверяемых snapshot/cleanup/archive протоколов и регрессий. Удалений файлов и
команд нет. Для каждого файла обновлены SHA-256, зависимости, current lexical
consumers, historical digest comparison, finding status и граница review.
Generated records и исторические отчёты исключены из current caller graph,
чтобы census не размножал собственные упоминания.

Повторно проверены CLI/registry/help compatibility, generator dependency barriers,
writer-lock ordering, reader/cache hash contracts, test discovery, CI/hook wiring,
MCP boundaries и основания удаления по всем семействам ниже. В исправленных путях
закрыты дополнительные частные случаи: PS5 UTF-8 manifest decoding, освобождение
ресурсов при ошибке подготовки worker, остановка index writer до rollback,
холодная инициализация telemetry под lock, сохранение BOM при архиве и замена delete-then-move на файловую replacement-операцию.
Отдельные assertions проверяют выбор группы для внешнего hook path и invalidation
receipt при его same-size/time правке.

Локальные 5000 событий telemetry сохранены штатной archive-командой. SHA-256 архива
совпадает с исходным файлом:
`5f249824097e7580b73094fb27b70b96c5568178ffe1c3cf6a9035f5e83802d9`.
Новые события записываются в активный registry; старые доступны в связанном архиве.
`metrics` и `list` явно описывают **активное окно**, не объединяют всю историю;
старые forecasts требуют пересчёта после смены registry hash. При чтении
проверяются bytes всех архивов, поэтому стоимость растёт с историей.

Результат общего gate фиксируется в
`.artifacts/wiki-tools-repair-20261024/final-verification.json` после заморозки
отчёта и JSON-приложений. Отдельные тесты и probes сохранены в `postRepair` evidence.
Повторный общий запуск — `wiki verify` и `wiki verify-full` с профилем Focused.
Исторический monolithic Full, frontend `npm run verify`, backend solution suite
и production сценарии этим tooling-only изменением не заявляются выполненными.

Границы: fault injection воспроизводит реальные функции на достижимых состояниях,
но не отключение питания/переполнение диска во всех возможных точках. Legacy
snapshot не восстановит файл, уже утраченный старой версией. Node deadline
допускает до 20 секунд на cleanup после основного срока. Batch stdout/stderr
ограничены по наблюдаемому размеру файлов с polling: возможен краткий overshoot;
память вывода ограничена preview. Cleanup tests подтверждают остановку живых owned
деревьев и siblings при ошибке запуска, а не универсальное supervision намеренно
фоновых процессов после успешного выхода родителя. Linux-specific поведение в
этой Windows-сессии не исполнялось. CI Node 22 не подменён локальным Node 26.

## Доказательства и границы полноты

- [Инвентарь](wiki-tools-audit-20261024.inventory.json): 369 CLI-команд, 550
  исполняемых файлов/проектов, 99 459 строк и 53 policy/schema/corpus/knowledge-файла.
  Для каждой команды сохранены tier, строка dispatch, файлы реализации, упоминания
  потребителей, модель стоимости и решение. Для каждого файла сохранены SHA-256,
  назначение/символы, семейство, ссылки потребителей и тестов, зависимости, стоимость,
  решение и граница глубины review. JSON является подробным приложением к этому отчёту.
  Дополнительно AST выделил 2805 PowerShell call/import/dispatcher edges:
  `syntacticConsumers` отделяет эти связи от простых текстовых упоминаний;
  flow-insensitive variable bindings помечены явно.
- [Доказательства и воспроизведение](wiki-tools-audit-20261024.evidence.json):
  результаты измерений, непустые retrieval-ответы, fault-injection probes,
  результаты выбранных тестов, исходники одноразовых probes и metadata baseline.
  Исходные логи находятся в `.artifacts/wiki-tools-audit-20261024/`.
- Census охватывает все 391 tracked-файла `.llm-wiki/tools/`, `wiki.ps1`, исходники
  Development MCP и его тестов, `scripts/ai`, `scripts/ci`, workflow entrypoints,
  Husky hooks, их progress helper, cleanup helper и четыре исторических audit scripts
  из `docs/reports`. В 391 входят 346 PowerShell,
  33 Node, 9 C# и 3 `.csproj`. Файлы тестов и библиотеки включены, даже если они
  не являются самостоятельной CLI-командой.
- Граница: это полный файловый/командный census названных областей, а не инвентарь
  каждого transitive NuGet/npm-файла, каждого сценария приложения или внешней
  пользовательской автоматизации. Generated records и Markdown-страницы учтены
  как данные/связи, а не как самостоятельные инструменты. Упоминание в старом
  отчёте, generated quality record, комментарии или тесте не доказывает runtime-вызов.
  Динамические имена, property-based dispatch, SDK test discovery и ручной CLI
  требуют отдельной интерпретации. Эти ограничения указаны и в JSON.
- Семантический анализ выполнен по семействам и критическим границам текущего кода.
  Исчерпывающее чтение каждой ветви всех 99 тысяч строк и исполнение каждой из
  369 команд здесь не заявляются. Формальное доказательство отсутствия дефектов
  также не заявляется. Для редких scheduler/learning-сценариев вердикт остаётся
  «недостаточно данных», а не «не нужен».
- Найден предыдущий [по-файловый аудит](../reports/wiki-tools-audit-2026-10-06/reviews.json).
  Из текущих 550 файлов 270 совпадают с его LF-normalized source SHA-256, 140 изменились,
  140 в нём отсутствуют. Сравнение учитывает нормализацию исходного аудита, а raw
  SHA-256 тоже сохранён. В инвентаре его заметки имеют отдельное историческое
  происхождение и флаг совпадения байтов. Его результаты исполнения не перенесены
  на новый HEAD. Старые WTA-001…WTA-030 помечены там исправленными; они не
  переоткрывались автоматически.

Нативный каталог чата по-прежнему не содержит FoodDiary Development MCP.
Первый diagnostic stdout аудита был усечён. Для отдельной repair-сессии сохранён
самостоятельный diagnostic: `ok=true`, 10 tools, успешный handshake и
`get_server_status`, cwd `D:\FD`. Generation freshness на тот момент была stale.
Это доступность самостоятельного сервера, а не регистрация tools в уже открытом
чате. Работа продолжена через документированный Wiki CLI.

`wiki.ps1 start` с явным путём этого документа выбрал `Documentation`,
`requiresWorkspace=false`. Governed workspace для аудита не создавался. Для ремонта start выбрал feature, medium, requiresWorkspace=false и требует design; checkpoint сохранён с независимыми исходными audit probes. Отдельный evidence bundle хранит substantive review окончательного diff.

## Исходные дефекты до исправлений и оставшиеся направления

### FDW-01, P1: rollback при ошибке первоначального backup теряет generated-файлы

**Закрыто в рабочем дереве; ниже сохранено описание исходного baseline и исходного probe.**

[Invoke-LlmWikiIndexPipeline.ps1](../../.llm-wiki/tools/Invoke-LlmWikiIndexPipeline.ps1),
основной `try/catch`, строки 575–589 и 629–640. `$transactionRoot` назначается
до завершения backup. Если `Copy-Item` завершается ошибкой после копирования лишь
части дерева, `catch` видит существующий transactionRoot, удаляет всё текущее
generated-дерево и восстанавливает неполный backup. Статус `initializing` защищает
отдельный orphan-recovery путь, но не этот `catch`.

Probe исполняет catch-блок, извлечённый AST из baseline исходника, в отдельном
каталоге. Начальные файлы `a.json`, `b.json`; backup содержит только `a.json`,
как после сбоя второй операции копирования. После обработчика остаётся только
`a.json`. Реальная ошибка диска/Copy-Item не инжектировалась во весь pipeline;
воспроизведён точный обработчик на достижимом частичном состоянии.

Исправление: состояние `backupReady`/готовый manifest должно стать обязательным
условием destructive rollback. До публикации полного backup исходное дерево
сохраняется. Нужен поведенческий тест с ошибкой первого и последующего copy,
проверяющий все исходные bytes, а не только наличие marker-строк.

### FDW-02, P1: orphan recovery принимает недокопированный checkpoint за полный

**Закрыто в рабочем дереве; ниже сохранено описание исходного baseline и исходного probe.**

Тот же файл: `Restore-OrphanedIndexTransaction`, строки 60–67, и checkpoint-loop,
строки 605–614. Предыдущий `checkpoint` удаляется, новый создаётся и заполняется
на месте. Если процесс завершается во время copy, старый `state.json` ещё сообщает
о предыдущем завершённом stage, а каталог нового checkpoint уже существует.
Recovery предпочитает его полному первоначальному backup и очищает generated.

Исходный probe вызывает baseline функцию recovery с умершим owner PID, полным backup
`a+b`, частичным checkpoint `a` и прежним completedStage. Получено `a`, файл `b`
утрачен. Это моделирование состояния после прерывания, не end-to-end kill-тест.
Артефакты производные и регенерируемые, но silent partial recovery нарушает
обещание целостного checkpoint и может ломать последующие запросы.

Исправление: immutable versioned checkpoint, проверяемый manifest/hashes и
публикация ссылки из атомарного state только после завершения copy. До публикации
нового checkpoint предыдущий остаётся доступным. Тесты: прерывание после каждой
операции, повреждённый manifest, отсутствующий файл, восстановление предыдущего
полного checkpoint и fallback на полный backup.

Исходный `Test-LlmWikiIndexCheckpoint.ps1` проходил, поскольку проверяет строки
исходника и их порядок. Это полезный wiring guard, но он не проверяет эти два
сценария восстановления. Его зелёный результат не опровергает probes.

### FDW-03, P2: pre-push не передаёт удаления в affected Wiki scope

**Закрыто в рабочем дереве; ниже сохранено описание исходного baseline и исходного probe.**

[pre-push](../../FoodDiary.Web.Client/.husky/pre-push), строка 18:
`git diff --name-only --diff-filter=ACMR "$remote_object_name" "$local_object_name"`.
Удалённые файлы не попадают в `changed_paths`, который передаётся affected index
check и используется для выбора условных проверок. Существующий remote ref с
изменением только удаления может дать пустую Wiki scope. Для нового ref используется
`ls-tree`, это другая ветвь. Последующая retrieval-проверка и CI остаются барьерами;
не утверждается, что дефект полностью обходит CI или все backend/frontend checks.

В отдельном Git index/worktree fixture удалён `a.cs`: `ACMR` возвращает пустую
строку, `ACMRD` возвращает `a.cs`. Проверены именно filter semantics; полный push
не выполнялся. Исправление: включить `D`, сохранить rename source/destination
там, где оба влияют на invalidation, добавить deletion-only и mixed-ref тесты.

### FDW-04, P2: неодинаковые гарантии остановки дочерних процессов

**Закрыто в рабочем дереве; ниже сохранено описание исходного baseline и исходного probe.**

[Invoke-LlmWikiAdaptiveVerification.ps1](../../.llm-wiki/tools/Invoke-LlmWikiAdaptiveVerification.ps1),
строки 25–52, запускает сразу все checks, затем вызывает безусловный `WaitForExit()`.
Нет собственного timeout/cancellation и общего `finally`, останавливающего уже
запущенных siblings при ошибке следующего `Start()`. Dispose процесса его не завершает.
[code-graph-process.mjs](../../.llm-wiki/tools/code-graph-process.mjs) и extractor
`spawnSync` в `code-graph.mjs` также не задают локальный timeout. Внешние bounded
runner/MCP/CI ограничивают часть вызовов, но прямой запуск CLI остаётся без такого
локального предела. Реальное зависание в этой сессии не наблюдалось.

Объединить wrapper с существующим bounded runner, передав deadline вниз и сохранив
исходный exit code, stdout/stderr и cleanup process tree. Не вводить короткий
универсальный timeout для restore/build. Проверять hanging child, failed spawn,
cancelled parent, переполненный stdout и отсутствие живых descendants.

### FDW-05, P2: заполненная telemetry перестала принимать результаты

**Закрыто в рабочем дереве; ниже сохранено описание исходного baseline и исходного probe.**

Во время заключительного `wiki verify` наблюдалось предупреждение
`Verification telemetry retention limit is reached; archive history before recording more events`.
Read-only подсчёт подтвердил 5000 events при policy limit 5000.
`Manage-LlmWikiVerificationTelemetry.ps1`, строки 204–206, отклоняет новую запись;
его доступные actions — `record/list/metrics/verify`, штатного archive action нет.
Это подтверждённый эксплуатационный пробел: последующие cost/failure observations
перестают поступать, хотя исходные execution logs и stage receipts сохраняются.
Warning не заменяет фактический результат проверки и не является доказательством
недостоверности прежних записей.

Нужен поддерживаемый архив с проверяемой связью сегментов, либо явный reviewed
maintenance workflow с сохранением provenance и согласованной обработкой
archived events читателями. Проверять достижение cap, concurrent record/archive,
повреждённый сегмент, восстановление и поведение metrics/cache invalidation.
Первоначальный аудит telemetry не изменял. При ремонте 5000 событий сохранены штатным архивированием.

### FDW-06, P1: force-перезапись state/registry имела окно отсутствующего файла

**Закрыто в повторном анализе.** Проверка IL фактически установленных PowerShell 7
и 5 показала, что force-ветвь FileSystemProvider.MoveFileInfoItem вызывает Delete,
затем MoveTo. Прерывание между ними могло оставить transaction без state либо
telemetry без manifest. Теперь существующий файл публикуется через File.Replace,
начальная публикация — File.Move в отсутствующий путь. Настоящий null backup
передаётся через NullString.Value: обычный PowerShell null преобразуется в пустую
строку и вызывает ошибку API. Последняя особенность обнаружена тестом и исправлена.
Новые assertions проверяют повторную публикацию state и сохранение его bytes при
ошибке sharing; остальные архивные и восстановительные регрессии повторены.
IL-вывод и исходник read-only disassembler сохранены в evidence. Это анализ
достижимого crash window, а не утверждение о наблюдавшейся потере данных в этой сессии.

### Остальные риски, без заявления о воспроизведённой аварии

- Риск повторного использования PID в index recovery закрыт schema-3 state: проверяются PID и process start ticks. Legacy state сохраняет прежнюю совместимость; совпавший живой legacy PID может задержать legacy recovery до завершения процесса.
- `Get-LlmWikiFileSha256` определён и в `LlmWikiIndexCache.ps1`, и в
  `LlmWikiQueryCache.ps1`; поведение для отсутствующего файла различается.
  Dot-sourcing создаёт зависимость от порядка загрузки. Ошибка принятия ложного
  успешного receipt не воспроизведена. Нужен один явно параметризованный контракт
  либо разные имена для strict и optional hashing.
- `Get-LlmWikiCompiledIndexMigration.ps1` выводит `removable` из отсутствия
  текстовых consumers, сканируя только `.ps1/.mjs/.md`. Это диагностический hint,
  не доказательство безопасности удаления; C#, policy JSON и динамические вызовы
  требуют дополнительной проверки. Сейчас наличие consumers защищает данные от
  такого вывода, но контракт флага стоит уточнить.
- Ассessments хешируют task logs и дополнительные scheduler/knowledge registries,
  повторно запрашивают версии `dotnet/npm/node`. Это корректнее слабого cache key,
  но при большой task history и fan-out стоимость растёт. Не измерено ускорение
  всего `task-finish`; это отдельный будущий benchmark.

## Измерения исходного baseline и повторные наблюдения

Windows, PowerShell 7.6.5, Node 26.2.0, .NET SDK 10.0.102. CI использует Node 22;
эти результаты нельзя объявлять CI benchmark. Другие процессы разработки не
останавливались. По три последовательных запуска в свежем subprocess; OS/file
cache не очищался. «Первый» запуск ниже не означает чистую машину или новый checkout.
HEAD и Git status за серию не изменились. Все измеренные команды завершились с 0.

| Операция | Первый запуск, с | Последующие, с | Медиана, с |
| --- | ---: | ---: | ---: |
| `pwsh -NoProfile`, минимальное тело | 0,303 | 0,269 / 0,267 | 0,269 |
| `wiki.ps1 help` | 0,637 | 0,573 / 0,580 | 0,580 |
| fingerprint stage `full verification` | 1,279 | 0,732 / 0,746 | 0,746 |
| Recipes context, `-SkipQueryCache` | 1,368 | 1,494 / 1,356 | 1,368 |
| Тот же context, cache enabled | 1,571 (miss) | 0,880 / 0,959 (hits) | 0,959 |
| `Build-LlmWikiSymbolIndex.ps1 -Check` | 9,839 | 9,664 / 9,665 | 9,665 |
| Batch Git hash ровно symbol-index source set, 4612 файлов | 0,741 | 0,684 / 0,612 | 0,684 |
| Copy всего generated, 47 файлов / 12 422 794 bytes | 0,063 | 0,060 / 0,057 | 0,060 |

Контекст во всех шести ответах содержит 12 кандидатов, один и тот же top path
`Modules/Recipes/Application/Services/RecipeNutritionUpdater.cs`, `fresh=true`,
37 079 indexed documents и одинаковые indexed/current change-set fingerprints.
Cache hits обозначены самим ответом. Медиана двух warm hits 0,919 с, примерно
на 33% ниже медианы без query cache на этом запросе. Stored SQL timings в cached
payload не являются новым измерением SQL. Для первого uncached ответа SQL занял
313 мс, round-trip около 837 мс: startup, freshness и serialization заметны.

Symbol check заново читает 4612 исходников и строит весь результат; `-Check`
сам по себе не означает дешёвый hash check. Сравнение с hash-only не является
готовым ускорением в 14 раз: будущий fast path должен включать discovery,
generator/runtime/policy inputs, output hash и invalidation. Разумная цель
эксперимента для неизменённого набора: 1–3 с вместо 9,7 с; её ещё нужно доказать.

Копирование generated сейчас не объясняет многоминутные проверки. Здесь сначала
нужно исправить корректность восстановления. Прежние ориентиры 371 с для восьми
verify stages и 1202 с для legacy governed audit являются историческими, а не
результатами этой серии. Cold graph rebuild, чистый NuGet/npm restore, полный
task-finish, большие task corpora и Linux/PS5 в этой сессии не benchmarked.

Повторены три последовательных запуска каждой операции тем же harness, без очистки
OS/cache и остановки чужих development процессов. Наблюдения сделаны до заключительной замены операции публикации state/registry; это измерения неизменённых read-команд. Дополнительные audit-файлы входят
в dirty working-tree fingerprint; это не изолированный A/B benchmark.

| Операция | До, медиана с | После, медиана с |
| --- | ---: | ---: |
| `pwsh-startup` | 0.269 | 0.326 |
| `help` | 0.580 | 0.769 |
| `stage-fingerprint` | 0.746 | 1.129 |
| `context-no-query-cache` | 1.368 | 1.750 |
| `context-cache-enabled` | 0.919 | 1.064 |
| `symbol-index-check` | 9.665 | 10.020 |

Все повторные команды завершились успешно. Retrieval снова вернул по 12 непустых
кандидатов с fresh compiled source. Ускорение не заявляется: подтверждён прежний
резерв symbol-index; из этих наблюдений нельзя приписать замедление конкретному алгоритму.

## Инвентарь и решения по семействам

Семейства ниже непересекающиеся для учёта файлов, но вызовы пересекают их границы.
Решение для конкретного файла и его consumers находятся в JSON; модель стоимости
наследуется из семейства, если отдельно не измерена. Прямые test references не
равны line/branch coverage. Общего измеренного coverage percentage для Wiki нет.

### <a id="family-shared-facade"></a>CLI и общие библиотеки — 53 файла

Оставить компактный facade. `wiki.ps1` содержит 369 команд: 86 core, 81 governed,
202 experimental. ValidateSet и command-registry совпадают полностью. 159 switch
clauses группируют команды; `help` обслуживается default branch, `verify-status`
имеет inline logic. Реальные контракты: ручной CLI, MCP adapters, workflow recipes,
test/catalog assertions и automation-oriented request files.

`LlmWikiGitPaths`, `LlmWikiJson`, collections, path/module lookup, source matching,
GitHistory/TestSourceScanner, native compilation и read-only snapshot helpers
нужны. Unicode/NUL transport, batching Windows argv, strict exit codes и
content-addressed Add-Type runtime уже решают реальные ограничения платформы.
PS5 fallback нельзя удалить, пока launcher/пользователи его поддерживают.

Упростить: команды/tier/usage/routing/source dependencies частично описаны в
нескольких местах: ValidateSet, switch, command-registry, help table, smoke patterns,
stage fingerprints, query-index manifest. Генерировать только механическую часть
из проверяемого manifest; policy semantics и tests оставить независимыми.
`Show-LlmWikiCommandHelp` имеет небольшой explicit usage-map; остальным возвращает
generic usage с советом вызвать ошибку пропущенного параметра. Это неполная помощь,
а не недоступность этих команд. Генерация параметров/примеров из AST снизит
поддержку без разрастания daily help.

Стоимость: startup около 0,27 с; facade help около 0,58 с. PowerShell pipelines
и повторный JSON parse/serialize создают allocations, особенно для больших
structured results. Raw JSON transport уже оптимизирован в graph manager;
сохранять его и budget/truncation contracts.

### <a id="family-indexes-graph"></a>Генераторы, индексы, graph и SQLite — 50 файлов

Оставить 12 index generators: catalog, symbols, frontend, frontend-contract,
domain-data, configuration, runtime-topology, sensitive-data, backend-contract,
quality, module-pages, architecture-health. Их данные потребляют страницы Wiki,
compiled queries, task packets, test-plan, retrieval и freshness manifests.
11 JSON query indexes перечислены в `query-indexes.json`; module pages имеют
отдельную форму. Генераторы нельзя удалять вслед за миграцией **reader** на SQLite:
JSON остаётся projection source и Git review artifact.

`Invoke-LlmWikiIndexPipeline` выбирает зависимый набор, разделяет required/analytical
индексы, исполняет stages с concurrency/timeout и хранит cache receipts. Сохранить
dependency propagation и строгий `verify`; FDW-01/02 исправлены manifest snapshots. Полные snapshots
стоят O(G) bytes на checkpoint, но измеренная стоимость мала при нынешнем G.

Node code graph нужен как единственный SQLite writer. Git inventory, до восьми
bounded input readers, dirty-content hash, incremental candidate parsing, SQL
transactions, before/after snapshot fence и corruption recovery уже реализованы.
Пустой/устаревший graph не выдаётся за свежий. File texts всех changed candidates
и extractor JSON удерживаются в памяти: O(суммы changed bytes + graph rows), а
не только O(concurrency). Full Roslyn/TypeScript rebuild дороже warm metadata pass.
Large C# batches переходят к ограниченной семантике; имя/heuristic edge остаётся
navigation lead, не доказательством реального runtime-вызова.

Алгоритмы: source scan обычно O(B); повторное вычисление line number через
`Substring(0, match.Index)` и split в symbol/catalog/frontend generators даёт
O(M·L) на файл вместо одного прохода line offsets O(L + M log L).
Нужно измерить влияние на реальных и длинных generated-like source files, сохранив
линии и Unicode. Backend contract extractor уже использует MultiPatternScanner;
не заменять его на regex-проход по каждому contract и каждому файлу.

Native SQLite reader полезен: CLI связывает общие C# context-search sources с MCP,
исключая отдельную реализацию SQL ranking на этой границе. Node search всё ещё
нужен для graph CLI/batch/parity, его нельзя объявить мёртвым fallback.
FTS recall ограничивает pool перед hydration; compiled-index reads валидируют
source hashes; corpus evaluation использует одну read transaction. SQLite cache
и bounded candidates уже сокращают I/O; не убирать freshness ради скорости.

Покрытие: graph inputs/snapshot/runtime/maintenance tests, Roslyn, source inventory,
index fingerprints/selection/checkpoint/concurrent-update, SQL parity и cold-checkout
tests. Строковые contracts не заменяют runtime fault injection (FDW-01/02).
Кандидаты на объединение migration diagnostics разобраны отдельно в R1.

### <a id="family-discovery-review"></a>Поиск, research, impact и review — 50 файлов

Оставить context/trace/brief/research, graph research/test plan, ownership,
contract/domain/runtime/privacy/security/quality/health queries, precedents,
journeys, dependencies и rollout. Потребители: daily workflow, Development MCP,
task initialization/refresh, review/report и conditional policy obligations.
`security` формирует review evidence, не заменяет vulnerability validation.
`hotspots/test-gaps/debt` — разные виды navigation evidence, не coverage metrics.

Основная стоимость — чтение source/index bytes, freshness и связанные queries.
Ограничение 12 результатов не ограничивает объём первоначального scan.
`Get-LlmWikiToolingContext` намеренно ищет по всем Wiki tools/workflows; для этого
небольшого корпуса простой scan приемлем. Git precedents и regression corpus
всё ещё нужны как редкие исследовательские сценарии.

Упростить: один immutable request snapshot и передача уже построенного packet
по цепочке вместо повторного discovery/parse. Query cache имеет atomic replace,
dependency hashes, runtime/HEAD keys и bounded retention. Сохранить отдельные
гарантии scoped/global snapshots и recovery stale graph. Ranking heuristics и
ASCII-oriented identifier scans имеют пределы recall; возвращаемые confidence,
ambiguity и partial states должны сохраняться, включая смешанный frontend/backend.

### <a id="family-task-delivery"></a>Task lifecycle и delivery — 34 файла

Оставить adaptive develop/start/next/status/design, workspace/baseline/session,
manifest, packet, refresh, pause/resume, handoff/import/export, readiness/report,
delivery status/replan/validate/finalize. Эти инструменты связаны CLI, task
contracts и сценариями длительной работы; для этого документа adaptive route
обоснованно не создал workspace. Совместимые короткие aliases не являются
отдельными ненужными реализациями.

Стоимость: fan-out в `Get-LlmWikiTaskAudit`, `Complete-LlmWikiTaskWorkspace` и
delivery assessments повторяет проверки нескольких JSON artifacts, log hashes,
runtime versions и source snapshots. Упростить DAG вычислений с передачей
проверенного snapshot, но оставить explicit source/evidence fences при публикации.
`Invoke-LlmWikiDeliveryWorkflow.Get-WorkspaceSnapshot` читает **все** workspace files
в byte arrays: O(W) памяти, включая крупные logs/media. Это риск на больших tasks;
измеренного OOM сейчас нет. Disk transaction/journal предпочтительнее расширения
неограниченного memory snapshot.

Покрытие: adaptive routing/experience, task scope/baseline/workspace/handoff,
session resolution, metadata refresh, governed-delivery и full audit shards.
Отдельные protocol states нельзя объединять только из сходства имён: refresh,
replan, assess, seal и verify имеют разные mutation/provenance обязанности.

### <a id="family-evidence-proof"></a>Evidence, acceptance и proof — 14 файлов

Оставить execution evidence, lineage, acceptance matrix, behavior specification,
requirements/proof/phase и substantive review. Они не дублируют app tests:
связывают критерий с текущими source bytes, definition, исходным log/exit,
execution record и review binding. Потребители — task run/finish/delivery,
phase completion, release report и runtime verification adapter.

Исправления baseline учтены: canonical review collections, explicit mapping
retention, exclusive run/recovery ownership, original exit code/hash verification,
receipt import и rollback в recovery уже присутствуют. Нет основания повторять
прежние дефекты как текущие. Историческое evidence из предыдущей сессии имеет
HEAD `deae228…`, а baseline этого аудита `b5e40b9…`; автоматическая совместимость
не заявляется. Эти receipts не переименовывались и не импортировались как новые.

Стоимость — O(scope bytes + log bytes + artifact count). Хеши логов необходимы:
замена на mtime или ручной `passed` ослабила бы доказательство. Оптимизация —
один scoped validation pass и reuse его результата внутри неизменного запроса,
с полным повторным fence при изменении inputs. Review completeness не является
независимой семантической проверкой реализации.

### <a id="family-verification-process"></a>Verification, process и smoke runners — 17 файлов

Оставить affected/parallel smoke, observed stage/worker, stage fingerprints,
verification receipts/cache/plan, native test discovery и process-tree helper.
Потребители: `verify-fast`, strict affected, `verify`, `verify-full`, task checks,
CI и hooks. Разные профили имеют разную силу: affected pass не становится полным
generation/deep verification receipt. Resume допустим только при совпадающем
fingerprint и не превращает failure/blocked в success.

Scheduler учитывает estimated durations и разделяет parallel-safe/stable-graph/
serial groups. Это полезная оптимизация, а не повод вновь запускать monolithic
Full на каждый шаг. Fingerprint `full verification` занимает около 0,75 с и
включает runtime versions; многократные вызовы тоже имеют стоимость.

`Invoke-LlmWikiAdaptiveVerification` теперь использует общий bounded batch (FDW-04/R2 закрыты).
Сохранить изоляцию snapshots, actual execution records, output budgets, UTF-8,
progress heartbeat и исходные exit codes. Внутренние raw Git/hash helpers пока
имеют более слабые timeout/disposal гарантии, чем MCP executor; это приоритет
для унификации deadline propagation.

### <a id="family-orchestration"></a>Scheduler, agents, dispatch и leases — 13 файлов

Недостаточно данных для удаления. GetTaskGraph/Schedule, TaskLease/Dispatch,
AgentRegistry, SchedulePlan, OrchestrationCycle, Watchdog, WorkspaceCircuit и
TaskDecomposition имеют CLI/policy/test consumers. Отсутствие активных агентов
этого аудита ничего не говорит об их необходимости в параллельных автоматизациях.
Здесь scheduler — repository task dispatcher, а не scheduler desktop-приложения.

Сохранить atomic leases и stable lock identity: предыдущие race fixes уже есть.
Стоимость: чтение T workspace registries и doctor/lineage для каждого, построение
graph и сортировка ready candidates. Вызов doctor внутри цикла может усиливать
цену до T повторных scoped validations. Pairwise scope-conflict checks зависят от
числа ready tasks; нужно benchmark T=1/10/100 с фиксированными artifacts, а не
ускорять удалением конфликтных checks. Улучшение — batch snapshot/lookup maps,
короткие critical sections, explicit PID lifetime и deterministic AsOfUtc.
Удаление требует проверки внешних dispatch clients и незавершённых receipts.

### <a id="family-learning-evals"></a>Learning, evals, metrics и experiments — 27 файлов

Оставить failure knowledge, deterministic regression evals/corpora, outcome
telemetry, retrospectives и reviewed promotion/rollback. Потребители: research,
verification plan/cost/risk, task finish, context ranking experiments, CI/focused
smoke и knowledge promotion. `verification-cost`, `failure-prediction`, confidence
и model routing дают оценку/совет, а не доказательство выполнения или качества.

Упростить optional цепочки context strategy/benchmark/experiment, instruction
experiments, canary/learning health и model outcome calibration. 202 experimental
CLI actions обозначают большой API surface, но сами по себе не доказывают runtime
накладные расходы: многие запускаются только явно. Сначала измерить, какие стадии
реально обязательны при ordinary task finish, и цену каждой на пустой/малой/
большой ledger. Сохранить provenance, review/approval и rollback существующих
promotions; performance experiment не вправе автоматически менять policy.

Цена — JSON ledger validation, joins/outcome aggregation и repeated corpus calls.
Similarity `find` сравнивает выбранную задачу с историей; его нельзя автоматически
называть O(T²). Pairwise batch operations нужно оценивать отдельно по их циклам.
Коллекции лежат в Git/local registries, есть риск
неограниченного накопления истории и повторного чтения. Использовать уже имеющиеся
prune/retention и versioned snapshots; новое «обучение» не следует добавлять в
обязательную цепочку без измеримого качества.

### <a id="family-tests"></a>Тесты и тестовые оболочки — 182 файла

Оставить тесты. В эту категорию входят PowerShell contracts/fixtures, Node unit
tests, .NET MCP tests и их helpers. SDK/glob discovery — реальный потребитель
даже при нуле явных filename references. Не удалять тест как «никем не вызываемый».

`Test-LlmWikiTools.ps1` — 5382 строки legacy audit. Focused runner не равен Full,
а frozen assertion inventory охраняет переносы. CI сейчас использует Full shards
Core, Workspace, Orchestration и aggregate gate. Удаление monolithic файла до
поведенческого переноса всех assertions ослабит покрытие. Разбивать по owning
contract и reusable fixtures, сохранять assertion identity/negative cases и
сравнивать completeness, не только число зелёных тестов. Строковые source checks
оставлять wiring guards, дополняя их fault-injection на stateful boundaries.

Стоимость тестов нельзя оценить числом файлов или references. Есть subprocess,
shared-clone, compiler, browser и full task fixtures. Исторические минуты не
смешивались с текущими timings. Изолированные logs/outcome registries важны:
предыдущие fixture-contamination исправления не следует отменять ради скорости.

### <a id="family-mcp"></a>Development MCP — 57 файлов

Оставить transport и in-process SQLite query layer. Production sources используют
MCP stdio tools, Wiki query cache/executor, Git snapshots/runtime identity,
SQLite context/flow readers, bounded Node task adapter и protocol result types.
Тестовые consumers перечислены в JSON. Native registration в этом чате отсутствует;
это не делает server code ненужным.

Зависимости: .NET 10, Microsoft.Data.Sqlite 10.0.11,
Microsoft.Extensions.Hosting 10.0.11, ModelContextProtocol 2.2.0; Roslyn extractor
использует Microsoft.CodeAnalysis.CSharp 5.9.0. Версии взяты из текущего
Directory.Packages.props. Node writer использует `node:sqlite`, TypeScript compiler
из frontend dependency root; полный graph требует frontend dependencies.
Независимых production HTTP/DB credentials для Wiki queries не требуется.

PowerShell executor ограничивает concurrent commands, timeout 3 минуты, output
budget, temp request transport, cancellation и process-tree cleanup. QueryCache
имеет TTL 2 минуты, максимум 128 entries и 1 Mi character payload; это character
limit, а не гарантия 1 MiB памяти. Strings плюс parsed JSON могут занимать больше.
ChangeSetSnapshotService использует watcher/generation, current HEAD check и
scoped projection; watcher overflow должен инвалидировать cached snapshot.
Readonly corpus readers удерживают один SQLite snapshot; interactive freshness
fences остаются необходимыми.

Не удалять Node ranking из-за наличия C#: CLI/parity его ещё используют. Более
дальняя унификация ranking допустима после сохранения SQL/identity/ambiguity/
Unicode/timeout contracts на Node 22 и локальном runtime. Параллельные misses
дедуплицируются; отмена одного waiter не должна отменять owner остальных.
Startup/build-if-stale и dependency hashes исправлены в baseline; focused
runtime test снова прошёл с missing dependency и same-time source edits.

### <a id="family-adjacent-runtime"></a>Соседние agent/runtime инструменты — 40 файлов

Оставить task-runtime/task-tools, owned runtime/fixtures/requests, frontend identity,
runtime diagnostics/redaction, lease/resource budgets, journey evidence и
verification bridges в их owning workflows. Coding probes/trials, load/k6,
recognition/reliability/recovery и feature generators имеют отдельные manual/
package/test consumers; это не автоматически «лишняя часть Wiki».

В этом аудите учтены wiring и boundaries, но не запускались реальные mail/provider,
browser/load/end-to-end journeys и не исследовалась вся product semantics этих
соседних средств. Стоимость зависит от внешнего runtime; environment/lease/source
identity и bounded output сохраняются. Их возможное удаление требует отдельного
owner review, а не выводов по частоте Wiki commands.

### <a id="family-ci-hooks"></a>CI и hooks — 9 файлов

Оставить ci-tests/ai-behavior/deploy entrypoints как внешние потребители,
backend CI wrapper, Wiki aggregate assertions, pre-commit/pre-push/progress и
scoped cleanup. Pre-commit выполняет только staged whitespace check. Pre-push
делает affected indexes, retrieval corpus и parallel frontend/backend checks;
backend outputs изолированы в `.artifacts/pre-push/<pid>`. FDW-03 исправлен и покрыт настоящим hook fixture.

CI focused Wiki требует полный focused catalog, audit matrix сохраняет legacy
coverage; итоговый Wiki job зависит от обоих и fail-closed трактует применимые
результаты. MCP process suite вынесен из локального push в CI slow group. Это
осознанная маршрутизация, не основание удалять suite. Измерять wall time и runner
minutes отдельно, не вызывать CI из этого аудита. Workflow deployment logic
проинвентаризирован как consumer, deployment в данной сессии не выполнялся.

### <a id="historical-audits"></a>Исторические audit helpers — 4 файла

`docs/reports/wiki-analysis-2026-10-03/Measure-WikiAudit.ps1`,
`Test-DirtySnapshotAudit.ps1` и `docs/reports/wiki-tools-audit-2026-10-06/Build-Inventory.ps1`,
`Test-Coverage.ps1` обслуживают прежние отчёты. Они выделены отдельно, чтобы ручные
исторические entrypoints не исчезли из census. `Test-Coverage` проверяет frozen
ledger против текущего source и закономерно устаревает при изменении кода;
его старый зелёный результат не является текущим coverage proof.
Операционного policy/CI consumer этих четырёх файлов не установлено. Кандидаты
на удаление/архивирование после сохранения воспроизводимых данных и замены manual
audit workflow; до этого не выдавать их за текущий обязательный gate.

## Удаление, объединение и упрощение

| Кандидат | Доказательство и оставшиеся потребители | Решение и условие |
| --- | --- | --- |
| R1: `Get-LlmWikiCompiledIndexMigration`, `Measure-LlmWikiStandaloneIndexRoutes` | Все 11 query indexes уже имеют migrated route; scripts всё ещё читают/проверяют workflow guides, holdout corpus, graph/standalone tests и ручной диагностический CLI. Старые audit JSON только упоминают их | Объединить migration-status с текущим catalog/freshness diagnostics; benchmark перенести в существующий benchmark harness. Удалять старые entrypoints после переноса callers, corpus anchors и сохранения parity/cold-reader проверки |
| R2: `Invoke-LlmWikiAdaptiveVerification` — выполнено | Вызывается `Invoke-LlmWikiAffectedSmoke`, есть smoke policy и regression consumers; та же архитектура bounded parallel stages уже существует | Оболочка переведена на общий bounded batch; сохранены routing/experience/integration/eval shards, scope, stdout/stderr и exit semantics; целевые регрессии и общий gate проверяют совместимость |
| R3: legacy `Test-LlmWikiTools` | Прямой CI Full matrix и frozen assertion inventory; отдельные assertions пока не заменены owning tests | Сейчас оставить. Постепенно перенести assertions; удалять файл только после проверяемого coverage map и удаления CI consumers. Сходство с focused smoke не доказывает дублирование assertions |
| R4: scheduler/dispatch/model/context/instruction experimental families | Registry, policy, task-finish или explicit command/test wiring есть; данные о пользовательской полезности неполны | Сохранить compatibility, отделить optional stages, измерить пользу/стоимость. Sunset возможен лишь после проверки внешних consumers и миграции durable ledgers/active leases |
| R5: raw JSON indexes / Node search / PS5 branches | Projection inputs, Git review, CLI/parity и launcher compatibility | Не кандидаты на немедленное удаление. Reader migration не устраняет writer/source; поддержка runtime снимается только отдельным совместимым решением |
| R6: четыре исторических audit helpers в `docs/reports` | Frozen reports/ledgers и manual runs; текущий policy/CI caller не найден | Кандидаты на архивирование/удаление после сохранения evidence и переноса ручных сценариев в один актуальный audit harness. Отдельно от production-инструментов |

Критерий безопасного удаления: отсутствует обязательный policy/registry/CI/hook/
MCP consumer, динамический dispatch разобран, manual/редкий recovery сценарий
заменён, active state мигрирован, negative/parity/rollback coverage сохранено,
объявлен совместимый переход CLI. Недавние execution logs служат дополнительным
сигналом, а не единственным критерием. На этой стадии список «удалить сейчас» пуст.

## Оставшийся приоритетный план

Оценки трудоёмкости — engineering estimates, не обязательства. Эффект вне
измеренной таблицы является гипотезой и должен подтверждаться перед/после.

| Приоритет | Работа | Ожидаемый эффект | Оценка | Зависимости и проверка |
| --- | --- | --- | --- | --- |
| P2 | Content-addressed symbol-index `-Check` fast path | Цель 1–3 с вместо 9,7 с для неизменённых 4612 файлов | 1 день | Key включает source set/content, generator/shared/runtime inputs и output hash. Same-size/time edits, deletion/rename, missing/corrupt output, Unicode; compare canonical full generation |
| P2 | Единый request snapshot и memoized assessments внутри одного invocation | Меньше повторных Git/version/log hash/subprocess вызовов в audit/finish | 2–3 дня | Сначала stage counters/bytes/starts на fixed small/large task. Source/log/policy mutation между phases обязана инвалидировать; запрет cross-request weak reuse |
| P3 | Общие strict/optional hashing и line-offset utility | Меньше load-order ambiguity; O(L+M log L) вместо O(M·L) | 1 день | Missing-file behavior, CRLF/LF, Cyrillic/surrogates, multi-declaration/DI files, identical path+line outputs; microbench плюс real corpus |
| P3 | R1: вывести завершённые migration diagnostics из отдельного tooling surface | Меньше duplicate scanner/benchmark maintenance | 0,5–1 день | Перенос всех workflow/test/corpus references, dynamic/manual replacement, parity/cold-reader остаются |
| P3 | Registry-derived mechanical help/dispatch metadata | Меньше согласуемых вручную таблиц и generic command help | 1–2 дня | Все 369 команд, alias/parameter/request-file compatibility; независимые catalog/critical contracts сохраняются |
| P3 | Batch scheduler snapshots и ограничение ledger/history growth | Контролируемая цена при десятках/сотнях tasks | 2–4 дня | T=1/10/100, contention/crash/reused PID, deterministic scheduling; external dispatch compatibility |
| P3 | Разбить legacy audit по owning boundary с inventory of assertions | Проще fixtures и точнее affected selection; выигрыш времени пока не доказан | 3–5 дней | Frozen assertion identity, failed/negative paths, same fixtures/results, CI matrix equivalence; не вычёркивать медленные assertions |

Не следует начинать с общего переписывания Wiki на один язык: затраты сейчас
сконцентрированы в нескольких границах, а существующие Node/C#/PowerShell роли
имеют практическую причину. Замена всех scripts увеличит объём повторного
доказательства корректности раньше, чем станет известен выигрыш.

## История исходных проверок и сохранение результатов

В первоначальном аудите прошли шесть Node tests `code-graph-inputs`/`code-graph-dotnet-runtime`,
включая реальный rebuild при отсутствующей Roslyn dependency и same-time edits;
`Test-LlmWikiGitPaths` с Unicode/NUL/worktree/exit cases; существующий checkpoint
source-contract test. Два recovery probes и deletion probe подтвердили описанные
пропуски. Это проверки аудируемых границ, а не полный suite приложения.

Первый заключительный verify прошёл шесть стадий и остановился на source-impact:
добавление отчёта изменило documentation catalog, являющийся source для
`workflow.query-context`. Страница и двухстрочный catalog diff проверены;
штатный `review` записывает обоснование её актуальности. Этот отказ сохранён
отдельно, его exit code не переименован в success. FDW-05 обнаружен в том же запуске.

Исходная заключительная Wiki-проверка и список изменённых файлов сохранены в локальном
`.artifacts/wiki-tools-audit-20261024/final-verification.json` после заморозки
трёх tracked-документов отчёта. Путь указан в evidence JSON; это позволяет не менять
source fingerprint проверенных документов ради добавления результата проверки. Повторная проверка ремонта имеет отдельный receipt: `.artifacts/wiki-tools-repair-20261024/final-verification.json`.
Проверяются JSON/schema/paths/counts, source hashes,
ссылки отчёта, UTF-8 и `git diff --check`. При необходимости обновляются только
обусловленные добавлением документа generated-индексы. Старые receipts не
становятся доказательствами нового отчёта; новые результаты не подменяют их.
