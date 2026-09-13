# Identity consumer contracts

Own public email delivery/template capabilities, administrative template results,
login-event read capabilities and the narrow impersonation-token issuer request.
Keep legacy FoodDiary.Application.Abstractions namespaces, email formats, error
semantics and cancellation unchanged. SecurityTokenGenerator retains its exact
algorithm as a shared owner helper. No aggregate, repository, provider or whole
Application dependencies are permitted. Depend only on Results and scalar UserId.

Admin owns impersonation authorization, target filtering, audit and session flow.
Identity owns JWT construction through IImpersonationTokenIssuer. Never expose
generic token validation or refresh-token generation through this consumer seam.
