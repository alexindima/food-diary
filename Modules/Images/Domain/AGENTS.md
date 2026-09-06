# Images Domain

Own `ImageAsset` under its stable `FoodDiary.Domain.Entities.Assets` CLR
namespace. Its scalar UserId references Users Domain.Contracts; central Domain must not reference this project.

User ownership: reference Users Domain.Contracts for UserId and shared user values. Keep foreign keys scalar; foreign aggregate CLR navigations are prohibited. PersistenceModel preserves the relational constraints with typed HasOne<T>() mappings.
