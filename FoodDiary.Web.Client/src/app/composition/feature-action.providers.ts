import { inject, Injector, type Provider, type Type } from '@angular/core';

import {
    DIETOLOGIST_RELATIONSHIP_ACTIONS,
    type DietologistRelationshipActions,
} from '../features/dietologist/contracts/relationship-actions';
import type { DietologistFacade } from '../features/dietologist/lib/dietologist.facade';
import type { GoalsService } from '../features/goals/api/goals.service';
import { CALORIE_GOAL_ACTIONS, type CalorieGoalActions } from '../features/goals/contracts/calorie-goal-actions';
import type { HydrationService } from '../features/hydration/api/hydration.service';
import { HYDRATION_ACTIONS, type HydrationActions } from '../features/hydration/contracts/hydration-actions';
import type { FavoriteMealService } from '../features/meals/api/favorite-meal.service';
import type { MealService } from '../features/meals/api/meal.service';
import { FAVORITE_MEAL_ACTIONS, type FavoriteMealActions, MEAL_ACTIONS, type MealActions } from '../features/meals/contracts/meal-actions';
import { BILLING_ACCOUNT_ACTIONS, type BillingAccountActions } from '../features/premium/contracts/billing-account-actions';
import type { PremiumBillingFacade } from '../features/premium/lib/premium-billing.facade';
import type { RecipeService } from '../features/recipes/api/recipe.service';
import { RECIPE_LOOKUP, type RecipeLookup } from '../features/recipes/contracts/recipe-lookup';
import type { UsdaService } from '../features/usda/api/usda.service';
import { USDA_PRODUCT_LINK, type UsdaProductLink } from '../features/usda/contracts/usda-product-link';
import { lazyFeatureAction } from './lazy-feature-action';

// Bind capabilities to their owning implementation only at the composition root.
export const FEATURE_ACTION_PROVIDERS: Provider[] = [
    {
        provide: RECIPE_LOOKUP,
        useFactory: (): RecipeLookup => {
            const injector = inject(Injector);
            const loadAsync = async (): Promise<Type<RecipeService>> => {
                const { RecipeService } = await import('../features/recipes/api/recipe.service');
                return RecipeService;
            };
            return { getById: id => lazyFeatureAction(injector, loadAsync, service => service.getById(id)) };
        },
    },
    {
        provide: MEAL_ACTIONS,
        useFactory: (): MealActions => {
            const injector = inject(Injector);
            const loadAsync = async (): Promise<Type<MealService>> => {
                const { MealService } = await import('../features/meals/api/meal.service');
                return MealService;
            };
            return {
                repeat: (id, date, mealType) => lazyFeatureAction(injector, loadAsync, service => service.repeat(id, date, mealType)),
                deleteById: id => lazyFeatureAction(injector, loadAsync, service => service.deleteById(id)),
            };
        },
    },
    {
        provide: FAVORITE_MEAL_ACTIONS,
        useFactory: (): FavoriteMealActions => {
            const injector = inject(Injector);
            const loadAsync = async (): Promise<Type<FavoriteMealService>> => {
                const { FavoriteMealService } = await import('../features/meals/api/favorite-meal.service');
                return FavoriteMealService;
            };
            return {
                getLookupPage: () => lazyFeatureAction(injector, loadAsync, service => service.getLookupPage()),
                add: mealId => lazyFeatureAction(injector, loadAsync, service => service.add(mealId)),
                remove: id => lazyFeatureAction(injector, loadAsync, service => service.remove(id)),
            };
        },
    },
    {
        provide: HYDRATION_ACTIONS,
        useFactory: (): HydrationActions => {
            const injector = inject(Injector);
            const loadAsync = async (): Promise<Type<HydrationService>> => {
                const { HydrationService } = await import('../features/hydration/api/hydration.service');
                return HydrationService;
            };
            return {
                addEntry: (amountMl, timestampUtc) =>
                    lazyFeatureAction(injector, loadAsync, service => service.addEntry(amountMl, timestampUtc)),
            };
        },
    },
    {
        provide: CALORIE_GOAL_ACTIONS,
        useFactory: (): CalorieGoalActions => {
            const injector = inject(Injector);
            const loadAsync = async (): Promise<Type<GoalsService>> => {
                const { GoalsService } = await import('../features/goals/api/goals.service');
                return GoalsService;
            };
            return { updateGoals: request => lazyFeatureAction(injector, loadAsync, service => service.updateGoals(request)) };
        },
    },
    {
        provide: USDA_PRODUCT_LINK,
        useFactory: (): UsdaProductLink => {
            const injector = inject(Injector);
            const loadAsync = async (): Promise<Type<UsdaService>> => {
                const { UsdaService } = await import('../features/usda/api/usda.service');
                return UsdaService;
            };
            return {
                getFoodDetail: fdcId => lazyFeatureAction(injector, loadAsync, service => service.getFoodDetail(fdcId)),
                linkProduct: (productId, fdcId) => lazyFeatureAction(injector, loadAsync, service => service.linkProduct(productId, fdcId)),
                unlinkProduct: productId => lazyFeatureAction(injector, loadAsync, service => service.unlinkProduct(productId)),
            };
        },
    },
    {
        provide: DIETOLOGIST_RELATIONSHIP_ACTIONS,
        useFactory: (): DietologistRelationshipActions => {
            const injector = inject(Injector);
            const loadAsync = async (): Promise<Type<DietologistFacade>> => {
                const { DietologistFacade } = await import('../features/dietologist/lib/dietologist.facade');
                return DietologistFacade;
            };
            return {
                getRelationship: () => lazyFeatureAction(injector, loadAsync, service => service.getRelationship()),
                invite: request => lazyFeatureAction(injector, loadAsync, service => service.invite(request)),
                updatePermissions: permissions => lazyFeatureAction(injector, loadAsync, service => service.updatePermissions(permissions)),
                revokeRelationship: () => lazyFeatureAction(injector, loadAsync, service => service.revokeRelationship()),
            };
        },
    },
    {
        provide: BILLING_ACCOUNT_ACTIONS,
        useFactory: (): BillingAccountActions => {
            const injector = inject(Injector);
            const loadAsync = async (): Promise<Type<PremiumBillingFacade>> => {
                const { PremiumBillingFacade } = await import('../features/premium/lib/premium-billing.facade');
                return PremiumBillingFacade;
            };
            return {
                getOverview: () => lazyFeatureAction(injector, loadAsync, service => service.getOverview()),
                createPortalSession: () => lazyFeatureAction(injector, loadAsync, service => service.createPortalSession()),
            };
        },
    },
];
