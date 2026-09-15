# Host tests

Follow `Tooling/Testing/AGENTS.md` for shared test rules.

- Own Web API, JobManager and Telegram Bot host tests here, including WebApplicationFactory integration tests and API snapshots.
- Keep module-owned behavior tests under the owning module.
- Production hosts retain their existing root paths during this test-only relocation.
- Preserve assembly identities, test discovery, HTTP snapshots and CI group membership.
- Import `Tooling/Testing/TestProjects.props`; do not duplicate runner configuration.
