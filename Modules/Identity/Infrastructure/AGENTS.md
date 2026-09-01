# Identity infrastructure

Own independent Identity persistence adapters. Keep the combined `UserRepository`, provider adapters, JWT/SSO/Redis, mail transport, shared cleanup, and mixed login-event reporting repository with their established central owners.

Register adapters through `AddIdentityPersistence`. Do not make central Infrastructure reference this adapter assembly; composition roots reference it explicitly.
