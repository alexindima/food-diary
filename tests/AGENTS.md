# Remaining mixed domain tests

Follow `Tooling/Testing/AGENTS.md` for shared test rules.

Only FoodDiary.Domain.Tests remains here. Extract owner-specific tests into module
or shared suites when ownership is verified. Application.Tests has been retired;
do not recreate it or link helper sources from another test suite.

Directory.Build.props imports Tooling/Testing/TestProjects.props. Shared settings
and runner files must not move back into this legacy folder.
