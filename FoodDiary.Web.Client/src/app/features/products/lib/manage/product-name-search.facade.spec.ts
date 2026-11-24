import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { NAME_SEARCH_DEBOUNCE_MS } from '../../../../config/runtime-ui.tokens';
import type { ProductSearchSuggestion } from '../../../../shared/models/product.data';
import { ProductService } from '../../api/product.service';
import { PRODUCT_NAME_SEARCH_SUGGESTION_LIMIT } from '../product-manage.constants';
import { ProductNameSearchFacade } from './product-name-search.facade';

const DEBOUNCE_MS = 20;
const USDA_FDC_ID = 12345;

let facade: ProductNameSearchFacade;
let productService: { searchSuggestions: ReturnType<typeof vi.fn> };

beforeEach(() => {
    productService = {
        searchSuggestions: vi.fn().mockReturnValue(of([])),
    };

    TestBed.configureTestingModule({
        providers: [
            ProductNameSearchFacade,
            { provide: ProductService, useValue: productService },
            { provide: NAME_SEARCH_DEBOUNCE_MS, useValue: DEBOUNCE_MS },
        ],
    });

    facade = TestBed.inject(ProductNameSearchFacade);
});

describe('ProductNameSearchFacade search', () => {
    it('clears options and skips API request when query is shorter than the minimum length', async () => {
        facade.search('ap');
        await settleSearchAsync();

        expect(productService.searchSuggestions).not.toHaveBeenCalled();
        expect(facade.options()).toEqual([]);
        expect(facade.isLoading()).toBe(false);
    });

    it('trims query and maps USDA and Open Food Facts suggestions to autocomplete options', async () => {
        const suggestions: ProductSearchSuggestion[] = [
            {
                source: 'usda',
                name: 'Apple, raw',
                brand: null,
                category: 'Fruits',
                usdaFdcId: USDA_FDC_ID,
            },
            {
                source: 'openFoodFacts',
                name: 'Apple yogurt',
                brand: 'Dairy Co',
                barcode: '4600000000000',
            },
        ];
        productService.searchSuggestions.mockReturnValueOnce(of(suggestions));

        facade.search(' apple ');
        await settleSearchAsync();

        expect(productService.searchSuggestions).toHaveBeenCalledWith('apple', PRODUCT_NAME_SEARCH_SUGGESTION_LIMIT);
        expect(facade.options()).toEqual([
            {
                id: `usda:${USDA_FDC_ID}`,
                value: 'Apple, raw',
                label: 'Apple, raw',
                hint: 'Fruits',
                badge: 'USDA',
                data: suggestions[0],
            },
            {
                id: 'open-food-facts:4600000000000',
                value: 'Apple yogurt',
                label: 'Apple yogurt',
                hint: 'Dairy Co',
                badge: 'Open Food Facts',
                data: suggestions[1],
            },
        ]);
        expect(facade.isLoading()).toBe(false);
    });

    it('returns empty options and clears loading state when suggestions request fails', async () => {
        productService.searchSuggestions.mockReturnValueOnce(throwError(() => new Error('Search failed')));

        facade.search('apple');
        await settleSearchAsync();

        expect(facade.options()).toEqual([]);
        expect(facade.isLoading()).toBe(false);
    });

    it('restarts the debounce and can search for the same trimmed query again', async () => {
        vi.useFakeTimers({ toFake: ['setInterval', 'clearInterval'] });
        try {
            facade.search('apple');
            TestBed.tick();
            vi.advanceTimersByTime(DEBOUNCE_MS - 1);

            facade.search(' apple ');
            TestBed.tick();
            vi.advanceTimersByTime(DEBOUNCE_MS - 1);
            expect(productService.searchSuggestions).not.toHaveBeenCalled();

            vi.advanceTimersByTime(1);
            await settleSearchAsync();
            expect(productService.searchSuggestions).toHaveBeenCalledExactlyOnceWith('apple', PRODUCT_NAME_SEARCH_SUGGESTION_LIMIT);

            facade.search('apple');
            TestBed.tick();
            vi.advanceTimersByTime(DEBOUNCE_MS);
            await settleSearchAsync();
            expect(productService.searchSuggestions).toHaveBeenCalledTimes(2);
        } finally {
            vi.useRealTimers();
        }
    });
});

async function settleSearchAsync(): Promise<void> {
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();
}

describe('ProductNameSearchFacade selection', () => {
    it('keeps a USDA suggestion selectable when an older response omits its source ID', async () => {
        const legacy: ProductSearchSuggestion = {
            source: 'usda',
            name: 'Rice',
            barcode: null,
            imageUrl: null,
            caloriesPer100G: 0,
        };
        const identified = { ...legacy, usdaFdcId: USDA_FDC_ID };
        productService.searchSuggestions.mockReturnValueOnce(of([legacy, identified]));

        facade.search(legacy.name);
        await settleSearchAsync();

        expect(facade.options()).toMatchObject([
            { id: 'usda:Rice', label: 'Rice', badge: 'USDA', data: legacy },
            { id: `usda:${USDA_FDC_ID}`, label: 'Rice', badge: 'USDA', data: identified },
        ]);
        const [option] = facade.options();
        facade.setSelectedSuggestion(legacy);
        expect(facade.options()).toEqual([option]);
    });

    it('sets selected suggestion as the only autocomplete option', () => {
        const suggestion: ProductSearchSuggestion = {
            source: 'openFoodFacts',
            name: 'Greek yogurt',
            barcode: null,
            category: 'Dairy',
        };

        facade.setSelectedSuggestion(suggestion);

        expect(facade.options()).toEqual([
            {
                id: 'open-food-facts:Greek yogurt',
                value: 'Greek yogurt',
                label: 'Greek yogurt',
                hint: 'Dairy',
                badge: 'Open Food Facts',
                data: suggestion,
            },
        ]);
    });
});

describe('Product name search lifecycle', () => {
    it('cancels the previous request when a newer query is dispatched', async () => {
        const old = new Subject<ProductSearchSuggestion[]>();
        const current = new Subject<ProductSearchSuggestion[]>();
        productService.searchSuggestions.mockReturnValueOnce(old).mockReturnValueOnce(current);
        facade.search('apple');
        TestBed.tick();
        await vi.waitFor(() => {
            expect(old.observed).toBe(true);
        });

        facade.search('banana');
        TestBed.tick();
        expect(old.observed).toBe(false);
        expect(productService.searchSuggestions).toHaveBeenCalledOnce();
        await vi.waitFor(() => {
            expect(current.observed).toBe(true);
        });
        current.next([{ source: 'usda', name: 'Banana', usdaFdcId: 7 }]);
        old.next([{ source: 'usda', name: 'Apple', usdaFdcId: 8 }]);
        await settleSearchAsync();
        expect(facade.options()[0].label).toBe('Banana');
        expect(facade.isLoading()).toBe(false);
    });
    it('cancels a pending lookup when the form scope is destroyed', async () => {
        const pending = new Subject<ProductSearchSuggestion[]>();
        productService.searchSuggestions.mockReturnValue(pending);
        facade.search('apple');
        TestBed.tick();
        await vi.waitFor(() => {
            expect(pending.observed).toBe(true);
        });
        TestBed.resetTestingModule();
        expect(pending.observed).toBe(false);
    });
});
