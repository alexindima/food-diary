# Identity tests

Keep focused Identity application behavior in the nested application test project. The Domain test project owns EmailTemplate, UserRefreshTokenSession and UserLoginEvent invariant tests.
Mixed Admin, Users, host, HTTP, provider, shared persistence, and cross-module tests
remain with their established owners. Do not duplicate moved tests.
