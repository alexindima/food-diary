# Audit persistence model

Own the shared audit record and its EF mapping. The shared `FoodDiaryDbContext`
applies this model explicitly; audit read/write services and module-specific audit
rules stay with their runtime owners. Keep this project free of host and adapter
dependencies.
