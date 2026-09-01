# Users persistence model

Own Users aggregate and role/audit EF mappings only. Identity mappings for login
events and refresh-token sessions remain central. Registration is explicit from
the shared `FoodDiaryDbContext`; migrations and snapshot stay central.

