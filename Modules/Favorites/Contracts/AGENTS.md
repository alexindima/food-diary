# Favorites consumer contracts

Expose only the three semantic favorite read services and their projection models.
Preserve legacy namespaces, nullability, defaults and strongly typed IDs. Do not
expose aggregates, repository ports, EF, HTTP or application implementations.

Favorites Domain and Meals Domain references currently provide FavoriteMealId and
MealId. They do not grant consumers aggregate mutation rights; removing those
assembly dependencies requires a separate ID-boundary change. Other IDs use their
existing Domain.Contracts owners. Build all repository consumers together after
an assembly relocation; no old precompiled binary compatibility is promised.

Favorites also owns IMealFavoriteReadService and MealFavoriteMealModel consumed by Meals; this avoids a reverse Favorites-to-Meals service contract dependency.
