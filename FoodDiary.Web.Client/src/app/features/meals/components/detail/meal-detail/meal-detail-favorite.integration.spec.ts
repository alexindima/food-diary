import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { expect, it, onTestFinished, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import type { FavoriteMealHttpResponse } from '../../../../../shared/api/sdk/generated/model/favorite-meal-http-response';
import { createEmptyProductSnapshot, type Meal, MealSourceType } from '../../../../../shared/models/meal.data';
import { utcInstant } from '../../../../../shared/models/semantics/date-value';
import { entityId } from '../../../../../shared/models/semantics/entity-id';
import { productQuantityFromStored } from '../../../../../shared/models/semantics/meal-quantity';
import type { MealDetailActionResult } from '../meal-detail-lib/meal-detail.types';
import { MealDetailComponent } from './meal-detail';

const FAVORITES_URL = 'http://localhost:5300/api/v1/favorite-meals';
const LOOKUP_LIMIT = 100;
const CALORIES = 52;
const SERVED_GRAMS = 100;
const MEAL_ID = entityId<'meal'>('favorite-meal-detail');
const OLD_FAVORITE_ID = entityId<'favorite-meal'>('old-favorite');
const replacement = {
    id: 'replacement-favorite',
    mealId: MEAL_ID,
    createdAtUtc: '2026-11-21T10:15:00Z',
    mealDate: '2026-11-20T09:00:00Z',
    name: null,
    mealType: 'BREAKFAST',
    totalCalories: CALORIES,
    totalProteins: 0,
    totalFats: 0,
    totalCarbs: 0,
    itemCount: 1,
} satisfies FavoriteMealHttpResponse;
const meal: Meal = {
    id: MEAL_ID,
    date: utcInstant('2026-11-20T09:00:00Z'),
    mealType: 'BREAKFAST',
    isFavorite: true,
    favoriteMealId: OLD_FAVORITE_ID,
    totalCalories: CALORIES,
    totalProteins: 0,
    totalFats: 0,
    totalCarbs: 0,
    totalFiber: 0,
    totalAlcohol: 0,
    isNutritionAutoCalculated: true,
    items: [
        {
            id: entityId<'meal-item'>('apple-portion'),
            mealId: MEAL_ID,
            sourceType: MealSourceType.Product,
            amount: productQuantityFromStored(SERVED_GRAMS),
            product: {
                ...createEmptyProductSnapshot(),
                id: entityId<'product'>('apple'),
                name: 'Apple',
                baseAmount: SERVED_GRAMS,
                caloriesPerBase: CALORIES,
            },
            recipe: null,
        },
    ],
    aiSessions: [],
};

it.each([
    { scenario: 'another tab removed and re-added the favorite', currentFavorite: replacement },
    { scenario: 'another tab already removed the favorite', currentFavorite: null },
])('reconciles the diary favorite on the first removal click when $scenario', ({ currentFavorite }) => {
    const { http, favorite, redraw } = renderFavoriteDetail();
    http.expectOne({ method: 'GET', url: `${FAVORITES_URL}/check/${meal.id}` }).flush(true);
    redraw();
    expect(favorite.getAttribute('aria-label')).toBe('MEAL_DETAIL.REMOVE_FAVORITE');

    favorite.click();
    redraw();
    expect(favorite.disabled).toBe(true);
    http.expectOne({ method: 'DELETE', url: `${FAVORITES_URL}/${OLD_FAVORITE_ID}` }).flush(null, {
        status: HttpStatusCode.NotFound,
        statusText: 'Not Found',
    });

    const lookup = http.expectOne(request => request.method === 'GET' && request.url === `${FAVORITES_URL}/page`);
    expect(lookup.request.params.get('page')).toBe('1');
    expect(lookup.request.params.get('limit')).toBe(String(LOOKUP_LIMIT));
    const data = currentFavorite === null ? [] : [currentFavorite];
    lookup.flush({ data, page: 1, limit: LOOKUP_LIMIT, totalItems: data.length, totalPages: 1 });

    if (currentFavorite !== null) {
        http.expectOne({ method: 'DELETE', url: `${FAVORITES_URL}/${currentFavorite.id}` }).flush(null, {
            status: HttpStatusCode.NoContent,
            statusText: 'No Content',
        });
    }
    redraw();
    expect(favorite.disabled).toBe(false);
    expect(favorite.getAttribute('aria-label')).toBe('MEAL_DETAIL.ADD_FAVORITE');
});

function renderFavoriteDetail(): { http: HttpTestingController; favorite: HTMLButtonElement; redraw: () => void } {
    vi.stubGlobal('matchMedia', (media: string) => ({ matches: false, media }));
    TestBed.configureTestingModule({
        imports: [MealDetailComponent],
        providers: [provideRouter([]), provideTranslateTesting(), provideHttpClient(), provideHttpClientTesting()],
    });
    const http = TestBed.inject(HttpTestingController);
    const dialog = TestBed.inject(FdUiDialogService);
    onTestFinished(() => {
        dialog.closeAll();
        vi.unstubAllGlobals();
        http.verify();
    });

    const ref = dialog.open<MealDetailComponent, Meal, MealDetailActionResult>(MealDetailComponent, { data: meal });
    const component = ref.componentRef;
    if (component === null) {
        throw new Error('Expected a rendered meal detail');
    }
    const redraw = (): void => {
        component.changeDetectorRef.detectChanges();
    };
    redraw();
    const host = component.location.nativeElement as HTMLElement;
    const favorite = host.querySelector<HTMLButtonElement>('.meal-detail__favorite-btn button');
    if (favorite === null) {
        throw new Error('Expected the native meal favorite button');
    }
    return { http, favorite, redraw };
}
