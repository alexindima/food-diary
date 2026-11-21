import { type HttpRequest, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting, type TestRequest } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { describe, expect, it, onTestFinished, vi } from 'vitest';

import { environment } from '../../../environments/environment';
import { stubScrollIntoView } from '../../../testing/dom-testing';
import { provideTranslateTesting } from '../../../testing/translate-testing.module';
import { FEATURE_ACTION_PROVIDERS } from '../../composition/feature-action.providers';
import type { MealPlanHttpResponse } from '../../shared/api/sdk/generated/model/meal-plan-http-response';
import mealPlanRoutes from './meal-plan.routes';

const PLAN_ID = 'b597e9bd-a038-4743-8889-390ba8917f3e';
const PLAN_NAME = 'My Mediterranean day';
const DIET_TYPE = 'Mediterranean';
const PAGE_SIZE = 50;
const CATALOGUE_PAGE = 2;
const API_PATH = environment.apiUrls.mealPlans;
const QUERY = { source: 'saved', dietType: DIET_TYPE, page: String(CATALOGUE_PAGE), from: 'dashboard' };
const DETAIL: MealPlanHttpResponse = {
    id: PLAN_ID,
    name: PLAN_NAME,
    dietType: DIET_TYPE,
    durationDays: 1,
    isCurated: false,
    days: [
        {
            id: 'b597e9bd-a038-4743-8889-390ba8917f3f',
            dayNumber: 1,
            meals: [
                {
                    id: 'b597e9bd-a038-4743-8889-390ba8917f40',
                    mealType: 'Dinner',
                    recipeId: 'b597e9bd-a038-4743-8889-390ba8917f41',
                    recipeName: 'Chickpea bowl',
                    servings: 1,
                },
            ],
        },
    ],
};

describe('Meal Plan direct detail requests', () => {
    it('loads only the selected plan and waits until Back to request the filtered catalogue', async () => {
        const http = configureMealPlanRoute();
        const queryString = new URLSearchParams(QUERY).toString();
        const harness = await RouterTestingHarness.create(`/meal-plans/${PLAN_ID}?${queryString}`);
        harness.detectChanges();

        const detail = http.expectOne({ method: 'GET', url: `${API_PATH}/${PLAN_ID}` });
        const earlyCatalogue = http.match(isCatalogueRequest);
        for (const request of earlyCatalogue) {
            flushCatalogue(request, 1);
        }
        detail.flush(DETAIL);
        flushLocaleAssets(http);
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(harness.routeNativeElement?.querySelector('.meal-plan-detail__title')?.textContent).toBe(PLAN_NAME);
        });

        const back = harness.routeNativeElement?.querySelector<HTMLButtonElement>('button[aria-label="MEAL_PLANS.BACK"]');
        if (!(back instanceof HTMLButtonElement)) {
            throw new Error('Expected the rendered Meal Plan Back action');
        }
        back.click();
        const catalogue = await vi.waitFor(() => http.expectOne(isCatalogueRequest));
        expect(catalogue.request.params.get('dietType')).toBe(DIET_TYPE);
        expect(catalogue.request.params.get('page')).toBe(String(CATALOGUE_PAGE));
        expect(catalogue.request.params.get('isCurated')).toBe('false');
        expect(catalogue.request.params.get('limit')).toBe(String(PAGE_SIZE));
        flushCatalogue(catalogue, CATALOGUE_PAGE);
        flushLocaleAssets(http);

        await vi.waitFor(() => {
            harness.detectChanges();
            expect(harness.routeNativeElement?.querySelector('.meal-plan-card__name')?.textContent).toBe(PLAN_NAME);
        });
        const router = TestBed.inject(Router);
        expect(router.url.split('?')[0]).toBe('/meal-plans');
        expect(router.parseUrl(router.url).queryParams).toEqual(QUERY);
        expect(earlyCatalogue.map(request => request.request.urlWithParams)).toEqual([]);
    });
});

describe('Meal Plan catalogue route', () => {
    it('renders the filtered catalogue with its own providers', async () => {
        const http = configureMealPlanRoute([]);
        const queryString = new URLSearchParams(QUERY).toString();
        const harness = await RouterTestingHarness.create(`/meal-plans?${queryString}`);
        harness.detectChanges();

        const catalogue = http.expectOne(isCatalogueRequest);
        expect(catalogue.request.params.get('dietType')).toBe(DIET_TYPE);
        expect(catalogue.request.params.get('page')).toBe(String(CATALOGUE_PAGE));
        expect(catalogue.request.params.get('isCurated')).toBe('false');
        http.expectNone({ method: 'GET', url: `${API_PATH}/${PLAN_ID}` });
        flushCatalogue(catalogue, CATALOGUE_PAGE);
        flushLocaleAssets(http);

        await vi.waitFor(() => {
            harness.detectChanges();
            expect(harness.routeNativeElement?.querySelector('.meal-plan-card__name')?.textContent).toBe(PLAN_NAME);
        });
    });
});

function configureMealPlanRoute(actionProviders: typeof FEATURE_ACTION_PROVIDERS = FEATURE_ACTION_PROVIDERS): HttpTestingController {
    vi.stubGlobal('matchMedia', (media: string) => ({
        matches: false,
        media,
        onchange: null,
        addListener: vi.fn(),
        removeListener: vi.fn(),
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
        dispatchEvent: vi.fn(() => true),
    }));
    stubScrollIntoView();
    TestBed.configureTestingModule({
        providers: [
            provideHttpClient(),
            provideHttpClientTesting(),
            provideTranslateTesting({ lang: 'en', fallbackLang: 'en' }),
            provideRouter([{ path: 'meal-plans', children: mealPlanRoutes }]),
            ...actionProviders,
        ],
    });
    const http = TestBed.inject(HttpTestingController);
    const dialogs = TestBed.inject(FdUiDialogService);
    onTestFinished(() => {
        dialogs.closeAll();
        vi.unstubAllGlobals();
        http.verify();
    });
    return http;
}

function isCatalogueRequest(request: HttpRequest<unknown>): boolean {
    return request.method === 'GET' && request.url === API_PATH;
}

function flushCatalogue(request: TestRequest, page: number): void {
    const totalItems = (page - 1) * PAGE_SIZE + 1;
    request.flush({
        page,
        limit: PAGE_SIZE,
        totalPages: page,
        totalItems,
        data: [{ id: PLAN_ID, name: PLAN_NAME, dietType: DIET_TYPE, durationDays: DETAIL.durationDays, isCurated: false, totalRecipes: 1 }],
    });
}

function flushLocaleAssets(http: HttpTestingController): void {
    for (const asset of ['core', 'app']) {
        for (const request of http.match(query => query.method === 'GET' && query.url.startsWith(`./assets/i18n/en/${asset}.json?`))) {
            request.flush({});
        }
    }
}
