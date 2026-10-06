import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { TranslateService } from '@ngx-translate/core';
import { of, Subject, throwError } from 'rxjs';
import { beforeEach, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { AuthService } from '../../../../services/auth.service';
import type { PageOf } from '../../../../shared/models/page-of.data';
import { PublicCatalogFavorites } from '../../lib/public-catalog-favorites.facade';
import { publicRecipeFixture } from '../../lib/public-recipe.test-helper';
import { PublicRecipesFacade } from '../../lib/public-recipes.facade';
import type { PublicRecipe } from '../../models/public-recipe.data';
import { PublicRecipeCatalogComponent } from './public-catalog';

const facade = { query: vi.fn<PublicRecipesFacade['query']>(), getCategories: vi.fn().mockReturnValue(of([])) };
const CATALOG_PAGES = 3;
beforeEach(() => {
    facade.query
        .mockReset()
        .mockImplementation(filters => of({ data: [publicRecipeFixture()], page: filters.page, limit: 20, totalPages: 3, totalItems: 41 }));
    TestBed.configureTestingModule({
        providers: [
            provideRouter([{ path: 'explore', component: PublicRecipeCatalogComponent }]),
            provideTranslateTesting(),
            { provide: AuthService, useValue: { isAuthenticated: signal(false) } },
        ],
    });
    TestBed.overrideComponent(PublicRecipeCatalogComponent, {
        set: {
            providers: [
                { provide: PublicRecipesFacade, useValue: facade },
                {
                    provide: PublicCatalogFavorites,
                    useValue: { savedIds: signal(new Set()), busyIds: signal(new Set()), message: signal(null), toggleAsync: vi.fn() },
                },
            ],
        },
    });
});
it('loads bookmarked filters and page without resetting to the first page', async () => {
    const harness = await RouterTestingHarness.create('/explore?page=2&search=Soup&category=Dinner&maxTotalTime=30');
    expect(facade.query).toHaveBeenLastCalledWith({
        page: 2,
        search: 'Soup',
        category: 'Dinner',
        maxTotalTime: 30,
        sortBy: 'newest',
        language: 'en',
    });
    expect(harness.routeNativeElement?.querySelector('a[href^="/explore/recipe?"]')).not.toBeNull();
    await harness.navigateByUrl('/explore?page=3&search=Soup&category=Dinner&maxTotalTime=30');
    expect(facade.query).toHaveBeenLastCalledWith({
        page: 3,
        search: 'Soup',
        category: 'Dinner',
        maxTotalTime: 30,
        sortBy: 'newest',
        language: 'en',
    });
});
it('recovers a stale bookmarked page without claiming a nonempty catalog is empty', async () => {
    facade.query.mockImplementation(filters =>
        of({
            data: filters.page > CATALOG_PAGES ? [] : [publicRecipeFixture()],
            page: filters.page,
            limit: 20,
            totalPages: 3,
            totalItems: 41,
        }),
    );
    const harness = await RouterTestingHarness.create('/explore?page=4&sortBy=name&maxTotalTime=60');
    await harness.fixture.whenStable();
    expect(facade.query).toHaveBeenLastCalledWith(expect.objectContaining({ page: 3, sortBy: 'name', maxTotalTime: 60 }));
    expect(TestBed.inject(Router).url).toContain('page=3');
    expect(harness.routeNativeElement?.querySelector('.catalog-state h2')).toBeNull();
});
it('always leaves a stale page when resetting already-default filters', async () => {
    facade.query.mockImplementation(filters => of({ data: [], page: filters.page, limit: 20, totalPages: 0, totalItems: 0 }));
    const harness = await RouterTestingHarness.create('/explore?page=4');
    harness.routeNativeElement?.querySelector<HTMLButtonElement>('.catalog-state button')?.click();
    await harness.fixture.whenStable();
    expect(TestBed.inject(Router).url).not.toContain('page=4');
});
it('normalizes unsupported pages before the request and stops on a genuine empty result', async () => {
    facade.query.mockImplementation(filters => of({ data: [], page: filters.page, limit: 20, totalPages: 0, totalItems: 0 }));
    const harness = await RouterTestingHarness.create('/explore?page=999999&sortBy=name');
    await harness.fixture.whenStable();
    expect(facade.query.mock.calls.every(([filters]) => filters.page === 1)).toBe(true);
    expect(facade.query.mock.calls.length).toBeLessThanOrEqual(2);
    expect(TestBed.inject(Router).url).not.toContain('page=');
    expect(TestBed.inject(Router).url).toContain('sortBy=name');
    expect(harness.routeNativeElement?.querySelector('.catalog-state')).not.toBeNull();
});
it('does not let a pending stale page or error overwrite a new filter draft', async () => {
    const pending = new Subject<PageOf<PublicRecipe>>();
    facade.query.mockReturnValueOnce(pending);
    const harness = await RouterTestingHarness.create('/explore?page=4');
    const input = harness.routeNativeElement?.querySelector<HTMLInputElement>('input');
    if (input === null || input === undefined) {
        throw new Error('Catalog search input was not rendered');
    }
    input.value = 'new draft';
    input.dispatchEvent(new Event('input', { bubbles: true }));
    pending.next({ data: [], page: 4, limit: 20, totalPages: 3, totalItems: 41 });
    pending.error(new Error('old request'));
    harness.detectChanges();
    expect(TestBed.inject(Router).url).toContain('page=4');
    expect(harness.routeNativeElement?.querySelector('[role="alert"]')).toBeNull();
    expect(input.value).toBe('new draft');
});
it('shows total results without a reset action in the filter bar', async () => {
    const harness = await RouterTestingHarness.create('/explore');
    expect(harness.routeNativeElement?.querySelector('.catalog-summary')?.textContent).toContain('PUBLIC_RECIPES.COUNT_OTHER');
    expect(harness.routeNativeElement?.querySelector('.catalog-filters fd-ui-button')).toBeNull();
    await harness.navigateByUrl('/explore?maxTotalTime=37');
    expect(harness.routeNativeElement?.querySelector('.catalog-filters fd-ui-button')).toBeNull();
    expect(facade.query).toHaveBeenLastCalledWith({
        page: 1,
        search: '',
        category: '',
        maxTotalTime: 37,
        sortBy: 'newest',
        language: 'en',
    });
});
it('filters by explicit language and supports all languages', async () => {
    const harness = await RouterTestingHarness.create('/explore?language=ru&page=2');
    expect(facade.query).toHaveBeenLastCalledWith(expect.objectContaining({ language: 'ru', page: 2 }));
    await harness.navigateByUrl('/explore?language=all');
    expect(facade.query).toHaveBeenLastCalledWith(expect.objectContaining({ language: undefined }));
    await harness.navigateByUrl('/explore?language=invalid');
    expect(facade.query).toHaveBeenLastCalledWith(expect.objectContaining({ language: 'en' }));
});
it('preserves server sorting on bookmarked pages and normalizes unknown orders', async () => {
    const harness = await RouterTestingHarness.create('/explore?page=2&sortBy=fastest');
    expect(facade.query).toHaveBeenLastCalledWith(expect.objectContaining({ page: 2, sortBy: 'fastest' }));
    await harness.navigateByUrl('/explore?page=3&sortBy=name');
    expect(facade.query).toHaveBeenLastCalledWith(expect.objectContaining({ page: 3, sortBy: 'name' }));
    for (const sortBy of ['oldest', 'slowest', 'name_desc']) {
        await harness.navigateByUrl(`/explore?page=2&sortBy=${sortBy}`);
        expect(facade.query).toHaveBeenLastCalledWith(expect.objectContaining({ page: 2, sortBy }));
    }
    await harness.navigateByUrl('/explore?sortBy=unknown');
    expect(facade.query).toHaveBeenLastCalledWith(expect.objectContaining({ page: 1, sortBy: 'newest' }));
});
it('uses the current language plural rule for the total count', async () => {
    const harness = await RouterTestingHarness.create('/explore');
    const translate = TestBed.inject(TranslateService);
    translate.use('ru');
    harness.detectChanges();
    expect(harness.routeNativeElement?.querySelector('.catalog-summary')?.textContent).toContain('PUBLIC_RECIPES.COUNT_ONE');
    translate.use('en');
    harness.detectChanges();
    expect(harness.routeNativeElement?.querySelector('.catalog-summary')?.textContent).toContain('PUBLIC_RECIPES.COUNT_OTHER');
});
it('shows a retryable failure rather than an empty catalog', async () => {
    facade.query.mockReturnValueOnce(throwError(() => new Error('offline')));
    const harness = await RouterTestingHarness.create('/explore');
    expect(harness.routeNativeElement?.textContent).toContain('PUBLIC_RECIPES.ERROR');
    harness.routeNativeElement?.querySelector<HTMLButtonElement>('[role="alert"] button')?.click();
    harness.detectChanges();
    expect(harness.routeNativeElement?.textContent).toContain('Soup');
    expect(facade.query).toHaveBeenCalledTimes(2);
});
