# Images Domain

Own `ImageAsset` under its stable `FoodDiary.Domain.Entities.Assets` CLR
namespace. Its one-way `ImageAsset.User` navigation may reference central
`User`/`UserId`; central Domain must not reference this project.
