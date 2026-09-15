# Identity consumer contracts

Own public email delivery capabilities, administrative template commands/queries and results,
login-event queries and the narrow impersonation-token issuer request.
Keep email formats, error
semantics and cancellation unchanged. SecurityTokenGenerator retains its exact
algorithm as a shared owner helper. No aggregate, repository, provider or whole
Application dependencies are permitted. Depend on Mediator, Results and scalar UserId. Template mutations retain the caller unit of work; generic transport and token capabilities remain ports.

Admin owns impersonation authorization, target filtering, audit and session flow.
Identity owns JWT construction through IImpersonationTokenIssuer. Never expose
generic token validation or refresh-token generation through this consumer seam.

Use canonical FoodDiary.Modules.Identity project identities and folder namespaces, including tests. Projects are siblings. Preserve historical migration metadata and relational schema.

Errors/IdentityErrors owns Google/Telegram/SSO protocol failures. Preserve their
Authentication-prefixed wire codes, messages and kinds. Shared account errors
consumed by Users remain outside Identity to keep the consumer graph one-way.
