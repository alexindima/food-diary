import { Location } from '@angular/common';
import { provideLocationMocks } from '@angular/common/testing';
import type { DebugElement } from '@angular/core';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { FdUiDialogService } from 'fd-ui-kit';
import { of, Subject } from 'rxjs';
import { beforeEach, expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../../../testing/async-testing';
import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { APP_SEARCH_DEBOUNCE_MS } from '../../../../config/runtime-ui.tokens';
import { NavigationService } from '../../../../services/navigation.service';
import type { RecipeOverview } from '../../../../shared/models/recipe.data';
import { API_MAX_PAGE_NUMBER } from '../../../../shared/navigation/pagination-query.utils';
import { ViewportService } from '../../../../shared/platform/viewport.service';
import { QuickMealService } from '../../../meals/contracts/quick-meal';
import { FavoriteRecipeService } from '../../api/favorite-recipe.service';
import { RecipeService } from '../../api/recipe.service';
import { RecipeListRouteStateFacade } from '../../lib/list/recipe-list-route-state.facade';
import { RecipeListComponent } from './recipe-list';

const queryOverview = vi.fn<RecipeService['queryOverview']>();
const STALE_PAGE = 3;
const PAGE_LIMIT = 10;
const SETTLE_DELAY_MS = 10;
beforeEach(() => {
    queryOverview.mockReset().mockImplementation(query => of(overview(query.page)));
    TestBed.configureTestingModule({
        providers: [
            provideRouter([{ path: 'recipes', component: RecipeListComponent }]),
            provideLocationMocks(),
            provideTranslateTesting(),
            { provide: APP_SEARCH_DEBOUNCE_MS, useValue: 0 },
            { provide: RecipeService, useValue: { queryOverview } },
            { provide: FavoriteRecipeService, useValue: {} },
            { provide: QuickMealService, useValue: {} },
            { provide: NavigationService, useValue: {} },
            { provide: FdUiDialogService, useValue: {} },
            { provide: ViewportService, useValue: { isMobile: signal(false) } },
        ],
    });
    TestBed.overrideComponent(RecipeListComponent, { set: { template: '<div #container></div>' } });
});

it('restores the full bookmarked query and keeps its page when signal form effects settle', async () => {
    const harness = await RouterTestingHarness.create(
        '/recipes?page=2&search=Rice&onlyMine=false&category=soups&maxTotalTime=60&caloriesFrom=100&caloriesTo=1000&hasImage=false',
    );
    await settleAsync(harness);
    expect(queryOverview).toHaveBeenCalledTimes(1);
    expect(queryOverview).toHaveBeenCalledWith(
        expect.objectContaining({
            page: 2,
            includePublic: true,
            filters: { search: 'Rice', category: 'soups', maxTotalTime: 60, caloriesFrom: 100, caloriesTo: 1000, hasImage: false },
        }),
    );
});

it('keeps URL, form and query aligned across filter changes and real browser Back', async () => {
    const harness = await RouterTestingHarness.create('/recipes?page=2&search=Rice&onlyMine=false&category=soups');
    const component = activePage(harness).componentInstance as RecipeListComponent;
    const state = activePage(harness).injector.get(RecipeListRouteStateFacade);
    TestBed.inject(Router).setUpLocationChangeListener();
    component['searchForm'].search().value.set('missing');
    await settleAsync(harness);
    expect(TestBed.inject(Router).url).toContain('search=missing');
    expect(TestBed.inject(Router).url).not.toContain('page=2');
    expect(queryOverview).toHaveBeenLastCalledWith(
        expect.objectContaining({
            page: 1,
            includePublic: true,
        }),
    );
    expect(queryOverview.mock.lastCall?.[0].filters).toEqual(expect.objectContaining({ search: 'missing', category: 'soups' }));
    TestBed.inject(Location).back();
    await settleAsync(harness);
    expect(state.current().search).toBe('Rice');
    expect(state.current().page).toBe(2);
    expect(queryOverview).toHaveBeenLastCalledWith(expect.objectContaining({ page: 2 }));
    expect(queryOverview.mock.lastCall?.[0].filters).toEqual(expect.objectContaining({ search: 'Rice', category: 'soups' }));
});

it('replaces stale and unsupported bookmarks while preserving the query', async () => {
    const harness = await RouterTestingHarness.create('/recipes?page=3&onlyMine=false&hasImage=false');
    await settleAsync(harness);
    expect(TestBed.inject(Router).url).toContain('page=2');
    expect(queryOverview).toHaveBeenLastCalledWith(expect.objectContaining({ page: 2, includePublic: true }));
    expect(queryOverview.mock.lastCall?.[0].filters).toEqual(expect.objectContaining({ hasImage: false }));
    await harness.navigateByUrl('/recipes?page=999999&search=Rice');
    await settleAsync(harness);
    expect(TestBed.inject(Router).url).not.toContain('page=');
    expect(queryOverview.mock.calls.every(([query]) => query.page <= API_MAX_PAGE_NUMBER)).toBe(true);
});

it('does not let a stale result recover the page or overwrite a newer draft search', async () => {
    const pending = new Subject<RecipeOverview>();
    queryOverview.mockReturnValueOnce(pending);
    const harness = await RouterTestingHarness.create('/recipes?page=3');
    const component = activePage(harness).componentInstance as RecipeListComponent;
    component['searchForm'].search().value.set('new draft');
    pending.next(overview(STALE_PAGE));
    expect(TestBed.inject(Router).url).toContain('page=3');
    expect(component['recipeData'].items()).toEqual([]);
    await settleAsync(harness);
    expect(TestBed.inject(Router).url).toContain('search=new%20draft');
    expect(queryOverview).toHaveBeenLastCalledWith(expect.objectContaining({ page: 1 }));
    expect(queryOverview.mock.lastCall?.[0].filters).toEqual(expect.objectContaining({ search: 'new draft' }));
});

function overview(page: number): RecipeOverview {
    return {
        recentItems: [],
        favoriteItems: [],
        favoriteTotalCount: 4,
        allRecipes: { page, limit: PAGE_LIMIT, totalPages: 2, totalItems: 14, data: [] },
    };
}

async function settleAsync(harness: RouterTestingHarness): Promise<void> {
    harness.detectChanges();
    await harness.fixture.whenStable();
    await waitForAsyncTasksAsync();
    await new Promise(resolve => setTimeout(resolve, SETTLE_DELAY_MS));
    await harness.fixture.whenStable();
    harness.detectChanges();
}

function activePage(harness: RouterTestingHarness): DebugElement {
    const element = harness.routeDebugElement;
    if (element === null) {
        throw new Error('Recipe page was not activated');
    }
    return element;
}
