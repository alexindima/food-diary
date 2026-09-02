# Images Domain

Own `ImageAsset` under its stable `FoodDiary.Domain.Entities.Assets` CLR
namespace. Its one-way `ImageAsset.User` navigation references Users Domain for User and Users Domain.Contracts for UserId; central Domain must not reference this project.

User ownership: use Users Domain for aggregate navigations, Users Domain.Contracts for ID-only dependencies, and the exact module or Primitives owner for shared values/guards. Preserve all existing relationships.
