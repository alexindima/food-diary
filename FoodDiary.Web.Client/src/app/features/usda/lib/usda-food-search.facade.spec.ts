import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { APP_SEARCH_DEBOUNCE_MS } from '../../../config/runtime-ui.tokens';
import { UsdaService } from '../api/usda.service';
import type { UsdaFood } from '../models/usda.data';
import { UsdaFoodSearchFacade } from './usda-food-search.facade';

const FDC_ID = 17_000;
const SECOND_FDC_ID = 18_000;
const SEARCH_DEBOUNCE_MS = 1;
const FOOD: UsdaFood = {
    fdcId: FDC_ID,
    description: 'Apple',
    foodCategory: 'Fruit',
};
const SECOND_FOOD: UsdaFood = {
    fdcId: SECOND_FDC_ID,
    description: 'Banana',
    foodCategory: 'Fruit',
};

type UsdaServiceMock = {
    searchFoods: ReturnType<typeof vi.fn>;
};

let facade: UsdaFoodSearchFacade;
let usdaService: UsdaServiceMock;

beforeEach(() => {
    vi.useFakeTimers();
    usdaService = {
        searchFoods: vi.fn().mockReturnValue(of([FOOD])),
    };

    TestBed.configureTestingModule({
        providers: [
            UsdaFoodSearchFacade,
            { provide: UsdaService, useValue: usdaService },
            { provide: APP_SEARCH_DEBOUNCE_MS, useValue: SEARCH_DEBOUNCE_MS },
        ],
    });

    facade = TestBed.inject(UsdaFoodSearchFacade);
});

afterEach(() => {
    vi.useRealTimers();
});

describe('UsdaFoodSearchFacade', () => {
    it('does not search until query reaches minimum length', async () => {
        facade.updateSearchQuery('a');
        await flushSearchAsync();

        expect(usdaService.searchFoods).not.toHaveBeenCalled();
        expect(facade.results()).toEqual([]);
        expect(facade.isLoading()).toBe(false);
    });

    it('searches foods and stores results', async () => {
        facade.updateSearchQuery('apple');
        await flushSearchAsync();

        expect(usdaService.searchFoods).toHaveBeenCalledWith('apple');
        expect(facade.results()).toEqual([FOOD]);
        expect(facade.isLoading()).toBe(false);
    });

    it('clears selected food on query change and resets state', async () => {
        facade.selectFood(SECOND_FOOD);
        facade.updateSearchQuery('apple');
        await flushSearchAsync();

        expect(facade.selectedFood()).toBeNull();

        facade.selectFood(FOOD);
        facade.reset();

        expect(facade.searchQuery()).toBe('');
        expect(facade.results()).toEqual([]);
        expect(facade.selectedFood()).toBeNull();
        expect(facade.isLoading()).toBe(false);
    });
});

async function flushSearchAsync(): Promise<void> {
    await Promise.resolve();
    await vi.advanceTimersByTimeAsync(SEARCH_DEBOUNCE_MS);
}
