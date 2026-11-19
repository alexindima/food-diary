import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { COLLECTION_PAGE_SIZE } from '../../../shared/api/load-paged-collection';
import { adminId } from '../../../shared/models/semantics/admin-meaning';
import type { CatalogPlanRequest } from '../models/admin-meal-plan.data';
import { AdminMealPlansService } from './admin-meal-plans.service';

const planId = adminId<'meal-plan'>('meal-plan-1');
const recipeId = adminId<'recipe'>('recipe-1');
const planSummary = { id: planId, name: 'Starter plan', dietType: 'Balanced', durationDays: 1, isCurated: false };
const days = [{ dayNumber: 1, meals: [{ mealType: 'Lunch', recipeId, servings: 2 }] }];
const detail = { ...planSummary, days };
const path = '/admin/meal-plans';

describe('AdminMealPlansService', () => {
    let service: AdminMealPlansService;
    let http: HttpTestingController;

    beforeEach(() => {
        TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
        service = TestBed.inject(AdminMealPlansService);
        http = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        http.verify();
    });

    it('loads each catalog page and preserves hidden plans and omitted optional fields', () => {
        const receive = vi.fn();
        service.getAll().subscribe(receive);
        const first = http.expectOne(request => request.url.endsWith(path) && request.params.get('page') === '1');
        expect(first.request.params.get('limit')).toBe(String(COLLECTION_PAGE_SIZE));
        const full = Array.from({ length: COLLECTION_PAGE_SIZE }, (_, index) => ({
            ...planSummary,
            id: `published-${index}`,
            isCurated: true,
        }));
        first.flush(full);
        expect(receive).not.toHaveBeenCalled();

        http.expectOne(request => request.url.endsWith(path) && request.params.get('page') === '2').flush([planSummary]);

        expect(receive).toHaveBeenCalledExactlyOnceWith(
            [...full, planSummary].map(plan => ({ ...plan, description: null, targetCaloriesPerDay: null })),
        );
    });

    it('loads plan details and returns matched recipe search results with serving quantities', () => {
        const receivePlan = vi.fn();
        const receiveRecipes = vi.fn();
        service.get(planId).subscribe(receivePlan);
        service.recipes('soup & salad').subscribe(receiveRecipes);

        http.expectOne(request => request.url.endsWith(`${path}/${planId}`)).flush(detail);
        const recipes = http.expectOne(request => request.url.endsWith(`${path}/recipes`));
        expect(recipes.request.params.get('search')).toBe('soup & salad');
        const recipe = { id: recipeId, name: 'Soup & salad', servings: 2 };
        recipes.flush([recipe]);

        expect(receivePlan).toHaveBeenCalledExactlyOnceWith({ ...detail, description: null, targetCaloriesPerDay: null });
        expect(receiveRecipes).toHaveBeenCalledExactlyOnceWith([recipe]);
    });

    it.each([
        ['POST', null, path],
        ['PUT', planId, `${path}/${planId}`],
    ] as const)('uses %s to save the correct draft and preserves optional response fields', (method, id, url) => {
        const draft: CatalogPlanRequest = {
            name: 'Starter plan',
            description: 'Lunch for two',
            dietType: 'Balanced',
            durationDays: 1,
            targetCaloriesPerDay: 1500,
            isPublished: true,
            days,
        };
        const receive = vi.fn();
        service.save(id, draft).subscribe(receive);
        const request = http.expectOne(candidate => candidate.method === method && candidate.url.endsWith(url));
        expect(request.request.body).toEqual(draft);
        const saved = { ...detail, description: draft.description, targetCaloriesPerDay: draft.targetCaloriesPerDay, isCurated: true };
        request.flush(saved);
        expect(receive).toHaveBeenCalledExactlyOnceWith(saved);
    });
});
