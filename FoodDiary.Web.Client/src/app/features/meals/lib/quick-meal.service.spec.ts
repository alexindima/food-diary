import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { MeasurementUnit, type Product, ProductType, ProductVisibility } from '../../products/models/product.data';
import { type Recipe, RecipeVisibility } from '../../recipes/models/recipe.data';
import { MealService } from '../api/meal.service';
import type { Meal } from '../models/meal.data';
import { QuickMealService } from './quick-meal.service';

const DEFAULT_PORTION_AMOUNT = 180;
const DOUBLE_DEFAULT_PORTION_AMOUNT = 360;

describe('QuickMealService', () => {
    let service: QuickMealService;
    let mealService: { create: ReturnType<typeof vi.fn> };
    let toastService: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn> };

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
    });

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
        expect(toastService.success).toHaveBeenCalledWith('QUICK_CONSUMPTION.SAVE_SUCCESS');
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

    it('keeps draft and shows error when save returns null', () => {
        mealService.create.mockReturnValue(of(null));
        service.addProduct(product);

        service.saveDraft();

        expect(toastService.error).toHaveBeenCalledWith('QUICK_CONSUMPTION.SAVE_ERROR');
        expect(toastService.success).not.toHaveBeenCalledWith('QUICK_CONSUMPTION.SAVE_SUCCESS');
        expect(service.items()).toHaveLength(1);
    });
});
