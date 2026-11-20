import { TestBed } from '@angular/core/testing';
import { firstValueFrom, of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { PREMIUM_TRIAL_ACTIONS } from '../components/shared/premium-required-dialog/premium-trial-actions';
import { DIETOLOGIST_RELATIONSHIP_ACTIONS } from '../features/dietologist/contracts/relationship-actions';
import { DietologistFacade } from '../features/dietologist/lib/dietologist.facade';
import { GoalsService } from '../features/goals/api/goals.service';
import { CALORIE_GOAL_ACTIONS } from '../features/goals/contracts/calorie-goal-actions';
import { HydrationService } from '../features/hydration/api/hydration.service';
import { HYDRATION_ACTIONS } from '../features/hydration/contracts/hydration-actions';
import { FavoriteMealService } from '../features/meals/api/favorite-meal.service';
import { MealService } from '../features/meals/api/meal.service';
import { FAVORITE_MEAL_ACTIONS, MEAL_ACTIONS } from '../features/meals/contracts/meal-actions';
import { PremiumBillingService } from '../features/premium/api/premium-billing.service';
import { RecipeService } from '../features/recipes/api/recipe.service';
import { RECIPE_LOOKUP } from '../features/recipes/contracts/recipe-lookup';
import { UsdaService } from '../features/usda/api/usda.service';
import { USDA_PRODUCT_LINK } from '../features/usda/contracts/usda-product-link';
import { utcInstant } from '../shared/models/semantics/date-value';
import { entityId } from '../shared/models/semantics/entity-id';
import { usdaFoodId } from '../shared/models/semantics/usda-food-id';
import { FEATURE_ACTION_PROVIDERS } from './feature-action.providers';

const HYDRATION_AMOUNT = 250;
const hydrationOwner = {
    addEntry: vi.fn(() => of({ id: 'entry', timestampUtc: '2026-10-07T00:00:00Z', amountMl: HYDRATION_AMOUNT })),
    deleteEntry: vi.fn(() => of(undefined)),
};
const relationshipOwner = {
    getRelationship: vi.fn(() => of(null)),
    invite: vi.fn(() => of(undefined)),
    updatePermissions: vi.fn(() => of(undefined)),
    revokeRelationship: vi.fn(() => of(undefined)),
};

describe('Lazy capability composition', () => {
    beforeEach(() => {
        hydrationOwner.addEntry.mockClear();
        relationshipOwner.getRelationship.mockClear();
        relationshipOwner.revokeRelationship.mockClear();
        TestBed.configureTestingModule({
            providers: [
                ...FEATURE_ACTION_PROVIDERS,
                {
                    provide: HydrationService,
                    useValue: hydrationOwner,
                },
                {
                    provide: DietologistFacade,
                    useValue: relationshipOwner,
                },
            ],
        });
    });

    it('resolves an overridden owner and keeps the selected hydration instant', async () => {
        const timestamp = new Date('2026-10-07T00:15:00+05:45');
        const request = TestBed.inject(HYDRATION_ACTIONS).addEntry(HYDRATION_AMOUNT, timestamp);
        expect(hydrationOwner.addEntry).not.toHaveBeenCalled();
        await firstValueFrom(request);
        expect(hydrationOwner.addEntry).toHaveBeenCalledExactlyOnceWith(HYDRATION_AMOUNT, timestamp);
    });

    it('delegates relationship actions to the same owner registered in DI', async () => {
        const actions = TestBed.inject(DIETOLOGIST_RELATIONSHIP_ACTIONS);
        expect(await firstValueFrom(actions.getRelationship())).toBeNull();
        await firstValueFrom(actions.revokeRelationship());
        expect(relationshipOwner.getRelationship).toHaveBeenCalledOnce();
        expect(relationshipOwner.revokeRelationship).toHaveBeenCalledOnce();
    });

    it('sends the chosen sharing permissions after invitation and can later restrict them', async () => {
        const actions = TestBed.inject(DIETOLOGIST_RELATIONSHIP_ACTIONS);
        const permissions = {
            shareProfile: true,
            shareMeals: true,
            shareStatistics: false,
            shareWeight: false,
            shareWaist: false,
            shareGoals: false,
            shareHydration: false,
            shareFasting: false,
        };
        const invitation = { dietologistEmail: 'dietologist@example.test', permissions };
        const restricted = { ...permissions, shareMeals: false };

        await firstValueFrom(actions.invite(invitation));
        await firstValueFrom(actions.updatePermissions(restricted));

        expect(relationshipOwner.invite).toHaveBeenCalledExactlyOnceWith(invitation);
        expect(relationshipOwner.updatePermissions).toHaveBeenCalledExactlyOnceWith(restricted);
    });

    it('deletes the exact hydration entry selected for undo', async () => {
        const entryId = entityId<'hydration-entry'>('hydration-to-undo');
        await firstValueFrom(TestBed.inject(HYDRATION_ACTIONS).deleteEntry(entryId));
        expect(hydrationOwner.deleteEntry).toHaveBeenCalledExactlyOnceWith(entryId);
    });
});

describe('Lazy meal and recipe actions', () => {
    it('preserves the diary instant and meal type for repeating, then deletes the selected entry', async () => {
        const original = entityId<'meal'>('meal-source');
        const created = { id: entityId<'meal'>('meal-copy') };
        const date = utcInstant('2026-10-07T00:15:01.123Z');
        const owner = { repeat: vi.fn(() => of(created)), deleteById: vi.fn(() => of(undefined)) };
        TestBed.configureTestingModule({ providers: [...FEATURE_ACTION_PROVIDERS, { provide: MealService, useValue: owner }] });
        const actions = TestBed.inject(MEAL_ACTIONS);
        const repetition = actions.repeat(original, date, 'Breakfast');
        expect(owner.repeat).not.toHaveBeenCalled();

        expect(await firstValueFrom(repetition)).toBe(created);
        await firstValueFrom(actions.deleteById(created.id));

        expect(owner.repeat).toHaveBeenCalledExactlyOnceWith(original, date, 'Breakfast');
        expect(owner.deleteById).toHaveBeenCalledExactlyOnceWith(created.id);
    });

    it('uses separate favorite-record and meal identifiers across lookup, add and remove', async () => {
        const source = entityId<'meal'>('saved-meal');
        const favorite = { id: entityId<'favorite-meal'>('favorite-record'), mealId: source };
        const owner = {
            getLookupPage: vi.fn(() => of([favorite])),
            add: vi.fn(() => of(favorite)),
            remove: vi.fn(() => of(undefined)),
        };
        TestBed.configureTestingModule({ providers: [...FEATURE_ACTION_PROVIDERS, { provide: FavoriteMealService, useValue: owner }] });
        const actions = TestBed.inject(FAVORITE_MEAL_ACTIONS);

        expect(await firstValueFrom(actions.getLookupPage())).toEqual([favorite]);
        expect(await firstValueFrom(actions.add(source))).toBe(favorite);
        await firstValueFrom(actions.remove(favorite.id));

        expect(owner.getLookupPage).toHaveBeenCalledOnce();
        expect(owner.add).toHaveBeenCalledExactlyOnceWith(source);
        expect(owner.remove).toHaveBeenCalledExactlyOnceWith(favorite.id);
    });

    it('passes legacy recipe identifiers unchanged and retains the missing-ingredient result', async () => {
        const owner = { getById: vi.fn(() => of(null)) };
        TestBed.configureTestingModule({ providers: [...FEATURE_ACTION_PROVIDERS, { provide: RecipeService, useValue: owner }] });
        const recipeId = 'old-cookbook/summer lunch';
        const result = await firstValueFrom(TestBed.inject(RECIPE_LOOKUP).getById(recipeId));

        expect(result).toBeNull();
        expect(owner.getById).toHaveBeenCalledExactlyOnceWith(recipeId);
    });
});

describe('Lazy entitlement and nutrition actions', () => {
    it('reads current trial eligibility and only starts a trial on an explicit request', async () => {
        const created = { status: 'trialing' };
        const owner = { getOverview: vi.fn(() => of({ canStartPremiumTrial: true })), startPremiumTrial: vi.fn(() => of(created)) };
        TestBed.configureTestingModule({ providers: [...FEATURE_ACTION_PROVIDERS, { provide: PremiumBillingService, useValue: owner }] });
        const actions = TestBed.inject(PREMIUM_TRIAL_ACTIONS);

        expect(await firstValueFrom(actions.canStart())).toBe(true);
        expect(owner.startPremiumTrial).not.toHaveBeenCalled();
        expect(await firstValueFrom(actions.start())).toBe(created);
        owner.getOverview.mockReturnValueOnce(of({ canStartPremiumTrial: false }));
        expect(await firstValueFrom(actions.canStart())).toBe(false);
        expect(owner.startPremiumTrial).toHaveBeenCalledOnce();
    });

    it('updates the requested calorie target without adding fields owned by other goals', async () => {
        const request = { dailyCalorieTarget: 2150 };
        const owner = { updateGoals: vi.fn(() => of(null)) };
        TestBed.configureTestingModule({ providers: [...FEATURE_ACTION_PROVIDERS, { provide: GoalsService, useValue: owner }] });

        expect(await firstValueFrom(TestBed.inject(CALORIE_GOAL_ACTIONS).updateGoals(request))).toBeNull();
        expect(owner.updateGoals).toHaveBeenCalledExactlyOnceWith(request);
    });

    it('keeps the foreign USDA food key separate from the product key when linking and unlinking', async () => {
        const productId = entityId<'product'>('catalog-product');
        const externalFoodId = 123456;
        const fdcId = usdaFoodId(externalFoodId);
        const food = { fdcId };
        const owner = {
            getFoodDetail: vi.fn(() => of(food)),
            linkProduct: vi.fn(() => of(undefined)),
            unlinkProduct: vi.fn(() => of(undefined)),
        };
        TestBed.configureTestingModule({ providers: [...FEATURE_ACTION_PROVIDERS, { provide: UsdaService, useValue: owner }] });
        const actions = TestBed.inject(USDA_PRODUCT_LINK);

        expect(await firstValueFrom(actions.getFoodDetail(fdcId))).toBe(food);
        await firstValueFrom(actions.linkProduct(productId, fdcId));
        await firstValueFrom(actions.unlinkProduct(productId));

        expect(owner.getFoodDetail).toHaveBeenCalledExactlyOnceWith(fdcId);
        expect(owner.linkProduct).toHaveBeenCalledExactlyOnceWith(productId, fdcId);
        expect(owner.unlinkProduct).toHaveBeenCalledExactlyOnceWith(productId);
    });
});
