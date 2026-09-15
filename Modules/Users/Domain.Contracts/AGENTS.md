# Users domain contracts

Own UserCalorieSchedule and UserPreferenceUpdate as reusable value contracts.
Preserve schedule validation, fallback and weekly-total behavior verbatim; profile
mutation rules remain on the User aggregate.

Own RoleNames, UserId, ActivityLevel, LanguageCode, DesiredWeightKg and DesiredWaistCm with their existing CLR namespaces. Reference only shared Domain.Primitives. LanguageCode owns the user's en/ru preference contract: TryParse accepts only exact normalized codes; FromPreferred maps the ru prefix to ru and otherwise defaults to en. Preserve this behavior and all existing ID/enum semantics. Do not add aggregate, application or persistence dependencies.

All module projects and tests use `FoodDiary.Modules.Users.<Project>` identities and namespaces matching physical folders. Projects are siblings, including Application.Abstractions and PersistenceModel. Namespace changes preserve database schema, historical migration metadata, HTTP payloads and runtime behavior.
