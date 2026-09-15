# Platform tests

Follow `Tooling/Testing/AGENTS.md` for shared test rules.

- Own full EF composition, migrations, cross-module persistence and shared HTTP presentation tests here.
- Production Infrastructure, ReadModel.Composition and Presentation.Api retain their existing root paths during this test-only relocation.
- Keep module-specific tests with their modules and preserve real PostgreSQL integration coverage.
- Preserve assembly identities and CI group membership.
- Import `Tooling/Testing/TestProjects.props`; do not duplicate runner configuration.
