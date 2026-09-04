# Admin Application/Abstractions guidelines

Rules for `Modules/Admin/Application/Abstractions/`.

Own Admin billing-report, impersonation, MailInbox and administrative role-audit reader ports/models. Depend on Admin Domain, Users Domain.Contracts and Results. Preserve legacy CLR namespaces. Identity owns email-template contracts; Users owns its role-audit aggregate.
