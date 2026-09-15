# Outbox persistence model

Own the shared operator replay-audit record and its EF mapping. Generic claiming,
processing stay in Shared/FoodDiary.Outbox.Infrastructure; replay coordination stays in Shared/FoodDiary.Persistence.Runtime;
stream records and SQL remain with Email or the owning feature module.
