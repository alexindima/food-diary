# Admin Application.Abstractions guidelines

Rules for `Modules/Admin/Application.Abstractions/`.

Own Admin billing-report, impersonation, MailInbox and administrative role-audit reader ports/models. Depend on Admin Domain, Users Domain.Contracts and Results. Use the project filename as the namespace root, followed by the relative folder path; do not override RootNamespace. Keep Common and Models directly under this project; do not reintroduce an Admin wrapper folder. IDE0130 enforces namespace alignment in this scope. Identity owns email-template contracts; Users owns its role-audit aggregate.

Admin billing and role audit DTO reads use IAdminBillingQuery and IAdminUserRoleAuditQuery. Query handlers consume these ports and IAdminImpersonationSessionQuery directly; combined repository compatibility aliases are not query-handler dependencies.
