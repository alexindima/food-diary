# ADR 0046: Authentication contract ownership and explicit outbox settings

Status: Accepted

## Decision

Retire Errors.Authentication from Application.Contracts. Authentication.Contracts
owns generic InvalidCredentials/InvalidToken through AuthenticationErrors.
Users.Contracts owns account-state and Google/Telegram identity-link failures
through UserAuthenticationErrors. Admin.Contracts owns ImpersonationErrors.
Identity.Contracts retains its provider/protocol failures. Preserve every existing
error code, message and kind; the Authentication wire prefix does not dictate the
assembly owner. Existing UserErrors with different codes remain distinct.

IdentityInputLimits owns Google, Telegram and SSO limits. Password and generic
opaque-token limits remain shared: Dietologist invitation tokens also use the
opaque-token bound. No input limit, attribute, payload or route changes.

Outbox.Infrastructure owns AddOutboxProcessing(configuration), including existing
binding, defaults, validation message and ValidateOnStart. Hosts and full-service
test composition call it explicitly. Persistence-only registration does not bind
or validate outbox configuration. Dispatch, retries and lease behavior are unchanged.

Move Application.Runtime physically under Shared while retaining its assembly
name, namespaces and two existing contract references. Hosts remain its consumers;
modules do not acquire the runtime implementation. Update solution grouping,
project paths, Docker copies and source-discovery tests together.

## Consequences

Users keeps its one-way consumer relationship with Identity. Generic application
contracts no longer export authentication/account policies. Configuration ownership
matches the outbox implementation. No additional production projects, database
migrations, endpoint changes or deployment actions are required.
