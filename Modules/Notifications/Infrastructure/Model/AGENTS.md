# Notifications Persistence Model

- Own notification/subscription/outbox-record mappings and the notification outbox persistence record.
- Keep EF table/column/index/navigation identity unchanged.
- Register mappings through ApplyNotificationsPersistenceModel from central FoodDiaryDbContext.
- Depend only on module Domain, EF and the shared outbox record contract. Keep migrations/snapshot central.
