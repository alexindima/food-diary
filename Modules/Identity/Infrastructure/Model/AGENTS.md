# Identity persistence model

Own EF configurations for Identity-owned domain entities. Preserve table, column, index, foreign-key, conversion, concurrency, and delete semantics. Central `FoodDiaryDbContext`, migrations, and model snapshot remain central and call `ApplyIdentityPersistenceModel` explicitly.

Do not add a migration when `has-pending-model-changes` reports no delta.

Also own the technical `ConsumedTelegramAssertion` record and its configuration.
This is replay persistence state, not a new domain aggregate. Preserve its CLR
namespace, fingerprint key/length, expiry column/index and table identity; do not
reference the central context or Identity adapter project from this model project.

Use Users.Domain.Contracts for scalar UserId. IdentityCrossModuleRelationships in
central Infrastructure composes foreign User relationships after owned models.
UserLoginEvent and UserRefreshTokenSession retain UserId Cascade deletion. Do not restore Users.Domain to PersistenceModel.
