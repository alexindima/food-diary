import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { AuthService } from '../../../../services/auth.service';
import { publicRecipeFixture } from '../../lib/public-recipe.test-helper';
import { PublicRecipesFacade } from '../../lib/public-recipes.facade';
import { PublicRecipeCatalogComponent } from './public-catalog';

describe('public catalog navigation', () => {
    const facade = { query: vi.fn() };
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
            set: { providers: [{ provide: PublicRecipesFacade, useValue: facade }] },
        });
    });
    it('loads bookmarked filters and page without resetting to the first page', async () => {
        const harness = await RouterTestingHarness.create('/explore?page=2&search=Soup&category=Dinner&maxTotalTime=30');
        expect(facade.query).toHaveBeenLastCalledWith({ page: 2, search: 'Soup', category: 'Dinner', maxTotalTime: 30 });
        expect(harness.routeNativeElement?.querySelector('a[href="/explore/recipe"]')).not.toBeNull();
        await harness.navigateByUrl('/explore?page=3&search=Soup&category=Dinner&maxTotalTime=30');
        expect(facade.query).toHaveBeenLastCalledWith({ page: 3, search: 'Soup', category: 'Dinner', maxTotalTime: 30 });
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
});
