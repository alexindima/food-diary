# Dietologist consumer contracts

Own IDietologistDashboardAccessService and DietologistPermissionsReadModel consumed by Dashboard. Depend only on Results and Users scalar Domain.Contracts. Preserve relationship authorization, eight permission flags, result errors and cancellation; never expose repositories, aggregates or grant permission through a DTO alone.
