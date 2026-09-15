# Lessons scalar domain contracts

Own NutritionLessonId, LessonCategory and LessonDifficulty with canonical folder namespaces and unchanged type
names, values and conversions. Depend only on shared Domain.Primitives. Do not
expose aggregates, repositories, application services or persistence dependencies.

Use canonical FoodDiary.Modules.Lessons project identities and folder namespaces, including tests. Projects are siblings. Preserve historical migration metadata and relational schema.
