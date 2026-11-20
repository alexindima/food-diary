import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { afterEach, expect, it } from 'vitest';

import { environment } from '../../../../../environments/environment';
import { waitForAsyncTasksAsync } from '../../../../../testing/async-testing';
import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { AuthService } from '../../../../services/auth.service';
import type { FavoriteRecipeHttpResponse } from '../../../../shared/api/sdk/generated/model/favorite-recipe-http-response';
import { entityId } from '../../../../shared/models/semantics/entity-id';
import { publicRecipeFixture } from '../../lib/public-recipe.test-helper';
import { PublicRecipeCatalogComponent } from './public-catalog';

const lookupUrl = `${environment.apiUrls.favoriteRecipes}/page`;

afterEach(() => {
    TestBed.inject(HttpTestingController).verify();
});

it('keeps current favorites usable when an earlier account catalog lookup fails late', async () => {
    const owner = signal(entityId<'user'>('00000000-0000-4000-8000-000000000510'));
    const recipe = { ...publicRecipeFixture(), id: entityId<'recipe'>('00000000-0000-4000-8000-000000000520') };
    const currentFavoriteId = entityId<'favorite-recipe'>('00000000-0000-4000-8000-000000000530');
    const currentFavorite: FavoriteRecipeHttpResponse = {
        id: currentFavoriteId,
        recipeId: recipe.id,
        recipeName: recipe.name,
        createdAtUtc: '2026-10-13T12:00:00Z',
        servings: recipe.servings,
        ingredientCount: 0,
    };
    TestBed.configureTestingModule({
        providers: [
            provideRouter([{ path: 'explore', component: PublicRecipeCatalogComponent }]),
            provideHttpClient(),
            provideHttpClientTesting(),
            provideTranslateTesting(),
            { provide: AuthService, useValue: { isAuthenticated: signal(true), getUserId: owner } },
        ],
    });
    const http = TestBed.inject(HttpTestingController);
    const harness = await RouterTestingHarness.create('/explore?maxTotalTime=30');
    for (const bundle of http.match(request => request.method === 'GET' && request.url.startsWith('./assets/i18n/'))) {
        bundle.flush({});
    }
    const previousLookup = http.expectOne(
        request => request.method === 'GET' && request.url === lookupUrl && request.params.get('page') === '1',
    );

    const catalog = http.expectOne(request => request.method === 'GET' && request.url === `${environment.apiUrls.recipes}/public`);
    expect(catalog.request.params.get('maxTotalTime')).toBe('30');
    catalog.flush({ data: [recipe], page: 1, limit: 20, totalPages: 1, totalItems: 1 });
    harness.detectChanges();
    const root = harness.routeNativeElement;
    if (root === null) {
        throw new Error('Catalog route did not render');
    }
    const favoriteButton = root.querySelector<HTMLButtonElement>('.catalog-favorite button');
    const status = root.querySelector<HTMLParagraphElement>('p[role="status"]');
    if (favoriteButton === null || status === null) {
        throw new Error('Catalog favorite controls were not rendered');
    }
    expect(favoriteButton.getAttribute('aria-label')).toBe('PUBLIC_RECIPES.SAVE');

    owner.set(entityId<'user'>('00000000-0000-4000-8000-000000000540'));
    harness.detectChanges();
    http.expectOne(request => request.method === 'GET' && request.url === lookupUrl && request.params.get('page') === '1').flush({
        data: [currentFavorite],
        page: 1,
        limit: 100,
        totalPages: 1,
        totalItems: 1,
    });
    await waitForAsyncTasksAsync();
    harness.detectChanges();
    expect(favoriteButton.getAttribute('aria-label')).toBe('PUBLIC_RECIPES.UNSAVE');

    previousLookup.flush({}, { status: HttpStatusCode.ServiceUnavailable, statusText: 'Unavailable' });
    await waitForAsyncTasksAsync();
    harness.detectChanges();
    expect(status.hidden).toBe(true);
    expect(favoriteButton.getAttribute('aria-label')).toBe('PUBLIC_RECIPES.UNSAVE');
    favoriteButton.click();
    await waitForAsyncTasksAsync();
    http.expectNone(request => request.method === 'GET' && request.url === lookupUrl);
    const remove = http.expectOne({ method: 'DELETE', url: `${environment.apiUrls.favoriteRecipes}/${currentFavoriteId}` });
    remove.flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });
    await waitForAsyncTasksAsync();
    harness.detectChanges();

    expect(status.hidden).toBe(false);
    expect(status.textContent.trim()).toBe('PUBLIC_RECIPES.REMOVED');
    expect(favoriteButton.getAttribute('aria-label')).toBe('PUBLIC_RECIPES.SAVE');
});
