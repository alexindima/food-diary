# Weekly Goals Infrastructure Guidelines

WeeklyGoals repository implementations, the advisory-lock transaction runner, and the complete `AddWeeklyGoalsModule` composition facade live here. Reuse central `FoodDiaryDbContext`; central Infrastructure must never reference this project. Keep external notification delivery outside the transaction. Historical migrations remain central.
