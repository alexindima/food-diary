import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

const TEST_PLAN_DURATION_DAYS = 7;

import { environment } from '../../../../environments/environment';
import { utcInstant } from '../../../shared/models/semantics/date-value';
import { entityId } from '../../../shared/models/semantics/entity-id';
import type { ShoppingList } from '../../../shared/models/shopping-list.data';
import type { MealPlan, MealPlanSummary } from '../models/meal-plan.data';
import { planDurationDaysFromStored } from '../models/meal-plan-values';
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
        const page = { data: [createSummary()], page: 1, limit: 50, totalPages: 1, totalItems: 1 };

        service.getPage('Keto').subscribe(result => {
            expect(result).toEqual(page);
        });

        const request = httpMock.expectOne(`${environment.apiUrls.mealPlans}?dietType=Keto&page=1&limit=50`);
        expect(request.request.method).toBe('GET');
        request.flush(page);
    });

    it('preserves the loading error instead of returning an empty catalog', () => {
        service.getPage().subscribe({
            next: () => {
                throw new Error('Expected a loading error');
            },
            error: (error: unknown) => {
                expect(error).toMatchObject({ status: 500 });
            },
        });

        const request = httpMock.expectOne(`${environment.apiUrls.mealPlans}?page=1&limit=50`);
        request.flush('Server error', { status: 500, statusText: 'Internal Server Error' });
    });

    it('loads meal plan detail by id', () => {
        const plan = createMealPlan();

        service.getById(entityId<'meal-plan'>('plan-1')).subscribe(result => {
            expect(result).toEqual(plan);
        });

        const request = httpMock.expectOne(`${environment.apiUrls.mealPlans}/plan-1`);
        expect(request.request.method).toBe('GET');
        request.flush(plan);
    });

    it('adopts meal plan', () => {
        const plan = createMealPlan();

        service.adopt(entityId<'meal-plan'>('plan-1')).subscribe(result => {
            expect(result).toEqual(plan);
        });

        const request = httpMock.expectOne(`${environment.apiUrls.mealPlans}/plan-1/adopt`);
        expect(request.request.method).toBe('POST');
        request.flush(plan);
    });

    it('deletes a personal plan through the owner-scoped endpoint', () => {
        let completed = false;
        service.deletePlan(entityId<'meal-plan'>('plan-1')).subscribe(() => {
            completed = true;
        });
        const request = httpMock.expectOne(`${environment.apiUrls.mealPlans}/plan-1`);
        expect(request.request.method).toBe('DELETE');
        request.flush(null, { status: 204, statusText: 'No Content' });
        expect(completed).toBe(true);
    });

    it('generates shopping list from meal plan', () => {
        const shoppingList = createShoppingList();

        service.generateShoppingList(entityId<'meal-plan'>('plan-1')).subscribe(result => {
            expect(result).toEqual(shoppingList);
        });

        const request = httpMock.expectOne(`${environment.apiUrls.mealPlans}/plan-1/shopping-list`);
        expect(request.request.method).toBe('POST');
        request.flush(shoppingList);
    });
});

function createSummary(): MealPlanSummary {
    return {
        id: entityId<'meal-plan'>('plan-1'),
        name: 'Keto plan',
        description: null,
        dietType: 'Keto',
        durationDays: planDurationDaysFromStored(TEST_PLAN_DURATION_DAYS),
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
        id: entityId<'shopping-list'>('shopping-list-1'),
        name: 'Keto plan',
        items: [],
        createdAt: utcInstant('2026-05-15T00:00:00Z'),
    };
}
