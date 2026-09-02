# Products domain contracts

Own ProductId identity and ProductType classification with stable FoodDiary.Domain.* namespaces. Reference only shared domain primitives. Keep scoring algorithms in Shared/FoodDiary.Nutrition.Domain and Product aggregates in Products Domain. Never reference central Domain, Nutrition Domain or Products Domain from this seam; central Domain must not reference this project.
