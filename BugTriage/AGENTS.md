# BugTriage service

- Independent operational service; do not reference primary FoodDiary modules or MailInbox server assemblies.
- Application owns report lifecycle, consumer/store ports and orchestration. Infrastructure owns PostgreSQL, MailInbox.Client and the polling worker. Presentation owns HTTP and token authorization. WebApi only composes them.
- MailInbox remains a reusable mail service. Only BugTriage Infrastructure may consume its client.
- Store one report per MailInbox stored ID, independently of the mail read flag. Claims require a random lease token, expire, and fence stale workers on every write.
- Never execute Codex, Git, repository tests, or attachment programs inside the service. Local workers return reviewed outcomes and draft PR/MR links.
- Keep bodies, addresses, MIME, tokens and report summaries out of logs. Original content expires after the configured retention; the import receipt must survive content purge.
- Use separate PostgreSQL database `fooddiary_bugtriage`; initialization is an explicit `--initialize` operation.
- Build: `dotnet build BugTriage/FoodDiary.BugTriage.WebApi`
- Tests: `dotnet test BugTriage/tests/FoodDiary.BugTriage.Tests` (Docker required).
