# Narrow consumer contracts

Users.Contracts and Lessons.Contracts no longer reference aggregate-bearing Domain
assemblies. UserCalorieSchedule and UserPreferenceUpdate belong to the existing
Users.Domain.Contracts. NutritionLessonId, LessonCategory and LessonDifficulty
belong to Lessons.Domain.Contracts. Existing CLR namespaces, validation, enum
values and ID conversions are preserved.

Notifications.Contracts owns the public writer request, writer, deduplication and
client-refresh capabilities plus shared notification type and payload helpers.
Repository and delivery ports remain in Notifications.Application.Abstractions.
Foreign business modules use the narrow consumer project. NotificationWriter still
constructs and validates the aggregate; callers retain SaveChanges and transaction
ownership. Payload serialization and push behavior are unchanged.

These are in-process assembly changes requiring a coordinated rebuild. They do not
change HTTP contracts, persistence models, migrations or deployment topology.
The common DbContext and other broad application abstractions remain separate work.
