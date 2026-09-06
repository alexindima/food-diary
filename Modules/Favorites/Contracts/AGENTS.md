# Favorites consumer contracts

Expose only the three semantic favorite read services and their projection models.
Preserve legacy namespaces, nullability, defaults and strongly typed IDs. Do not
expose aggregates, repository ports, EF, HTTP or application implementations.

Favorites and Meals Domain.Contracts supply favorite IDs and MealId. Other IDs use their
existing Domain.Contracts owners. Build all repository consumers together after
an assembly relocation; no old precompiled binary compatibility is promised.

Favorites also owns IMealFavoriteReadService and MealFavoriteMealModel consumed by Meals; this avoids a reverse Favorites-to-Meals service contract dependency.

Favorite IDs belong to Favorites Domain.Contracts; MealId belongs to Meals Domain.Contracts. Consumer contracts reference those scalar seams and never either aggregate assembly.
