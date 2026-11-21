import { type HttpRequest, HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting, type TestRequest } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { catchError, map, of } from 'rxjs';
import { describe, expect, it, onTestFinished, vi } from 'vitest';

import { environment } from '../../../../../environments/environment';
import { stubScrollIntoView } from '../../../../../testing/dom-testing';
import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import type { FavoriteRecipeHttpResponse } from '../../../../shared/api/sdk/generated/model/favorite-recipe-http-response';
import { FavoriteRecipeService } from '../../api/favorite-recipe.service';
import { RecipeFavoritesPickerComponent, type RecipeFavoritesPickerData } from './recipe-favorites-picker';

const FAVORITES_URL = environment.apiUrls.favoriteRecipes;
const PAGE_SIZE = 10;
const SEARCH = 'rice';
const SEARCH_DELAY_MS = 300;
const FAVORITE = {
    id: '2c8e3eee-34b7-4cb7-9245-4c180fb39550',
    recipeId: '2c8e3eee-34b7-4cb7-9245-4c180fb39551',
    recipeName: 'Rice bowl',
    servings: 2,
    createdAtUtc: '2026-05-02T06:43:00Z',
    ingredientCount: 1,
    ingredientNames: ['Rice'],
} as const satisfies FavoriteRecipeHttpResponse;
const OTHER_FAVORITE = {
    ...FAVORITE,
    id: '2c8e3eee-34b7-4cb7-9245-4c180fb39552',
    recipeId: '2c8e3eee-34b7-4cb7-9245-4c180fb39553',
    recipeName: 'Rice soup',
};

describe('Recipe Favorites native search and removal', () => {
    it('does not leave a deleted recipe actionable when a previously typed search matches it', async () => {
        const http = openFavoritesPicker();
        flushPage(
            http.expectOne(request => isPage(request) && request.params.get('search') === ''),
            [FAVORITE, OTHER_FAVORITE],
        );
        TestBed.tick();

        const picker = document.querySelector('fd-recipe-favorites-picker');
        const input = picker?.querySelector('fd-ui-input input');
        const remove = picker?.querySelector('fd-favorite-recipe-row button[aria-label="RECIPE_DETAIL.REMOVE_FAVORITE"]');
        if (!(picker instanceof HTMLElement) || !(input instanceof HTMLInputElement) || !(remove instanceof HTMLButtonElement)) {
            throw new Error('Expected the actual recipe favorites picker controls');
        }
        expect(activeFavoriteTitles(picker)).toEqual([FAVORITE.recipeName, OTHER_FAVORITE.recipeName]);
        expect(input.disabled).toBe(false);
        expect(remove.disabled).toBe(false);
        expect(remove.getAttribute('aria-pressed')).toBe('true');

        vi.useFakeTimers({ toFake: ['Date', 'setInterval', 'clearInterval', 'setTimeout', 'clearTimeout'] });
        onTestFinished(() => {
            vi.useRealTimers();
        });
        input.value = SEARCH;
        input.dispatchEvent(new Event('input', { bubbles: true }));
        http.expectNone(isFilteredPage);
        remove.click();
        const removal = http.expectOne({ method: 'DELETE', url: `${FAVORITES_URL}/${FAVORITE.id}` });
        TestBed.tick();
        expect(input.disabled).toBe(true);

        await vi.advanceTimersByTimeAsync(SEARCH_DELAY_MS);
        const earlyPages = http.match(isFilteredPage);
        for (const request of earlyPages) {
            flushPage(request, [FAVORITE, OTHER_FAVORITE]);
        }
        TestBed.tick();
        expect(removal.cancelled).toBe(false);

        removal.flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });
        TestBed.tick();
        await vi.advanceTimersByTimeAsync(0);
        TestBed.tick();
        const acknowledgedPages = http.match(isFilteredPage);
        for (const request of acknowledgedPages) {
            flushPage(request, [OTHER_FAVORITE]);
        }
        TestBed.tick();

        expect(input.value).toBe(SEARCH);
        expect(input.disabled).toBe(false);
        expect(activeFavoriteTitles(picker)).toEqual([OTHER_FAVORITE.recipeName]);
        expect(earlyPages).toHaveLength(0);
        expect(acknowledgedPages).toHaveLength(1);
    });
});

function openFavoritesPicker(): HttpTestingController {
    vi.stubGlobal('matchMedia', (media: string) => ({
        matches: false,
        media,
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
    }));
    stubScrollIntoView();
    TestBed.configureTestingModule({
        providers: [provideHttpClient(), provideHttpClientTesting(), provideTranslateTesting()],
    });
    const favorites = TestBed.inject(FavoriteRecipeService);
    const dialogs = TestBed.inject(FdUiDialogService);
    const http = TestBed.inject(HttpTestingController);
    onTestFinished(() => {
        dialogs.closeAll();
        vi.unstubAllGlobals();
        http.verify();
    });
    dialogs.open<RecipeFavoritesPickerComponent, RecipeFavoritesPickerData, boolean>(RecipeFavoritesPickerComponent, {
        size: 'md',
        data: {
            remove: item =>
                favorites.remove(item.id).pipe(
                    map(() => true),
                    catchError(() => of(false)),
                ),
            repeat: () => of(false),
            restore: () => of(false),
        },
    });
    TestBed.tick();
    return http;
}

function isPage(request: HttpRequest<unknown>): boolean {
    return request.method === 'GET' && request.url === `${FAVORITES_URL}/page`;
}

function isFilteredPage(request: HttpRequest<unknown>): boolean {
    return isPage(request) && request.params.get('search') === SEARCH;
}

function flushPage(request: TestRequest, data: FavoriteRecipeHttpResponse[]): void {
    expect(request.request.params.get('page')).toBe('1');
    expect(request.request.params.get('limit')).toBe(String(PAGE_SIZE));
    request.flush({ data, page: 1, limit: PAGE_SIZE, totalPages: 1, totalItems: data.length });
}

function activeFavoriteTitles(picker: HTMLElement): string[] {
    return Array.from(picker.querySelectorAll('.favorite-row__heading strong'), title => title.textContent.trim());
}
