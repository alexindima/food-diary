# USDA Domain

Own USDA reference-data entities and HealthAreaScore, HealthAreaGrade and HealthAreaScores. Preserve legacy CLR namespaces and the exact nutrient-ID-based calculation, sodium penalty, limits and grade rules. Reference shared Domain.Primitives for generic guards; never add a reverse dependency on Products or Users. Products Domain may reference this owner for USDA navigation and nutritional calculations.
