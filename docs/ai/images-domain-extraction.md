# Images domain extraction

`ImageAssetId` is physically owned by the dependency-free
`Modules/Images/Contracts` project while retaining its
`FoodDiary.Domain.ValueObjects.Ids` CLR namespace. Central Domain references only
that contract. `ImageAsset` is physically owned by `Modules/Images/Domain`, keeps
its legacy CLR namespace, and references central Domain one-way for `User` and
`UserId`.

Central `MealAiSession` retains `ImageAssetId` but no longer exposes an
`ImageAsset` CLR navigation. The Meals persistence model preserves the same
optional `SetNull` foreign key through `HasOne<ImageAsset>()`; Meals projections
batch-resolve asset URLs, `UpdateMeal` reloads its response through the same
projection seam, and Dashboard uses an explicit left join. Routes, payloads,
authorization, query ordering and the database schema remain unchanged.

The shared DbContext, migrations and snapshot, User cleanup, storage providers,
and generic deletion-outbox claim/replay remain with their existing owners.
Images-only domain and repository tests live under `Modules/Images/tests`; mixed
HTTP, cleanup, outbox and migration tests remain central.
