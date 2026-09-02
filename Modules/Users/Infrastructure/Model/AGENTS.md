# Users persistence model

Own Users aggregate and role/audit EF mappings only. Identity mappings for login
events and refresh-token sessions belong to Modules/Identity/Infrastructure/Model. Registration is explicit from
the shared `FoodDiaryDbContext`; migrations and snapshot stay central.

