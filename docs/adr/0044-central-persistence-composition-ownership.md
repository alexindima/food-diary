# 0044: Central persistence composition ownership

Status: Accepted

## Decision

Place FoodDiary.Persistence.Runtime in Shared alongside persistence contracts and
outbox infrastructure. Its assembly name, CLR namespaces, algorithms and migration
ownership stay unchanged. The full FoodDiaryDbContext and migration history remain
in FoodDiary.Infrastructure.

FoodDiary.Infrastructure registers only persistence. Shared authentication
infrastructure owns JWT binding and the in-memory SSO code store; each executable
host composes AddSharedAuthentication explicitly. Identity owns email link option
binding through AddIdentityEmailOptions. Both retain existing keys, validation
messages, startup validation and lifetimes. API Redis registration still replaces
the fallback store. The fallback also preserves a store registered before it.

Structured audit logging joins the existing shared audit runtime. It preserves
fields, level and injected clock; its logger category follows the new CLR namespace.
Identity, AI and Products register their own memory cache prerequisites. Cache
lifetimes, keys and expiration policies are unchanged.

## Scope and verification

This changes assembly and registration ownership, not schema, HTTP contracts,
token generation, SSO consumption rules or transaction behavior. No migration is
required. Backend assemblies must be rebuilt and deployed together.

Tests enforce physical ownership, project dependencies, explicit host registration,
SSO expiry/one-time use/cancellation and host overrides, option validation, runtime
isolation and full-model compatibility. PostgreSQL integration tests remain the
authority for shared transactions, intermediate-save visibility and rollback.
