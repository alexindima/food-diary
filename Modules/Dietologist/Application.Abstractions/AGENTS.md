# Dietologist Application Abstractions

Own Dietologist ports, read models, message payloads and DietologistErrors.
Keep canonical project and folder namespaces and preserve all error codes/messages/kinds. Depend on owner
Domain, Users Domain.Contracts and shared Results; do not reference central
Application.Abstractions, which no longer exports an Errors.Dietologist facade.
Do not add hosts, persistence implementations or provider SDK dependencies.

Dashboard access capability and permissions projection belong to Dietologist Contracts.
Reference that owner directly when reusing its DTO in internal repository results.
