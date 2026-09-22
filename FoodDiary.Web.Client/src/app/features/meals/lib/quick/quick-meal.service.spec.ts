import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { SessionEventsService } from '../../../../shared/auth/session-events.service';
import { NutritionDataInvalidationService } from '../../../../shared/state/nutrition-data-invalidation.service';
import { MeasurementUnit, type Product, ProductType, ProductVisibility } from '../../../products/models/product.data';
import { type Recipe, RecipeVisibility } from '../../../recipes/models/recipe.data';
import { MealService } from '../../api/meal.service';
import type { Meal } from '../../models/meal.data';
import { QuickMealService } from './quick-meal.service';

const DEFAULT_PORTION_AMOUNT = 180;
const DOUBLE_DEFAULT_PORTION_AMOUNT = 360;

const product: Product = {
    id: 'product-1',
    name: 'Crab salad',
    productType: ProductType.Other,
    baseUnit: MeasurementUnit.G,
    baseAmount: 100,
    defaultPortionAmount: DEFAULT_PORTION_AMOUNT,
    caloriesPerBase: 185,
    proteinsPerBase: 10,
    fatsPerBase: 12,
    carbsPerBase: 8,
    fiberPerBase: 0,
    alcoholPerBase: 0,
    usageCount: 0,
    visibility: ProductVisibility.Private,
    createdAt: new Date('2026-05-03T12:00:00Z'),
    isOwnedByCurrentUser: true,
    qualityScore: 50,
    qualityGrade: 'yellow',
};

const createdMeal: Meal = {
    id: 'meal-1',
    date: '2026-05-03T12:00:00Z',
    mealType: null,
    comment: null,
    imageUrl: null,
    imageAssetId: null,
    totalCalories: 185,
    totalProteins: 10,
    totalFats: 12,
    totalCarbs: 8,
    totalFiber: 0,
    totalAlcohol: 0,
    isNutritionAutoCalculated: true,
    items: [],
    aiSessions: [],
};

const recipe: Recipe = {
    id: 'recipe-1',
    name: 'Rice bowl',
    servings: 4,
    visibility: RecipeVisibility.Private,
    usageCount: 0,
    createdAt: '2026-05-03T12:00:00Z',
    isOwnedByCurrentUser: true,
    isNutritionAutoCalculated: true,
    steps: [],
};

let service: QuickMealService;
let sessionEvents: SessionEventsService;
let mealService: { create: ReturnType<typeof vi.fn> };
let toastService: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn> };

beforeEach(() => {
    mealService = {
        create: vi.fn().mockReturnValue(of(createdMeal)),
    };
    toastService = {
        success: vi.fn(),
        error: vi.fn(),
    };

    TestBed.configureTestingModule({
        providers: [
            QuickMealService,
            { provide: MealService, useValue: mealService },
            { provide: FdUiToastService, useValue: toastService },
            {
                provide: TranslateService,
                useValue: {
                    instant: vi.fn((key: string) => key),
                },
            },
        ],
    });

    service = TestBed.inject(QuickMealService);
    sessionEvents = TestBed.inject(SessionEventsService);
});

describe('QuickMealService draft items', () => {
    it('uses product default portion amount for quick add', () => {
        service.addProduct(product);

        expect(service.items()).toEqual([
            expect.objectContaining({
                key: 'product-product-1',
                amount: DEFAULT_PORTION_AMOUNT,
            }),
        ]);
    });

    it('adds another default portion when the same product is added again', () => {
        service.addProduct(product);
        service.addProduct(product);

        expect(service.items()[0]?.amount).toBe(DOUBLE_DEFAULT_PORTION_AMOUNT);
    });

    it('clears preview items when an authenticated session starts', () => {
        service.setPreviewItems([
            {
                key: 'product-product-1',
                type: 'product',
                product,
                amount: DEFAULT_PORTION_AMOUNT,
            },
        ]);

        sessionEvents.notifyAuthenticated();

        expect(service.items()).toEqual([]);
    });

    it('clears regular draft items when an authenticated session starts', () => {
        service.addProduct(product);

        sessionEvents.notifyAuthenticated();

        expect(service.items()).toEqual([]);
        expect(service.hasItems()).toBe(false);
    });

    it('clears regular draft items when the session ends', () => {
        service.addProduct(product);

        sessionEvents.notifySessionEnded();

        expect(service.items()).toEqual([]);
        expect(service.hasItems()).toBe(false);
    });

    it('updates an existing draft item amount', () => {
        service.addProduct(product);

        service.updateItem('product-product-1', {
            key: 'product-product-1',
            type: 'product',
            product,
            amount: 90,
        });

        expect(service.items()).toEqual([
            expect.objectContaining({
                key: 'product-product-1',
                amount: 90,
            }),
        ]);
        expect(typeof service.items()[0]?.flashId).toBe('number');
    });

    it('merges draft items when an edit changes the source to an existing item', () => {
        const rice: Product = {
            ...product,
            id: 'product-2',
            name: 'Rice',
            defaultPortionAmount: 120,
        };
        service.addProduct(product);
        service.addProduct(rice);

        service.updateItem('product-product-2', {
            key: 'product-product-1',
            type: 'product',
            product,
            amount: 60,
        });

        expect(service.items()).toHaveLength(1);
        expect(service.items()[0]).toEqual(
            expect.objectContaining({
                key: 'product-product-1',
                amount: 240,
            }),
        );
    });
});

describe('QuickMealService saving', () => {
    it('clears draft and shows success only when save returns created meal', () => {
        service.addProduct(product);
        service.updateDetails({
            date: '2026-05-04',
            time: '13:45',
            comment: 'Lunch note',
            preMealSatietyLevel: 2,
            postMealSatietyLevel: 4,
        });

        service.saveDraft();

        expect(mealService.create).toHaveBeenCalledWith(
            expect.objectContaining({
                date: new Date('2026-05-04T13:45'),
                comment: 'Lunch note',
                items: [
                    expect.objectContaining({
                        productId: 'product-1',
                        amount: DEFAULT_PORTION_AMOUNT,
                    }),
                ],
                isNutritionAutoCalculated: true,
                preMealSatietyLevel: 2,
                postMealSatietyLevel: 4,
            }),
        );
        expect(toastService.success).toHaveBeenCalledWith('QUICK_MEAL.SAVE_SUCCESS');
        expect(toastService.error).not.toHaveBeenCalled();
        expect(service.items()).toEqual([]);
    });

    it('saves recipe draft amount as servings', () => {
        service.addRecipe(recipe);

        service.saveDraft();

        expect(mealService.create).toHaveBeenCalledWith(
            expect.objectContaining({
                items: [
                    expect.objectContaining({
                        recipeId: 'recipe-1',
                        productId: null,
                        amount: 1,
                    }),
                ],
            }),
        );
    });

    it('keeps draft and shows error when save fails', () => {
        mealService.create.mockReturnValue(throwError(() => new Error('save failed')));
        service.addProduct(product);

        service.saveDraft();

        expect(toastService.error).toHaveBeenCalledWith('QUICK_MEAL.SAVE_ERROR');
        expect(toastService.success).not.toHaveBeenCalledWith('QUICK_MEAL.SAVE_SUCCESS');
        expect(service.items()).toHaveLength(1);
    });

    it('ignores duplicate save while a meal is being created', () => {
        const pendingCreate$ = new Subject<Meal>();
        mealService.create.mockReturnValue(pendingCreate$);
        service.addProduct(product);

        service.saveDraft();
        service.saveDraft();

        expect(mealService.create).toHaveBeenCalledOnce();
        expect(service.isSaving()).toBe(true);

        pendingCreate$.next(createdMeal);
        pendingCreate$.complete();

        expect(service.isSaving()).toBe(false);
    });
});

describe('QuickMealService asynchronous ownership', () => {
    it('retains only additional portions of the same product after saving the original portion', () => {
        const pending = new Subject<Meal>();
        mealService.create.mockReturnValue(pending);
        service.addProduct(product);
        service.saveDraft();
        service.addProduct(product);
        pending.next(createdMeal);
        pending.complete();
        expect(service.items()).toEqual([expect.objectContaining({ product, amount: DEFAULT_PORTION_AMOUNT })]);
    });

    it('does not clear the next session draft when an old save completes', () => {
        const pending = new Subject<Meal>();
        mealService.create.mockReturnValue(pending);
        service.addProduct(product);
        service.saveDraft();
        sessionEvents.notifySessionEnded();
        service.addRecipe(recipe);
        pending.next(createdMeal);
        pending.complete();
        expect(service.items()).toEqual([expect.objectContaining({ recipe, amount: 1 })]);
        expect(service.isSaving()).toBe(false);
        expect(pending.observed).toBe(false);
    });
    it('retains items added while an existing draft is being saved', () => {
        const pending = new Subject<Meal>();
        mealService.create.mockReturnValue(pending);
        service.addProduct(product);
        service.saveDraft();
        service.addRecipe(recipe);
        pending.next(createdMeal);
        pending.complete();
        expect(service.items()).toEqual([expect.objectContaining({ recipe, amount: 1 })]);
    });
    it('invalidates dependent nutrition after a successful quick save', () => {
        const invalidation = vi.spyOn(TestBed.inject(NutritionDataInvalidationService), 'reportMealMutation');
        service.addProduct(product);
        service.saveDraft();
        expect(invalidation).toHaveBeenCalledTimes(1);
    });
});

describe('QuickMealService draft boundaries', () => {
    it('removes only the requested item and ignores unknown keys', () => {
        service.addProduct(product);
        service.addRecipe(recipe);
        service.removeItem('unknown');
        service.removeItem('product-product-1');
        expect(service.items()).toEqual([expect.objectContaining({ recipe })]);
    });
    it('does not save empty or preview drafts and resets preview on exit', () => {
        service.saveDraft();
        service.setPreviewItems([{ key: 'preview', type: 'product', product, amount: 1 }]);
        service.saveDraft();
        expect(mealService.create).not.toHaveBeenCalled();
        service.exitPreview();
        expect(service.hasItems()).toBe(false);
        service.exitPreview();
        service.addRecipe(recipe);
        service.saveDraft();
        expect(mealService.create).toHaveBeenCalledTimes(1);
    });
    it.each([0, -1, Number.NaN, Number.POSITIVE_INFINITY])('falls back from invalid preferred amount %s', amount => {
        service.addProduct(product, amount);
        expect(service.items()[0].amount).toBe(DEFAULT_PORTION_AMOUNT);
    });
    it('ignores sources with empty IDs', () => {
        service.addProduct({ ...product, id: '' });
        service.addRecipe({ ...recipe, id: '' });
        expect(service.hasItems()).toBe(false);
    });
});

describe('QuickMealService pending draft replacements', () => {
    it('retains a removed and re-added product as a new draft item', () => {
        const pending = new Subject<Meal>();
        mealService.create.mockReturnValue(pending);
        service.addProduct(product);
        service.saveDraft();
        service.removeItem('product-product-1');
        service.addProduct(product);
        pending.next(createdMeal);
        expect(service.items()).toEqual([expect.objectContaining({ product, amount: DEFAULT_PORTION_AMOUNT })]);
    });
    it('preserves both original and added portions if save fails', () => {
        const pending = new Subject<Meal>();
        mealService.create.mockReturnValue(pending);
        service.addProduct(product);
        service.saveDraft();
        service.addProduct(product);
        pending.error(new Error('offline'));
        expect(service.items()[0].amount).toBe(DEFAULT_PORTION_AMOUNT + DEFAULT_PORTION_AMOUNT);
        expect(service.isSaving()).toBe(false);
    });
});
