import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { NAME_SEARCH_DEBOUNCE_MS } from '../../../../../config/runtime-ui.tokens';
import { ProductService } from '../../../api/product.service';
import { PRODUCT_NAME_SEARCH_SUGGESTION_LIMIT } from '../../../lib/product-manage.constants';
import type { ProductSearchSuggestion } from '../../../models/product.data';
import { ProductNameSearchFacade } from './product-name-search.facade';

const ZERO_DEBOUNCE_MS = 0;
const DEBOUNCE_FLUSH_MS = 1;
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
            { provide: NAME_SEARCH_DEBOUNCE_MS, useValue: ZERO_DEBOUNCE_MS },
        ],
    });

    facade = TestBed.inject(ProductNameSearchFacade);
});

describe('ProductNameSearchFacade search', () => {
    it('clears options and skips API request when query is shorter than the minimum length', async () => {
        facade.search('ap');
        await flushDebounceAsync();

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
        await flushDebounceAsync();

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
        await flushDebounceAsync();

        expect(facade.options()).toEqual([]);
        expect(facade.isLoading()).toBe(false);
    });
});

async function flushDebounceAsync(): Promise<void> {
    await new Promise(resolve => {
        setTimeout(resolve, DEBOUNCE_FLUSH_MS);
    });
}

describe('ProductNameSearchFacade selection', () => {
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
