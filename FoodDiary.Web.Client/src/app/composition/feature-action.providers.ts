import type { Provider } from '@angular/core';

import { DIETOLOGIST_RELATIONSHIP_ACTIONS } from '../features/dietologist/contracts/relationship-actions';
import { DietologistFacade } from '../features/dietologist/lib/dietologist.facade';
import { GoalsService } from '../features/goals/api/goals.service';
import { CALORIE_GOAL_ACTIONS } from '../features/goals/contracts/calorie-goal-actions';
import { HydrationService } from '../features/hydration/api/hydration.service';
import { HYDRATION_ACTIONS } from '../features/hydration/contracts/hydration-actions';
import { FavoriteMealService } from '../features/meals/api/favorite-meal.service';
import { MealService } from '../features/meals/api/meal.service';
import { FAVORITE_MEAL_ACTIONS, MEAL_ACTIONS } from '../features/meals/contracts/meal-actions';
import { BILLING_ACCOUNT_ACTIONS } from '../features/premium/contracts/billing-account-actions';
import { PremiumBillingFacade } from '../features/premium/lib/premium-billing.facade';
import { RecipeService } from '../features/recipes/api/recipe.service';
import { RECIPE_LOOKUP } from '../features/recipes/contracts/recipe-lookup';
import { UsdaService } from '../features/usda/api/usda.service';
import { USDA_PRODUCT_LINK } from '../features/usda/contracts/usda-product-link';

// Bind capabilities to their owning implementation only at the composition root.
export const FEATURE_ACTION_PROVIDERS: Provider[] = [
    { provide: RECIPE_LOOKUP, useExisting: RecipeService },
    { provide: MEAL_ACTIONS, useExisting: MealService },
    { provide: FAVORITE_MEAL_ACTIONS, useExisting: FavoriteMealService },
    { provide: HYDRATION_ACTIONS, useExisting: HydrationService },
    { provide: CALORIE_GOAL_ACTIONS, useExisting: GoalsService },
    { provide: USDA_PRODUCT_LINK, useExisting: UsdaService },
    { provide: DIETOLOGIST_RELATIONSHIP_ACTIONS, useExisting: DietologistFacade },
    { provide: BILLING_ACCOUNT_ACTIONS, useExisting: PremiumBillingFacade },
];
