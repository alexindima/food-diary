import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { TranslateService } from '@ngx-translate/core';
import { of, throwError } from 'rxjs';
import { beforeEach, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { AuthService } from '../../../../services/auth.service';
import { PublicCatalogFavorites } from '../../lib/public-catalog-favorites.facade';
import { publicRecipeFixture } from '../../lib/public-recipe.test-helper';
import { PublicRecipesFacade } from '../../lib/public-recipes.facade';
import { PublicRecipeCatalogComponent } from './public-catalog';

const facade = { query: vi.fn(), getCategories: vi.fn().mockReturnValue(of([])) };
beforeEach(() => {
    facade.query.mockReset().mockReturnValue(of({ data: [publicRecipeFixture()], page: 2, limit: 20, totalPages: 3, totalItems: 41 }));
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
    expect(harness.routeNativeElement?.querySelector('a[href="/explore/recipe"]')).not.toBeNull();
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
it('shows total results and hides reset for an unfiltered catalog', async () => {
    const harness = await RouterTestingHarness.create('/explore');
    expect(harness.routeNativeElement?.querySelector('.catalog-summary')?.textContent).toContain('PUBLIC_RECIPES.COUNT_OTHER');
    expect(harness.routeNativeElement?.querySelector('.catalog-reset button')).toBeNull();
    await harness.navigateByUrl('/explore?maxTotalTime=37');
    expect(harness.routeNativeElement?.querySelector('.catalog-reset button')).not.toBeNull();
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
