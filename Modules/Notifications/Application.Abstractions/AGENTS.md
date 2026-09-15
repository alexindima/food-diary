# Notifications Application Abstractions

- Own internal notification persistence/delivery ports and delivery/read models.
- Consumer capabilities, creation requests and shared payloads belong to Notifications.Contracts. Foreign business modules reference Contracts rather than this assembly.
- Use `FoodDiary.Modules.Notifications.Application.Abstractions` with folder suffixes.
- Cross-module writers accept immutable NotificationRequest data, never Notification aggregates. NotificationWriter owns aggregate construction and validation; repository and outbox ports remain owner-internal capabilities.
- Depend on owner Domain, owner Contracts, scalar Users contracts and shared Results. Do not reference central application abstractions or expose this assembly as a consumer facade.
