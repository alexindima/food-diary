# Notifications Application Abstractions

- Own notification persistence/delivery ports, semantic cross-module capabilities and immutable payload/read models.
- Preserve legacy `FoodDiary.Application.Abstractions.Notifications` namespaces.
- Depend only on module Domain and shared Results. Do not reference central Application abstractions: that project exposes this assembly transitively for compatibility.
