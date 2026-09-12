# Isolated Telegram test stand

Status: local services are running; live Telegram acceptance remains outstanding.

Local checkpoint, 2026-09-12: rebuilt from empty Docker storage after the local storage reset. All seven application images built successfully. PostgreSQL, Redis, MinIO and Mailpit are healthy; storage initialization and both database migration runs completed successfully. API, frontend, MailRelay, JobManager and the test bot receiver are running. Local frontend, public HTTPS frontend, HTTPS readiness and Telegram configuration return HTTP 200. Login, registration and OIDC are enabled. The bot authenticated as `fooddiary_telegram_test_bot`; startup logs contain no bot or JobManager error entries. The API image includes timezone data and `Asia/Tbilisi`. The timezone selector translations are served correctly. Earlier test accounts, sessions and uploaded files were lost with the old Docker volumes; live registration and photo acceptance must be repeated against this fresh database.

Test bot confirmed by the user: `@fooddiary_telegram_test_bot`. Its token was supplied through a private local file and verified with Telegram `getMe`; API and bot configuration now use that identity, with login/registration/operation flags enabled locally. Browser OIDC credentials are configured in the private runtime file. The test frontend image uses this username. No token is stored in the repository.

Use `docker-compose.telegram-test.yml` **alone**. It does not extend the main Compose file, load its `.env`, use production image tags, or publish database/cache ports. The project name is `fooddiary-telegram-test`. Database, cache and API Data Protection keys have separate project-scoped volumes. API port is loopback `15300`; the frontend/reverse proxy listens on loopback `15420`.

The user selected local hosting through a separate Cloudflare Tunnel and approved `telegram-test.fooddiary.club`. On 2026-09-12, tunnel `fooddiary-telegram-test` (`2774eac7-c90f-4ff9-b01d-ba743802f2c6`) and its proxied CNAME were created. The local ingress configuration was validated for `http://127.0.0.1:15420`, with a final 404 catch-all. Automatic approval review rejected the attempted background connector launch with `blocked by policy`; the user was given the foreground command below. The user started the connector; public HTTPS application readiness was reverified successfully after rebuilding the stand. The existing `fooddiary-local` tunnel was not changed.

Tunnel credentials and local configuration live outside the repository in the user's `.cloudflared` directory. After the local application responds, run the dedicated connector:

```powershell
cloudflared tunnel --config "$env:USERPROFILE/.cloudflared/fooddiary-telegram-test.yml" run fooddiary-telegram-test
```

Verify the HTTPS application and callback after startup. Do not publish through the tunnel until the local test configuration has been checked for production provider settings. Stop only this connector process when pausing the stand.

## Configuration and launch

Create an isolated runtime configuration file outside the checkout. In a separate interpolation file, set `FD_TEST_ENV_FILE` to the runtime file's absolute path and set new `FD_TEST_DB_PASSWORD`, `FD_TEST_S3_USER`, `FD_TEST_S3_PASSWORD`, `FD_TEST_MAIL_KEY` and `FD_TEST_MAIL_DB_PASSWORD` values. Set the shell variable `FD_TEST_COMPOSE_ENV_FILE` to the interpolation file's absolute path. Do not reuse production values. Supply the API's validated JWT and MailInbox placeholder credentials in the runtime file. MailInbox remains unavailable on container loopback. MailRelay uses its own test PostgreSQL database and the supported PostgresPolling broker, with SMTP routed only to Mailpit. Mailpit's capture UI is bound to loopback port 15425 and is not exposed through the tunnel. No SMTP forwarding/relay is configured in Mailpit.

The custom `TelegramTest` environment avoids Development user secrets. The standalone Compose file overrides connection strings and disables Google and selected outbound notification/payment jobs. Do not put provider settings in the runtime file until their test endpoints and account scope have been verified. Empty S3/OpenAI configuration cannot prove the photo journey.

From the repository root, using the same prepared environment for each command:

```powershell
docker compose --env-file "$env:FD_TEST_COMPOSE_ENV_FILE" -f docker-compose.telegram-test.yml config --quiet
docker compose --env-file "$env:FD_TEST_COMPOSE_ENV_FILE" -f docker-compose.telegram-test.yml --profile app --profile telegram build
docker compose --env-file "$env:FD_TEST_COMPOSE_ENV_FILE" -f docker-compose.telegram-test.yml up -d --wait postgres redis minio mail-postgres mailpit
docker compose --env-file "$env:FD_TEST_COMPOSE_ENV_FILE" -f docker-compose.telegram-test.yml run --rm storage-init
docker compose --env-file "$env:FD_TEST_COMPOSE_ENV_FILE" -f docker-compose.telegram-test.yml --profile app run --rm mail-db-init
docker compose --env-file "$env:FD_TEST_COMPOSE_ENV_FILE" -f docker-compose.telegram-test.yml --profile app up -d --no-deps mail-relay
docker compose --env-file "$env:FD_TEST_COMPOSE_ENV_FILE" -f docker-compose.telegram-test.yml --profile app run --rm db-init
docker compose --env-file "$env:FD_TEST_COMPOSE_ENV_FILE" -f docker-compose.telegram-test.yml --profile app up -d --no-deps api client
docker compose --env-file "$env:FD_TEST_COMPOSE_ENV_FILE" -f docker-compose.telegram-test.yml --profile app ps
```

The last startup command deliberately uses `--no-deps` only after both initializers have succeeded. Start the dedicated tunnel after the frontend responds on `http://127.0.0.1:15420`. Then verify `https://telegram-test.fooddiary.club/health/ready`. The API's S3 health check uses that HTTPS origin, so waiting for API health before starting the frontend/tunnel would deadlock bootstrap. JobManager may be started independently for the isolated Mailpit delivery check (verified locally on 2026-09-12). Start the Telegram receiver only after HTTPS readiness is verified:

```powershell
docker compose --env-file "$env:FD_TEST_COMPOSE_ENV_FILE" -f docker-compose.telegram-test.yml --profile app --profile telegram up -d --no-deps job-manager telegram-bot
```

Use `config --quiet`, not resolved configuration output, once real credentials are present. Always keep the explicit `--env-file`: Compose's default project `.env` can affect interpolation even without service `env_file`. Do not copy the root file. This interpolation file and the runtime file have different purposes. The shell's `FD_TEST_*` values take precedence over the interpolation file; clear stale values before changing environments.

Stop without deleting data:

```powershell
docker compose --env-file "$env:FD_TEST_COMPOSE_ENV_FILE" -f docker-compose.telegram-test.yml --profile app --profile telegram stop
```

No automatic volume deletion or production deployment is part of these commands. Before starting the bot, both `app` and `telegram` profiles must be selected and the separate BotFather identity configured.

MinIO server and client images use the official `quay.io/minio` registry with their existing pinned digests; the former Docker Hub server reference failed to pull during the clean rebuild.

## Outstanding before live acceptance

- Verify the configured MinIO storage end to end through HTTPS. Staging is private; the final bucket policy permits only anonymous GetObject, with no public list/write. Both bucket routes use the same origin as the application, preserving S3 signature host/URI. Check actual presigned uploads from browser and bot; successful bucket initialization alone is insufficient.
- Verify Telegram-specific backup-email journeys in the real browser. The isolated email-account journey passed through the running API, database, JobManager, MailRelay and Mailpit: registration email captured; email confirmation 204; password recovery request 204; recovery email captured with the approved HTTPS origin; password reset 200; new-password login 200; consumed reset-token replay 401. Synthetic credentials remain outside the repository. This does not prove live Telegram authentication.
- Build and verify the supplied test frontend image with `FD_TEST_BOT_USERNAME`. Its image-only configuration keeps same-origin API paths and removes the production Google client and admin URL. The existing tunnel environment must not be used unchanged.
- Configure a single HTTPS application origin and reverse proxy for frontend/API, without exposing databases or admin services. Register the exact Mini App and OIDC callback URLs for the test bot.
- Complete HTTPS readiness and protected Telegram-operation persistence checks. Initializer migrations passed. The focused PostgreSQL migration safety suite also passed 4/4 without skips, including upgrade from InitialCreate and legacy fasting data conversion. After a real restart of the isolated API container, the existing synthetic email-account access token still fetched user info (200), and its refresh token renewed the session (200). This verifies that account/session persistence scenario; protected Telegram operation recovery remains a separate check.
- Run the actual Telegram/browser journeys in the implementation plan and record the build identity and evidence. Compose parsing and mocked HTTP tests are separate evidence.

See [release runbook](TELEGRAM_CLIENT_RUNBOOK.md) and [implementation plan](../plans/TELEGRAM_CLIENT_IMPLEMENTATION_PLAN.md).
