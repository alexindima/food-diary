# Dietologist Application Abstractions

Own Dietologist ports, read models, message payloads and DietologistErrors.
Preserve legacy namespaces and all error codes/messages/kinds. Depend on owner
Domain, Users Domain.Contracts and shared Results; do not reference central
Application.Abstractions, which no longer exports an Errors.Dietologist facade.
Do not add hosts, persistence implementations or provider SDK dependencies.
