# Notifications Application Abstractions

- Own notification persistence/delivery ports, semantic cross-module capabilities and immutable payload/read models.
- Preserve legacy `FoodDiary.Application.Abstractions.Notifications` namespaces.
- Cross-module writers accept immutable NotificationRequest data, never Notification aggregates. NotificationWriter owns aggregate construction and validation; repository and outbox ports remain owner-internal capabilities.
- Depend only on module Domain and shared Results. Do not reference central Application abstractions: that project exposes this assembly transitively for compatibility.
