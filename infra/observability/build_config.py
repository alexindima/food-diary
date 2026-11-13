#!/usr/bin/env python3
"""Generate the operational rules/dashboards. Requires PyYAML 6.0.3."""
import json
import math
import pathlib
import yaml

ROOT = pathlib.Path(__file__).resolve().parent
PROM = "cfhlwwx2t4w00a"
LOKI = "cfhlezxl8fdvka"
API = 'exported_job="FoodDiary.Web.Api",url_path=~"/api/.*",url_path!~"/api/v[^/]+/auth/telegram/bot/operations/.*|/api/v[^/]+/logs"'
JOB_DEADLINE = '(fooddiary_job_last_success_age_seconds > on(fooddiary_job_name) (2*fooddiary_job_expected_interval_seconds+300)) or ((fooddiary_job_expected_interval_seconds unless on(fooddiary_job_name) fooddiary_job_last_success_age_seconds) and on(fooddiary_job_name) ((time()-scalar(max(fooddiary_container_started_timestamp_seconds{service="job-manager"}))) > fooddiary_job_expected_interval_seconds*2+300))'

# Keep internal rule titles stable for routing and existing alert identities.
# Operator-facing Telegram text comes from these Russian annotations.
NOTIFICATION_TITLES = {
    "fd-api-user-errors": "Ошибки пользовательского API превышают 2%",
    "fd-api-user-slow": "p95 чтения пользовательского API превышает 1500 мс",
    "fd-otel-unavailable": "Недоступен сборщик метрик приложения",
    "fd-monitoring-service-down": "Не удаётся получить метрики службы мониторинга",
    "fd-api-metrics-missing": "Перестали поступать метрики API",
    "fd-job-metrics-missing": "Перестали поступать метрики фоновых заданий",
    "fd-host-probes-stale": "Инфраструктурные проверки не завершались более 3 минут",
    "fd-host-probe-failed": "Не удалось выполнить инфраструктурную проверку",
    "fd-job-schedule-missed": "Фоновое задание пропустило срок успешного выполнения",
    "fd-container-missing": "Обязательный контейнер приложения не запущен",
    "fd-container-unhealthy": "Проверка здоровья контейнера завершилась ошибкой",
    "fd-container-restarted": "Контейнер приложения недавно перезапустился",
    "fd-container-oom": "Контейнер остановлен из-за ограничения памяти",
    "fd-kernel-oom": "Linux завершил процесс из-за нехватки памяти",
    "fd-db-command-errors": "Ошибки выполнения команд базы данных",
    "fd-backup-old": "Проверенной резервной копии PostgreSQL более 26 часов",
    "fd-backup-corrupt": "Не совпала контрольная сумма резервной копии PostgreSQL",
    "fd-pg-locks": "Сеансы PostgreSQL ожидают блокировки",
    "fd-pg-long-transaction": "Транзакция PostgreSQL длится более 120 секунд",
    "fd-pg-connections": "Использовано более 80% подключений PostgreSQL",
    "fd-redis-evictions": "Redis удаляет ключи из-за ограничения памяти",
    "fd-rabbit-no-consumers": "В RabbitMQ есть сообщения без обработчиков",
    "fd-rabbit-backlog": "В очереди RabbitMQ более 100 готовых сообщений",
    "fd-tls-expiry": "Сертификат TLS истекает менее чем через 14 дней",
    "fd-public-probe-down": "Публичный HTTP-адрес недоступен с production-сервера",
    "fd-api-budget-fast": "Быстро расходуется бюджет доступности пользовательского API",
    "fd-api-budget-slow": "Устойчиво расходуется бюджет доступности пользовательского API",
    "fd-telegram-intake-stalled": "Нет подтверждений успешного опроса Telegram",
    "fd-telegram-errors": "Повторные ошибки транспорта Telegram",
}


def write_json(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")


def ratio(window):
    eligible = f'sum(rate(fooddiary_api_requests_total{{{API},http_response_status_code!~"4.."}}[{window}]))'
    errors = f'sum(rate(fooddiary_api_requests_total{{{API},http_response_status_code=~"5.."}}[{window}]))'
    return f'({errors} or (0 * {eligible})) / ({eligible} > 0)'


def price_vector(kind):
    prices = json.loads((ROOT / "ai-model-prices.json").read_text())
    rows = []
    for model, rates in prices["models"].items():
        value = float(rates[kind])
        if not math.isfinite(value) or value <= 0:
            raise ValueError("A confirmed positive model price is required")
        rows.append(f'label_replace(vector({value:g}), "fooddiary_ai_model", {json.dumps(model)}, "__name__", ".*")')
    return " or ".join(rows)


def cost_expression(window="$__range", model="$model"):
    terms = []
    for kind in ["input", "output"]:
        tokens = f'sum by(fooddiary_ai_model)(increase(fooddiary_ai_tokens_total{{fooddiary_ai_model=~"{model}",fooddiary_ai_token_type="{kind}"}}[{window}]))'
        terms.append(f'({tokens} * on(fooddiary_ai_model) ({price_vector(kind)}))')
    return "(" + " + on(fooddiary_ai_model) ".join(terms) + ") / 1000000"


def alert(uid, title, expr, threshold=0, pending="5m", severity="warning", service="application", no_data="OK", datasource=PROM):
    source_type = "loki" if datasource == LOKI else "prometheus"
    # Rule labels override query labels in Grafana. Keep container service
    # identity intact, otherwise all workloads collapse into one alert instance.
    labels = {"severity": severity, "scope" if service == "containers" else "service": service}
    dimension = ("service" if service == "containers" else
                 "database" if service in {"backup", "postgresql"} and uid != "fd-db-command-errors" else
                 "target" if service in {"tls", "availability"} else
                 "component" if uid == "fd-host-probe-failed" else
                 "fooddiary_job_name" if uid == "fd-job-schedule-missed" else None)
    notification_title = NOTIFICATION_TITLES[uid]
    summary = notification_title + (": {{ $labels." + dimension + " }}" if dimension else "")
    return {
        "uid": uid, "title": title, "condition": "C",
        "data": [
            {"refId": "A", "datasourceUid": datasource, "relativeTimeRange": {"from": 900, "to": 0},
             "model": {"datasource": {"type": source_type, "uid": datasource}, "expr": expr,
                       "instant": True, "range": False, "refId": "A", "editorMode": "code", "queryType": "instant"}},
            {"refId": "B", "datasourceUid": "__expr__", "relativeTimeRange": {"from": 0, "to": 0},
             "model": {"type": "reduce", "expression": "A", "reducer": "last", "refId": "B"}},
            {"refId": "C", "datasourceUid": "__expr__", "relativeTimeRange": {"from": 0, "to": 0},
             "model": {"type": "threshold", "expression": "B", "refId": "C", "conditions": [
                 {"evaluator": {"type": "gt", "params": [threshold]}, "operator": {"type": "and"},
                  "query": {"params": ["C"]}, "reducer": {"type": "last", "params": []}, "type": "query"}]}},
        ],
        "noDataState": no_data, "execErrState": "Error", "for": pending, "isPaused": False,
        "labels": labels,
        "annotations": {"notification_title": notification_title, "summary": summary, "runbook": "infra/observability/README.md"},
    }


def panel(title, expr, unit="short", datasource=PROM, description=""):
    expressions = expr if isinstance(expr, list) else [expr]
    return {"title": title, "type": "timeseries", "description": description,
            "datasource": {"type": "loki" if datasource == LOKI else "prometheus", "uid": datasource},
            "targets": [{"expr": value, "refId": chr(65+i)} for i, value in enumerate(expressions)],
            "fieldConfig": {"defaults": {"unit": unit}, "overrides": []}, "options": {"legend": {"displayMode": "list", "placement": "bottom"}}}


def dashboard(uid, title, panels, variables=None):
    for index, item in enumerate(panels):
        item.update({"id": index+1, "gridPos": {"x": (index % 2)*12, "y": (index//2)*8, "w": 12, "h": 8}})
    return {"uid": uid, "title": title, "schemaVersion": 39, "version": 1, "refresh": "1m",
            "tags": ["fooddiary", "observability"], "timezone": "browser", "time": {"from": "now-6h", "to": "now"},
            "templating": {"list": variables or []}, "panels": panels}


def generate():
    records = [{"record": "fooddiary:api_error_ratio_"+window, "expr": ratio(window)} for window in ["5m", "30m", "1h", "6h"]]
    records.append({"record": "fooddiary:api_eligible_requests_5m", "expr": f'sum(increase(fooddiary_api_requests_total{{{API},http_response_status_code!~"4.."}}[5m]))'})
    records.append({"record": "fooddiary:job_success_deadline_exceeded", "expr": JOB_DEADLINE})
    (ROOT / "prometheus/fooddiary-recording.yml").write_text(yaml.safe_dump({"groups": [{"name": "fooddiary-user-api", "interval": "30s", "rules": records}]}, sort_keys=False), encoding="utf-8", newline="\n")
    # The optional Prometheus paging rules share the same recording definitions
    # as Grafana. Loading both files must not create duplicate recording rules.
    slo_path = ROOT / "prometheus/fooddiary-slo-alerts.yml"
    slo = yaml.safe_load(slo_path.read_text())
    for group in slo["groups"]:
        group["rules"] = [rule for rule in group["rules"] if "record" not in rule]
        for rule in group["rules"]:
            if rule.get("alert") == "FoodDiaryApiAvailabilityBudgetFastBurn":
                rule["expr"] = '(fooddiary:api_error_ratio_1h > 14.4 * 0.001) and (fooddiary:api_error_ratio_5m > 14.4 * 0.001) and on() (fooddiary:api_eligible_requests_5m > 10)'
            elif rule.get("alert") == "FoodDiaryApiAvailabilityBudgetSlowBurn":
                rule["expr"] = f'(fooddiary:api_error_ratio_6h > 6 * 0.001) and (fooddiary:api_error_ratio_30m > 6 * 0.001) and on() (sum(increase(fooddiary_api_requests_total{{{API},http_response_status_code!~"4.."}}[30m])) > 50)'
    slo_path.write_text(yaml.safe_dump(slo, sort_keys=False), encoding="utf-8", newline="\n")
    backend_rules_path = ROOT / "prometheus/fooddiary-backend-alerts.yml"
    backend_rules = yaml.safe_load(backend_rules_path.read_text())
    for group in backend_rules["groups"]:
        for rule in group["rules"]:
            if rule.get("alert") == "FoodDiaryApiServerErrorRatioHigh":
                rule["expr"] = '(fooddiary:api_error_ratio_5m > 0.02) and on() (fooddiary:api_eligible_requests_5m >= 10)'
    backend_rules_path.write_text(yaml.safe_dump(backend_rules, sort_keys=False), encoding="utf-8", newline="\n")
    tests_path = ROOT / "prometheus/fooddiary-alert-tests.yml"
    tests = yaml.safe_load(tests_path.read_text())
    if "fooddiary-recording.yml" not in tests["rule_files"]:
        tests["rule_files"].insert(0, "fooddiary-recording.yml")
    for test in tests["tests"]:
        for series in test.get("input_series", []):
            if series["series"].startswith("fooddiary_api_requests_total{") and "exported_job" not in series["series"]:
                series["series"] = series["series"].replace("{", '{exported_job="FoodDiary.Web.Api",url_path="/api/v1/products",', 1)
    tests_path.write_text(yaml.safe_dump(tests, sort_keys=False), encoding="utf-8", newline="\n")

    rules = [
        alert("fd-api-user-errors", "User API errors exceed 2%", '(fooddiary:api_error_ratio_5m > bool 0.02) and on() (fooddiary:api_eligible_requests_5m >= 10)', severity="critical", service="api"),
        alert("fd-api-user-slow", "User read API p95 exceeds 1500 ms", f'histogram_quantile(0.95, sum by(le) (rate(fooddiary_api_request_duration_milliseconds_bucket{{{API},http_response_status_code!~"4..",url_path=~".*/(products|recipes|meals|dashboard).*"}}[10m]))) and on() (sum(increase(fooddiary_api_request_duration_milliseconds_count{{{API},http_response_status_code!~"4..",url_path=~".*/(products|recipes|meals|dashboard).*"}}[10m])) >= 20)', 1500, "10m", service="api"),
        alert("fd-otel-unavailable", "Application metric collector unavailable", '1 - up{job="otel-collector"}', pending="2m", severity="critical", service="telemetry", no_data="Alerting"),
        alert("fd-monitoring-service-down", "Monitoring service scrape is failing", '1-up{job=~"prometheus|loki|promtail|otel-internal"}', pending="2m", severity="critical", service="telemetry", no_data="Alerting"),
        alert("fd-api-metrics-missing", "API runtime metrics stopped", 'absent_over_time(dotnet_process_memory_working_set_bytes{exported_job="FoodDiary.Web.Api"}[5m]) or vector(0)', pending="2m", severity="critical", service="telemetry"),
        alert("fd-job-metrics-missing", "JobManager metrics stopped", 'absent_over_time(fooddiary_job_last_success_age_seconds[5m]) or vector(0)', pending="2m", severity="critical", service="telemetry"),
        alert("fd-host-probes-stale", "Infrastructure probes have not completed for 3 minutes", 'time()-fooddiary_collector_last_run_timestamp_seconds', 180, "1m", "critical", "telemetry", "Alerting"),
        alert("fd-host-probe-failed", "An infrastructure probe is failing", '1-fooddiary_collector_component_success', pending="3m", severity="critical", service="telemetry", no_data="Alerting"),
        alert("fd-job-schedule-missed", "Recurring job missed its success deadline", 'fooddiary:job_success_deadline_exceeded', pending="2m", severity="critical", service="job-manager"),
        # NoData must reset this timer: telemetry failure before a brief planned
        # stop must not count as three minutes of measured container downtime.
        # Separate probe-failure/staleness rules retain critical coverage.
        alert("fd-container-missing", "Required application container is not running", '1-fooddiary_container_running', pending="3m", severity="critical", service="containers"),
        # Exclude only MailInbox during the same startup grace as its Docker health check.
        alert("fd-container-unhealthy", "Application container health check is failing", '(fooddiary_container_running-fooddiary_container_healthy) unless (time()-fooddiary_container_started_timestamp_seconds{service="mail-inbox"} < 120)', pending="3m", severity="critical", service="containers"),
        alert("fd-container-restarted", "Application container restarted recently", 'delta(fooddiary_container_restarts[10m]) > bool 0', pending="0m", service="containers"),
        alert("fd-container-oom", "Container was killed by the memory limit", 'fooddiary_container_oom_killed', pending="0m", severity="critical", service="containers"),
        alert("fd-kernel-oom", "Linux OOM killer terminated a process", 'increase(node_vmstat_oom_kill[10m])', pending="0m", severity="critical", service="infrastructure"),
        alert("fd-db-command-errors", "Database commands are failing", 'sum(increase(fooddiary_db_command_failures_total{error_type!="canceled"}[5m]))', pending="0m", service="postgresql"),
        alert("fd-backup-old", "Verified PostgreSQL backup is older than 26 hours", 'time()-fooddiary_backup_last_success_timestamp_seconds', 93600, "5m", "critical", "backup", "Alerting"),
        alert("fd-backup-corrupt", "PostgreSQL backup checksum verification failed", '1-fooddiary_backup_verified', pending="1m", severity="critical", service="backup", no_data="Alerting"),
        alert("fd-pg-locks", "PostgreSQL sessions are waiting for locks", 'fooddiary_postgres_waiting_locks', pending="5m", service="postgresql"),
        alert("fd-pg-long-transaction", "PostgreSQL transaction is older than 120 seconds", 'fooddiary_postgres_oldest_transaction_seconds', 120, "5m", service="postgresql"),
        alert("fd-pg-connections", "PostgreSQL connection usage exceeds 80%", 'fooddiary_postgres_connections/fooddiary_postgres_connections_max', .8, "10m", service="postgresql"),
        alert("fd-redis-evictions", "Redis is evicting keys", 'increase(fooddiary_redis_evicted_keys_total[15m])', pending="1m", service="redis"),
        alert("fd-rabbit-no-consumers", "RabbitMQ has queued messages without consumers", 'fooddiary_rabbitmq_queues_without_consumers', pending="5m", severity="critical", service="rabbitmq"),
        alert("fd-rabbit-backlog", "RabbitMQ ready queue exceeds 100 messages", 'fooddiary_rabbitmq_messages_ready', 100, "10m", service="rabbitmq"),
        alert("fd-tls-expiry", "Public TLS certificate expires within 14 days", '1209600-fooddiary_public_probe_tls_seconds_remaining', pending="10m", service="tls"),
        alert("fd-public-probe-down", "Public HTTP probe fails from production", '1-fooddiary_public_probe_success', pending="5m", service="availability"),
        alert("fd-api-budget-fast", "User API availability budget burns rapidly", '(fooddiary:api_error_ratio_1h > bool 0.0144) * (fooddiary:api_error_ratio_5m > bool 0.0144) and on() (fooddiary:api_eligible_requests_5m > 10)', pending="0m", severity="critical", service="api"),
        alert("fd-api-budget-slow", "User API availability budget burns steadily", '(fooddiary:api_error_ratio_6h > bool 0.006) * (fooddiary:api_error_ratio_30m > bool 0.006) and on() (fooddiary:api_eligible_requests_5m > 10)', pending="0m", service="api"),
    ]
    rules.extend([
        alert("fd-telegram-intake-stalled", "Telegram has no successful intake heartbeat", 'sum(absent_over_time({compose_service="telegram-bot"} |= "Telegram loop telemetry intake." |~ "SuccessfulPolls=[1-9][0-9]*;" [5m])) or vector(0)', pending="2m", severity="critical", service="telegram-bot", no_data="Alerting", datasource=LOKI),
        alert("fd-telegram-errors", "Telegram transport repeatedly fails", 'sum(count_over_time({compose_service="telegram-bot"} |~ "Telegram (intake paused|work discovery failed|operation .* will resume|bot error|API error)" [10m])) or vector(0)', 5, "2m", service="telegram-bot", datasource=LOKI),
    ])
    (ROOT / "grafana/fooddiary-operational-alerting.yml").write_text(yaml.safe_dump({"apiVersion": 1, "groups": [{"orgId": 1, "name": "FoodDiary Operational Coverage", "folder": "Infrastructure", "interval": "1m", "rules": rules}]}, sort_keys=False, allow_unicode=True), encoding="utf-8", newline="\n")

    original_path = ROOT / "grafana/fooddiary-backend-alerting.yml"
    original = yaml.safe_load(original_path.read_text(encoding="utf-8"))
    for rule in original["groups"][0]["rules"]:
        if rule["uid"] != "fd-outbox-dead-letter":
            rule["data"][0]["model"]["expr"] = rule["data"][0]["model"]["expr"].replace(" or vector(0)", "")
            rule["noDataState"] = "Alerting"
    original_path.write_text(yaml.safe_dump(original, sort_keys=False), encoding="utf-8", newline="\n")

    backend_path = ROOT / "grafana/fooddiary-backend-reliability.json"
    backend = json.loads(backend_path.read_text(encoding="utf-8"))
    backend["panels"][0]["targets"][0]["expr"] = 'fooddiary:api_error_ratio_5m'
    backend["panels"][0]["description"] = 'User API requests only; health, telemetry and internal bot polling excluded. No traffic remains No data.'
    for item in backend["panels"]:
        if item["title"] == "API latency":
            for target in item["targets"]:
                target["expr"] = target["expr"].replace('fooddiary_api_request_duration_milliseconds_bucket[', 'fooddiary_api_request_duration_milliseconds_bucket{'+API+'}[')
                if 'http_response_status_code!~"4.."' not in target["expr"]:
                    target["expr"] = target["expr"].replace('url_path=~"/api/.*",', 'http_response_status_code!~"4..",url_path=~"/api/.*",')
    write_json(backend_path, backend)

    infra_panels = [
        panel("Running containers", "fooddiary_container_running"),
        panel("Healthy containers", "fooddiary_container_healthy"),
        panel("Container CPU (busy cores)", "rate(fooddiary_container_cpu_usage_seconds_total[5m])", "short"),
        panel("Container working set", "fooddiary_container_memory_working_set_bytes", "bytes"),
        panel("Container restart snapshots", "fooddiary_container_restarts"),
        panel("Infrastructure probe failures", "1-fooddiary_collector_component_success"),
        panel("PostgreSQL connections / configured maximum", ["fooddiary_postgres_connections", "fooddiary_postgres_connections_max"]),
        panel("PostgreSQL lock waits / idle transactions", ["fooddiary_postgres_waiting_locks", "fooddiary_postgres_idle_in_transaction"]),
        panel("PostgreSQL oldest transaction", "fooddiary_postgres_oldest_transaction_seconds", "s"),
        panel("PostgreSQL database size", "fooddiary_postgres_size_bytes", "bytes"),
        panel("PostgreSQL deadlocks / rollbacks per second", ["rate(fooddiary_postgres_deadlocks_total[5m])", "rate(fooddiary_postgres_rollbacks_total[5m])"]),
        panel("Redis memory", ["fooddiary_redis_memory_used_bytes", "fooddiary_redis_memory_max_bytes"], "bytes", description="A max of zero means no configured Redis memory limit."),
        panel("Redis hits / misses per second", ["rate(fooddiary_redis_keyspace_hits_total[5m])", "rate(fooddiary_redis_keyspace_misses_total[5m])"]),
        panel("Redis evictions / blocked clients", ["increase(fooddiary_redis_evicted_keys_total[15m])", "fooddiary_redis_blocked_clients"]),
        panel("RabbitMQ ready / unacknowledged messages", ["fooddiary_rabbitmq_messages_ready", "fooddiary_rabbitmq_messages_unacknowledged"]),
        panel("RabbitMQ consumers / queues without consumers", ["fooddiary_rabbitmq_consumers", "fooddiary_rabbitmq_queues_without_consumers"]),
        panel("Verified backup age", "time()-fooddiary_backup_last_success_timestamp_seconds", "s"),
        panel("Backup sizes", "fooddiary_backup_size_bytes", "bytes"),
        panel("Backup checksum verified", "fooddiary_backup_verified"),
        panel("Job last-success age", "fooddiary_job_last_success_age_seconds", "s"),
        panel("Public probe success (from VPS)", "fooddiary_public_probe_success", description="Independent probes run in GitHub Actions; these are additional measurements from this VPS."),
        panel("TLS certificate remaining lifetime", "fooddiary_public_probe_tls_seconds_remaining", "s"),
        panel("CPU steal / I/O wait", 'sum by(mode) (rate(node_cpu_seconds_total{mode=~"steal|iowait"}[5m])) / scalar(count(node_cpu_seconds_total{mode="idle"}))', "percentunit"),
        panel("Free inode fraction", 'node_filesystem_files_free{mountpoint="/"}/node_filesystem_files{mountpoint="/"}', "percentunit"),
    ]
    write_json(ROOT / "grafana/fooddiary-operations.json", dashboard("fooddiary-operations", "FoodDiary Operational Coverage", infra_panels))

    telegram = '{compose_service="telegram-bot"} |= "Telegram loop telemetry " | regexp "Telegram loop telemetry (?P<loop>intake|discovery)\\\\." | loop!=""'
    def loop_sum(field):
        return f'sum by(loop)(sum_over_time({telegram} | regexp "{field}=(?P<value>[0-9]+)" | unwrap value | __error__="" [5m]))'
    telegram_panels = [
        panel("Bot container running", 'fooddiary_container_running{service="telegram-bot"}'),
        panel("Bot working set", 'fooddiary_container_memory_working_set_bytes{service="telegram-bot"}', "bytes"),
        panel("Bot restart snapshot", 'fooddiary_container_restarts{service="telegram-bot"}'),
        panel("Successful polls reported over 5 minutes", loop_sum("SuccessfulPolls"), datasource=LOKI),
        panel("Failed polls reported over 5 minutes", loop_sum("FailedPolls"), datasource=LOKI),
        panel("Last successful poll", f'max by(loop)(last_over_time({telegram} | regexp "LastSuccessUnixSeconds=(?P<value>[0-9]+)" | unwrap value | __error__="" [5m])) * 1000', "dateTimeAsIso", LOKI),
        panel("Reported mean poll duration", f'avg by(loop)(avg_over_time({telegram} | regexp "AverageDurationMs=(?P<value>[0-9]+)" | unwrap value | __error__="" [5m]))', "ms", LOKI, description="Mean of minute summaries. Intake includes Telegram long polling and persistence; discovery measures the ready-work request, not user operation latency."),
        panel("Operational failures over 10 minutes", 'sum(count_over_time({compose_service="telegram-bot"} |~ "Telegram (intake paused|work discovery failed|operation .* will resume|bot error|API error)" [10m])) or vector(0)', datasource=LOKI),
        {"title": "Recent operational diagnostics", "type": "logs", "datasource": {"type": "loki", "uid": LOKI}, "targets": [{"expr": '{compose_service="telegram-bot"} |~ "Telegram (intake paused|work discovery failed|operation .* (resume|invalid)|API error|bot error|photo progress notice)"', "refId": "A", "maxLines": 100}], "options": {"showTime": True, "wrapLogMessage": True}},
    ]
    write_json(ROOT / "grafana/fooddiary-telegram.json", dashboard("fooddiary-telegram", "FoodDiary Telegram Operations", telegram_panels))

    prefix = '{log_source="frontend"} | regexp "Client telemetry event (?P<category>client_error|http_request|route_timing|web_vital)/"'
    build = ' | regexp "BuildVersion=(?P<build_version>[a-zA-Z0-9.+_-]{1,64})"'
    frontend_panels = [
        panel("Client exceptions by build", 'sum by(build_version) (count_over_time('+prefix+' | category="client_error"'+build+'[$__interval]))', datasource=LOKI),
        panel("API errors from browsers by build", 'sum by(build_version) (count_over_time('+prefix+' | category="http_request" |~ "Outcome=(server_error|network_error)"'+build+'[$__interval]))', datasource=LOKI),
        panel("Navigation duration p95 by build", 'quantile_over_time(0.95, '+prefix+' | category="route_timing"'+build+' | regexp "DurationMs=(?P<duration>[0-9.]+)" | unwrap duration | __error__="" [$__interval]) by(build_version)', "ms", LOKI),
        panel("Web vital p75 by name / build", 'quantile_over_time(0.75, '+prefix+' | category="web_vital" | regexp "web_vital/(?P<vital>[a-z_]+)"'+build+' | regexp "Value=(?P<value>[0-9.]+)" | unwrap value | __error__="" [$__interval]) by(vital,build_version)', "ms", LOKI),
        panel("Frontend telemetry coverage", 'sum by(category) (count_over_time('+prefix+'[$__interval]))', datasource=LOKI),
    ]
    frontend = dashboard("fooddiary-frontend-reliability", "FoodDiary Frontend Reliability", frontend_panels)
    frontend["time"] = {"from": "now-24h", "to": "now"}
    write_json(ROOT / "grafana/fooddiary-frontend-reliability.json", frontend)

    variables = [
        {"name": "model", "type": "query", "datasource": {"type": "prometheus", "uid": PROM}, "query": "label_values(fooddiary_ai_tokens_total, fooddiary_ai_model)", "refresh": 1, "includeAll": True, "allValue": ".*", "multi": True, "current": {"text": "All", "value": "$__all"}},
    ]
    ai_panels = [
        panel("Provider request p95 including retries / body", 'histogram_quantile(0.95,sum by(le,fooddiary_ai_operation,fooddiary_ai_model)(rate(fooddiary_ai_request_duration_milliseconds_bucket[5m])))', "ms"),
        panel("Provider request outcomes", 'sum by(fooddiary_ai_operation,fooddiary_ai_outcome)(increase(fooddiary_ai_requests_total[1h]))'),
        panel("Measured provider tokens", 'sum by(fooddiary_ai_model,fooddiary_ai_token_type)(increase(fooddiary_ai_tokens_total[1h]))'),
        panel("Estimated token cost by model over range", cost_expression(), "currencyUSD", description="Verified 2026-11-07 against https://developers.openai.com/api/docs/pricing. Standard short-context uncached rates are mapped separately for gpt-6-luna and gpt-4o. Unknown models stay absent. Excludes cache discounts/writes, long-context/premium/regional adjustments, audio, token-count calls and missing usage; this is a baseline estimate, not an invoice."),
        panel("Token usage without a configured model price", 'sum by(fooddiary_ai_model)(increase(fooddiary_ai_tokens_total[$__range])) unless on(fooddiary_ai_model) ('+price_vector("input")+')', description="Missing price coverage is shown as usage, never as a zero-cost estimate."),
    ]
    write_json(ROOT / "grafana/fooddiary-ai-operations.json", dashboard("fooddiary-ai-operations", "FoodDiary AI Provider Operations", ai_panels, variables))

    pricing_test = {"rule_files": [], "evaluation_interval": "1m", "tests": [{"name": "model-specific prices preserve units and omit unknown models", "interval": "1m", "input_series": [], "promql_expr_test": [{"expr": cost_expression("5m", ".*"), "eval_time": "5m", "exp_samples": [{"labels": '{fooddiary_ai_model="gpt-6-luna"}', "value": 0.000023}, {"labels": '{fooddiary_ai_model="gpt-4o"}', "value": 0.0004875}]}]}]}
    for model in ["gpt-6-luna", "gpt-4o", "unpriced-fixture-model"]:
        for kind, count in [("input", 11), ("output", 7)]:
            pricing_test["tests"][0]["input_series"].append({"series": f'fooddiary_ai_tokens_total{{fooddiary_ai_model="{model}",fooddiary_ai_token_type="{kind}"}}', "values": f"0+{count}x10"})
    (ROOT / "prometheus/fooddiary-pricing-tests.yml").write_text(yaml.safe_dump(pricing_test, sort_keys=False), encoding="utf-8", newline="\n")


if __name__ == "__main__":
    generate()
