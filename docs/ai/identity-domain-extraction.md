# Identity domain and persistence ownership

Identity physically owns `EmailTemplate`, `UserRefreshTokenSession`, and `UserLoginEvent` in `Modules/Identity/Domain`, retaining their existing `FoodDiary.Domain.Entities.*` CLR namespaces. The module Domain references central Domain one-way for shared primitives, `LanguageCode`, and the compatibility `UserId` type.

Identity PersistenceModel owns the three existing EF configurations. Central `FoodDiaryDbContext` explicitly applies the module model; central migrations and snapshot remain unchanged because the final EF comparison reports no model delta. Table, index, conversion, foreign-key, cascade-delete, timestamp, and `xmin` semantics are unchanged.

Identity Infrastructure owns the independent `EmailTemplateRepository` and `RefreshTokenSessionRepository`, registered by `AddIdentityPersistence` in Web API, Initializer, and Job Manager composition roots.

`User`, `UserId`, `Role`, `UserRole`, `UserRoleAuditEvent`, credential/security state, the combined `UserRepository`, provider/JWT/SSO/Redis adapters, mail transport, HTTP endpoints, hosts, migrations, and snapshot remain central. `UserLoginEventRepository` also remains central: it joins the central User graph and implements mixed Admin reporting plus shared cleanup contracts, so moving it would create an artificial boundary or dependency cycle.

The Identity Application project remains physically under `Modules/Identity/Application` while preserving assembly and root namespace `FoodDiary.Application.Identity`. `FoodDiary.slnx` now nests Identity projects and tests under their own module folders; a generic architecture test enforces module solution-folder ownership.
