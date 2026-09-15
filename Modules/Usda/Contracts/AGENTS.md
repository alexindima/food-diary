# USDA consumer contracts

SearchUsdaFoodsQuery is the shared combined local/provider search use case. Its handler belongs to Application; Products dispatches it through ISender and maps the result to suggestions. Do not restore IUsdaProductSuggestionReadService or duplicate fallback search in consumers.

IUsdaFoodSearchService remains the external provider port. IUsdaProductLinkService is implemented by Products; IUsdaMealNutritionReadService by Meals. Preserve owner access, errors, cancellation and caller-owned writes. Expose no aggregates, repositories or provider SDK types. Use canonical project/folder namespaces.

Daily micronutrient input includes the product measurement unit. USDA nutrient values are per 100 grams. Without a gram conversion, piece/volume products remain in TotalProductCount but are excluded from LinkedProductCount and nutrient totals; a USDA ID alone does not prove calculable coverage.
