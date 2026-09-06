# USDA Application Abstractions

Own provider and persistence ports. Preserve `FoodDiary.Application.Abstractions.Usda.*` namespaces; do not add HTTP or EF implementations.

Own IUsdaMealNutritionReadService, UsdaMealProductNutritionReadModel, IUsdaProductLinkService and IUsdaProductSuggestionReadService. The supplying Meals/Products owners implement these ports; product mutations return original owner Result errors.
