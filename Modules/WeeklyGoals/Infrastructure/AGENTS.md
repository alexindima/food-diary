# Weekly Goals Infrastructure Guidelines

WeeklyGoals repositories use the owner WeeklyGoalsDbContext. Its scalar model contains only WeeklyGoal; historical migrations and the User Cascade FK remain central. Central Infrastructure must never reference this project.

AddWeeklyGoalsModule registers the owner context through CreateModuleContext and supplies the repository with a live shared transaction accessor. Synchronize before reads, including when the repository was resolved before the transaction began. Writes are staged and saved by IUnitOfWork.

The advisory-lock transaction runner consumes IModuleTransactionCoordinator from FoodDiary.Persistence.Abstractions. Do not reference central Infrastructure directly or transitively. The shared coordinator owns top-level transaction/retry/reset/save/commit; WeeklyGoals owns the user/week SQL lock. Preserve the user/week lock, clean-entry check, retry/reset semantics, shared atomic save and post-commit notification delivery.
