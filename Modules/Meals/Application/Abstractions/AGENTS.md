# Meals Application Abstractions

Owner-only Meal aggregate repository ports and errors. Depend on Meals Domain
and Meals Contracts. Never expose aggregate repositories to foreign production modules.

MealErrors uses shared Results directly. Keep codes/messages/kinds unchanged;
central Errors.Meal delegates here without a reverse central dependency.
