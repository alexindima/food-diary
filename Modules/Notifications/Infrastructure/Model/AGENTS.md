# Notifications Persistence Model

- Own notification/subscription/outbox-record mappings and the notification outbox persistence record.
- Keep EF table/column/index identity and same-owner navigations unchanged; scalar User keys retain their original typed FK/delete mappings in central NotificationsCrossModuleRelationships.
- Register mappings through ApplyNotificationsPersistenceModel from central FoodDiaryDbContext.
- Depend only on module Domain, Users.Domain.Contracts, EF and the shared outbox record contract. Keep migrations/snapshot central.
