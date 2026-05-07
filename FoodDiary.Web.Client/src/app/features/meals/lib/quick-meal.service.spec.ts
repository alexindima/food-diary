import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { NavigationService } from '../../../services/navigation.service';
import { MeasurementUnit, type Product, ProductType, ProductVisibility } from '../../products/models/product.data';
import { MealService } from '../api/meal.service';
import { type Meal } from '../models/meal.data';
import { QuickMealService } from './quick-meal.service';

describe('QuickMealService', () => {
    let service: QuickMealService;
    let mealService: { create: ReturnType<typeof vi.fn> };
    let navigationService: { navigateToConsumptionAddAsync: ReturnType<typeof vi.fn> };
    let toastService: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn> };

    const product: Product = {
        id: 'product-1',
        name: 'Crab salad',
        productType: ProductType.Other,
        baseUnit: MeasurementUnit.G,
        baseAmount: 100,
        defaultPortionAmount: 180,
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

    beforeEach(() => {
        mealService = {
            create: vi.fn().mockReturnValue(of(createdMeal)),
        };
        navigationService = {
            navigateToConsumptionAddAsync: vi.fn().mockResolvedValue(true),
        };
        toastService = {
            success: vi.fn(),
            error: vi.fn(),
        };

        TestBed.configureTestingModule({
            providers: [
                QuickMealService,
                { provide: MealService, useValue: mealService },
                { provide: NavigationService, useValue: navigationService },
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
                amount: 180,
            }),
        ]);
    });

    it('adds another default portion when the same product is added again', () => {
        service.addProduct(product);
        service.addProduct(product);

        expect(service.items()[0]?.amount).toBe(360);
    });

    it('clears draft after opening editor only when navigation succeeds', async () => {
        service.addProduct(product);

        await service.openEditorAsync();

        expect(navigationService.navigateToConsumptionAddAsync).toHaveBeenCalledWith(undefined, {
            state: {
                quickConsumptionItems: [
                    expect.objectContaining({
                        key: 'product-product-1',
                        amount: 180,
                    }),
                ],
            },
        });
        expect(service.items()).toEqual([]);
    });

    it('keeps draft when editor navigation is cancelled', async () => {
        navigationService.navigateToConsumptionAddAsync.mockResolvedValue(false);
        service.addProduct(product);

        await service.openEditorAsync();

        expect(service.items()).toHaveLength(1);
    });

    it('clears draft and shows success only when save returns created meal', () => {
        service.addProduct(product);

        service.saveDraft();

        expect(mealService.create).toHaveBeenCalledWith(
            expect.objectContaining({
                items: [
                    expect.objectContaining({
                        productId: 'product-1',
                        amount: 180,
                    }),
                ],
                isNutritionAutoCalculated: true,
            }),
        );
        expect(toastService.success).toHaveBeenCalledWith('QUICK_CONSUMPTION.SAVE_SUCCESS');
        expect(toastService.error).not.toHaveBeenCalled();
        expect(service.items()).toEqual([]);
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
