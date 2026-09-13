# USDA Application Abstractions

Own internal repository ports, daily micronutrient orchestration port and errors.
Preserve FoodDiary.Application.Abstractions.Usda namespaces; no HTTP or EF implementations.
Public food search, product suggestions, product-link and meal-nutrition integration
capabilities and their DTOs belong to Modules/Usda/Contracts. Reference that owner
directly; do not expose aggregate-returning repository APIs to foreign modules.
