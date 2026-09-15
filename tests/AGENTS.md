# Remaining mixed test suites

Follow `Tooling/Testing/AGENTS.md` for shared test rules.

Only FoodDiary.Application.Tests and FoodDiary.Domain.Tests remain here. Extract owner-specific tests into module/shared suites when their ownership is verified; keep cross-module compatibility coverage explicit. Do not add new host, platform or tooling projects here.

Directory.Build.props imports Tooling/Testing/TestProjects.props. The shared settings and runner files must not move back into this legacy folder.
