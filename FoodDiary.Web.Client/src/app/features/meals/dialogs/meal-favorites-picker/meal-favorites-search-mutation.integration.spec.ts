import { type HttpRequest, HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting, type TestRequest } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { map, of } from 'rxjs';
import { describe, expect, it, onTestFinished, vi } from 'vitest';

import { environment } from '../../../../../environments/environment';
import { stubScrollIntoView } from '../../../../../testing/dom-testing';
import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import type { FavoriteMealHttpResponse } from '../../../../shared/api/sdk/generated/model/favorite-meal-http-response';
import { FavoriteMealService } from '../../api/favorite-meal.service';
import { MealFavoritesPickerComponent, type MealFavoritesPickerData } from './meal-favorites-picker';

const FAVORITES_URL = environment.apiUrls.favoriteMeals;
const PAGE_SIZE = 10;
const SEARCH = 'rice';
const SEARCH_DELAY_MS = 300;
const FAVORITE = {
    id: '2c8e3eee-34b7-4cb7-9245-4c180fb39550',
    mealId: '2c8e3eee-34b7-4cb7-9245-4c180fb39551',
    name: 'Rice with chicken',
    mealDate: '2026-05-01T12:30:00Z',
    createdAtUtc: '2026-05-02T06:43:00Z',
    mealType: 'Lunch',
    itemCount: 1,
    itemNames: ['Rice'],
    totalCalories: 400,
    totalProteins: 25,
    totalFats: 10,
    totalCarbs: 40,
} as const satisfies FavoriteMealHttpResponse;
const OTHER_FAVORITE = {
    ...FAVORITE,
    id: '2c8e3eee-34b7-4cb7-9245-4c180fb39552',
    mealId: '2c8e3eee-34b7-4cb7-9245-4c180fb39553',
    name: 'Rice with vegetables',
};

describe('Meal Favorites native search and removal', () => {
    it('does not leave a deleted favorite actionable when a previously typed search matches it', async () => {
        const http = openFavoritesPicker();
        flushPage(
            http.expectOne(request => isPage(request) && request.params.get('search') === ''),
            [FAVORITE, OTHER_FAVORITE],
        );
        TestBed.tick();

        const picker = document.querySelector('fd-meal-favorites-picker');
        const input = picker?.querySelector('fd-ui-input input');
        const remove = picker?.querySelector('fd-favorite-meal-row button[aria-label="MEAL_DETAIL.REMOVE_FAVORITE"]');
        if (!(picker instanceof HTMLElement) || !(input instanceof HTMLInputElement) || !(remove instanceof HTMLButtonElement)) {
            throw new Error('Expected the actual favorites picker controls');
        }
        expect(activeFavoriteTitles(picker)).toEqual([FAVORITE.name, OTHER_FAVORITE.name]);
        expect(remove.disabled).toBe(false);

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

        expect(earlyPages.length + acknowledgedPages.length).toBeGreaterThan(0);
        expect(input.value).toBe(SEARCH);
        expect(input.disabled).toBe(false);
        expect(activeFavoriteTitles(picker)).toEqual([OTHER_FAVORITE.name]);
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
    const favorites = TestBed.inject(FavoriteMealService);
    const dialogs = TestBed.inject(FdUiDialogService);
    const http = TestBed.inject(HttpTestingController);
    onTestFinished(() => {
        dialogs.closeAll();
        vi.unstubAllGlobals();
        http.verify();
    });
    dialogs.open<MealFavoritesPickerComponent, MealFavoritesPickerData, boolean>(MealFavoritesPickerComponent, {
        preset: 'form',
        data: {
            remove: item => favorites.remove(item.id).pipe(map(() => true)),
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

function flushPage(request: TestRequest, data: FavoriteMealHttpResponse[]): void {
    expect(request.request.params.get('page')).toBe('1');
    expect(request.request.params.get('limit')).toBe(String(PAGE_SIZE));
    request.flush({ data, page: 1, limit: PAGE_SIZE, totalPages: 1, totalItems: data.length });
}

function activeFavoriteTitles(picker: HTMLElement): string[] {
    return Array.from(picker.querySelectorAll('.favorite-row__heading strong'), title => title.textContent.trim());
}
