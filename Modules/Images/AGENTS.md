# Images logical module

Images owns the dependency-free `ImageAssetId` contract, the `ImageAsset` domain
entity, application policies, persistence mapping and repository adapters. Preserve
their legacy CLR namespaces. Central Domain references Contracts only; Images Domain
may reference central Domain one-way for `User`/`UserId`.

`MealAiSession` retains only `ImageAssetId`; Meals and Dashboard resolve image URLs
through persistence read joins. Shared DbContext, migrations/snapshot, storage
providers, generic deletion outbox processing and UserCleanup remain central.
