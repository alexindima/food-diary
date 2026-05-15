import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import type { ShoppingList } from '../../shopping-lists/models/shopping-list.data';
import type { MealPlan, MealPlanSummary } from '../models/meal-plan.data';
import { MealPlanService } from './meal-plan.service';

describe('MealPlanService', () => {
    let service: MealPlanService;
    let httpMock: HttpTestingController;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [MealPlanService, provideHttpClient(), provideHttpClientTesting()],
        });

        service = TestBed.inject(MealPlanService);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('loads meal plans with optional diet type filter', () => {
        const plans: MealPlanSummary[] = [createSummary()];

        service.getAll('Keto').subscribe(result => {
            expect(result).toEqual(plans);
        });

        const request = httpMock.expectOne(`${environment.apiUrls.mealPlans}/?dietType=Keto`);
        expect(request.request.method).toBe('GET');
        request.flush(plans);
    });

    it('returns empty list when meal plan loading fails', () => {
        service.getAll().subscribe(result => {
            expect(result).toEqual([]);
        });

        const request = httpMock.expectOne(`${environment.apiUrls.mealPlans}/`);
        request.flush('Server error', { status: 500, statusText: 'Internal Server Error' });
    });

    it('loads meal plan detail by id', () => {
        const plan = createMealPlan();

        service.getById('plan-1').subscribe(result => {
            expect(result).toEqual(plan);
        });

        const request = httpMock.expectOne(`${environment.apiUrls.mealPlans}//plan-1`);
        expect(request.request.method).toBe('GET');
        request.flush(plan);
    });

    it('adopts meal plan', () => {
        const plan = createMealPlan();

        service.adopt('plan-1').subscribe(result => {
            expect(result).toEqual(plan);
        });

        const request = httpMock.expectOne(`${environment.apiUrls.mealPlans}//plan-1/adopt`);
        expect(request.request.method).toBe('POST');
        request.flush(plan);
    });

    it('generates shopping list from meal plan', () => {
        const shoppingList = createShoppingList();

        service.generateShoppingList('plan-1').subscribe(result => {
            expect(result).toEqual(shoppingList);
        });

        const request = httpMock.expectOne(`${environment.apiUrls.mealPlans}//plan-1/shopping-list`);
        expect(request.request.method).toBe('POST');
        request.flush(shoppingList);
    });
});

function createSummary(): MealPlanSummary {
    return {
        id: 'plan-1',
        name: 'Keto plan',
        description: null,
        dietType: 'Keto',
        durationDays: 7,
        targetCaloriesPerDay: 1800,
        isCurated: true,
        totalRecipes: 21,
    };
}

function createMealPlan(): MealPlan {
    return {
        ...createSummary(),
        days: [],
    };
}

function createShoppingList(): ShoppingList {
    return {
        id: 'shopping-list-1',
        name: 'Keto plan',
        items: [],
        createdAt: '2026-05-15T00:00:00Z',
    };
}
