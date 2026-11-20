import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom, type Observable } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import type { ApiErrorHttpResponse } from '../../../shared/api/sdk/generated/model/api-error-http-response';
import type { ShoppingListHttpResponse } from '../../../shared/api/sdk/generated/model/shopping-list-http-response';
import { utcInstant } from '../../../shared/models/semantics/date-value';
import { entityId } from '../../../shared/models/semantics/entity-id';
import type { ShoppingList } from '../../../shared/models/shopping-list.data';
import type { MealPlan, MealPlanSummary } from '../models/meal-plan.data';
import { planDurationDaysFromStored } from '../models/meal-plan-values';
import { MealPlanService } from './meal-plan.service';

const TEST_PLAN_DURATION_DAYS = 7;
const PLAN_ID = entityId<'meal-plan'>('b597e9bd-a038-4743-8889-390ba8917f3e');
const PLAN_URL = `${environment.apiUrls.mealPlans}/${PLAN_ID}`;
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

describe('MealPlanService', () => {
    it.each([undefined, true, false])('loads meal plans with diet and optional source %s', isCurated => {
        const page = { data: [createSummary()], page: 1, limit: 50, totalPages: 1, totalItems: 1 };

        service.getPage('Keto', 1, page.limit, isCurated).subscribe(result => {
            expect(result).toEqual(page);
        });

        const request = httpMock.expectOne(req => req.url === environment.apiUrls.mealPlans && req.params.get('dietType') === 'Keto');
        expect(request.request.params.get('isCurated')).toBe(isCurated === undefined ? null : String(isCurated));
        expect(request.request.params.get('page')).toBe('1');
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

describe('Unavailable meal plan operations', () => {
    it.each([
        { operation: 'read', method: 'GET', suffix: '', run: (): Observable<unknown> => service.getById(PLAN_ID) },
        { operation: 'adopt', method: 'POST', suffix: '/adopt', run: (): Observable<unknown> => service.adopt(PLAN_ID) },
        { operation: 'delete', method: 'DELETE', suffix: '', run: (): Observable<unknown> => service.deletePlan(PLAN_ID) },
        {
            operation: 'generate shopping list',
            method: 'POST',
            suffix: '/shopping-list',
            run: (): Observable<unknown> => service.generateShoppingList(PLAN_ID),
        },
    ])('propagates a removed plan from the $operation operation', async ({ method, suffix, run }) => {
        const pending = firstValueFrom(run());
        const removed = { error: 'MealPlan.NotFound' } satisfies ApiErrorHttpResponse;
        const request = httpMock.expectOne({ method, url: `${PLAN_URL}${suffix}` });
        expect(request.request.body).toBeNull();
        request.flush(removed, { status: HttpStatusCode.NotFound, statusText: 'Not Found' });

        await expect(pending).rejects.toMatchObject({ status: HttpStatusCode.NotFound, error: removed });
    });
});

describe('Generated plan shopping list data', () => {
    it('preserves recipe provenance, amount units and optional text ingredients', async () => {
        const pending = firstValueFrom(service.generateShoppingList(PLAN_ID));
        const list = { ...createShoppingList(), id: entityId<'shopping-list'>('1c153d43-a360-4a63-abb4-32b8bbacab42') };
        const riceSource = {
            id: 'ddce6ec8-18cb-4425-9d94-9506f6ef29a7',
            sourceType: 'MealPlan',
            mealPlanId: PLAN_ID,
            mealPlanMealId: 'fce4db9c-1cd1-4d28-9a32-22cc15f180a1',
            recipeId: '61780774-3c62-4bb7-a264-3966ec154a6f',
            label: 'Rice bowl',
            dayNumber: 2,
            mealType: 'Lunch',
            amount: 150,
            unit: 'G',
        };
        const rice = {
            id: 'bf400bc9-e961-497a-873c-d6be273c0a50',
            shoppingListId: list.id,
            productId: '0cf65268-0295-4e5e-8d3f-964a4dcc11ac',
            name: 'Rice',
            amount: riceSource.amount,
            unit: riceSource.unit,
            isChecked: false,
            checkedOnUtc: null,
            sortOrder: 1,
            sources: [riceSource],
        };
        const seasoning = {
            id: '15290044-da4c-4f2f-9c60-22bca317f72e',
            shoppingListId: list.id,
            productId: null,
            name: 'Seasoning',
            note: 'to taste',
            amount: null,
            unit: null,
            isChecked: false,
            checkedOnUtc: null,
            sortOrder: 2,
            sources: [],
        };
        const payload = { ...list, items: [rice, seasoning] } satisfies ShoppingListHttpResponse;
        const request = httpMock.expectOne({ method: 'POST', url: `${PLAN_URL}/shopping-list` });
        expect(request.request.body).toBeNull();
        request.flush(payload, { status: HttpStatusCode.Created, statusText: 'Created' });

        await expect(pending).resolves.toEqual({
            ...list,
            items: [
                { ...rice, id: entityId<'shopping-list-item'>(rice.id), productId: entityId<'product'>(rice.productId) },
                { ...seasoning, id: entityId<'shopping-list-item'>(seasoning.id) },
            ],
        });
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
        revision: 'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa',
        id: entityId<'shopping-list'>('shopping-list-1'),
        name: 'Keto plan',
        items: [],
        createdAt: utcInstant('2026-05-15T00:00:00Z'),
    };
}
