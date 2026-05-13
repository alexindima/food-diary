import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { APP_SEARCH_DEBOUNCE_MS } from '../../../../../config/runtime-ui.tokens';
import { NavigationService } from '../../../../../services/navigation.service';
import { ViewportService } from '../../../../../services/viewport.service';
import type { PageOf } from '../../../../../shared/models/page-of.data';
import { QuickMealService } from '../../../../meals/lib/quick-meal.service';
import { FavoriteProductService } from '../../../api/favorite-product.service';
import { type OpenFoodFactsProduct, OpenFoodFactsService } from '../../../api/open-food-facts.service';
import { ProductService } from '../../../api/product.service';
import { type FavoriteProduct, MeasurementUnit, type Product, ProductType, ProductVisibility } from '../../../models/product.data';
import {
    PRODUCT_LIST_FAVORITE_LIMIT,
    PRODUCT_LIST_OFF_SEARCH_LIMIT,
    PRODUCT_LIST_PAGE_SIZE,
    PRODUCT_LIST_RECENT_LIMIT,
} from '../product-list.config';
import { ProductListFacade } from './product-list.facade';

const ZERO_DEBOUNCE_MS = 0;
const DEBOUNCE_FLUSH_MS = 1;
const PRODUCT_CALORIES = 120;
const PRODUCT_PROTEINS = 12;
const PRODUCT_FATS = 4;
const PRODUCT_CARBS = 8;
const QUALITY_SCORE_GREEN = 80;

let facade: ProductListFacade;
let productService: {
    queryOverview: ReturnType<typeof vi.fn>;
    query: ReturnType<typeof vi.fn>;
    getById: ReturnType<typeof vi.fn>;
    deleteById: ReturnType<typeof vi.fn>;
};
let favoriteProductService: { getAll: ReturnType<typeof vi.fn>; add: ReturnType<typeof vi.fn>; remove: ReturnType<typeof vi.fn> };
let openFoodFactsService: { search: ReturnType<typeof vi.fn> };
let quickMealService: { addProduct: ReturnType<typeof vi.fn> };
let navigationService: { navigateToProductAddAsync: ReturnType<typeof vi.fn> };
let dialogService: { open: ReturnType<typeof vi.fn> };
let isMobile: ReturnType<typeof signal<boolean>>;

beforeEach(() => {
    productService = {
        queryOverview: vi.fn(),
        query: vi.fn(),
        getById: vi.fn(),
        deleteById: vi.fn(),
    };
    favoriteProductService = {
        getAll: vi.fn(),
        add: vi.fn(),
        remove: vi.fn(),
    };
    openFoodFactsService = {
        search: vi.fn(),
    };
    quickMealService = {
        addProduct: vi.fn(),
    };
    navigationService = {
        navigateToProductAddAsync: vi.fn().mockResolvedValue(true),
    };
    dialogService = {
        open: vi.fn(),
    };
    isMobile = signal(false);

    productService.queryOverview.mockReturnValue(
        of({
            recentItems: [createProduct({ id: 'recent-product', name: 'Recent product' })],
            allProducts: createPage([
                createProduct({ id: 'recent-product', name: 'Recent product' }),
                createProduct({ id: 'all-product', name: 'All product' }),
            ]),
            favoriteItems: [createFavoriteProduct()],
            favoriteTotalCount: 1,
        }),
    );
    productService.query.mockReturnValue(of(createPage([createProduct({ id: 'query-product', name: 'Query product' })])));
    productService.getById.mockReturnValue(of(createProduct()));
    productService.deleteById.mockReturnValue(of(undefined));
    favoriteProductService.getAll.mockReturnValue(of([createFavoriteProduct()]));
    favoriteProductService.add.mockReturnValue(of(createFavoriteProduct()));
    favoriteProductService.remove.mockReturnValue(of(null));
    openFoodFactsService.search.mockReturnValue(of([createOpenFoodFactsProduct()]));
    dialogService.open.mockReturnValue({ afterClosed: () => of(null) });

    TestBed.configureTestingModule({
        providers: [
            ProductListFacade,
            { provide: ProductService, useValue: productService },
            { provide: FavoriteProductService, useValue: favoriteProductService },
            { provide: OpenFoodFactsService, useValue: openFoodFactsService },
            { provide: QuickMealService, useValue: quickMealService },
            { provide: NavigationService, useValue: navigationService },
            { provide: FdUiDialogService, useValue: dialogService },
            { provide: ViewportService, useValue: { isMobile } },
            { provide: APP_SEARCH_DEBOUNCE_MS, useValue: ZERO_DEBOUNCE_MS },
        ],
    });

    facade = TestBed.inject(ProductListFacade);
});

describe('ProductListFacade overview', () => {
    it('loads initial overview and hides duplicate recent products from the all-products section', () => {
        expect(productService.queryOverview).toHaveBeenCalledWith({
            page: 1,
            limit: PRODUCT_LIST_PAGE_SIZE,
            includePublic: true,
            recentLimit: PRODUCT_LIST_RECENT_LIMIT,
            favoriteLimit: PRODUCT_LIST_FAVORITE_LIMIT,
        });
        expect(facade.recentProducts().map(product => product.id)).toEqual(['recent-product']);
        expect(facade.allProductItems().map(item => item.product.id)).toEqual(['all-product']);
        expect(facade.favorites()).toEqual([createFavoriteProduct()]);
        expect(facade.favoriteTotalCount()).toBe(1);
        expect(facade.errorKey()).toBeNull();
    });

    it('sets error state when overview loading fails', () => {
        productService.queryOverview.mockReturnValueOnce(throwError(() => new Error('Load failed')));

        facade.retryLoad();

        expect(facade.productData.items()).toEqual([]);
        expect(facade.recentProducts()).toEqual([]);
        expect(facade.favorites()).toEqual([]);
        expect(facade.favoriteTotalCount()).toBe(0);
        expect(facade.errorKey()).toBe('ERRORS.LOAD_FAILED_TITLE');
    });
});

describe('ProductListFacade search and filters', () => {
    it('loads searched products and Open Food Facts suggestions', async () => {
        productService.query.mockClear();

        facade.searchForm.controls.search.setValue('banana');
        await flushDebounceAsync();

        expect(productService.query).toHaveBeenCalledWith(1, PRODUCT_LIST_PAGE_SIZE, expect.objectContaining({ search: 'banana' }), true);
        expect(openFoodFactsService.search).toHaveBeenCalledWith('banana', PRODUCT_LIST_OFF_SEARCH_LIMIT);
        expect(facade.searchValue()).toBe('banana');
        expect(facade.recentProducts()).toEqual([]);
        expect(facade.productData.items().map(product => product.id)).toEqual(['query-product']);
        expect(facade.offProducts()).toEqual([createOpenFoodFactsProduct()]);
    });

    it('reloads products without public items when only-mine filter is enabled', () => {
        productService.query.mockClear();

        facade.toggleOnlyMine();

        expect(facade.onlyMineFilter()).toBe(true);
        expect(productService.query).toHaveBeenCalledWith(1, PRODUCT_LIST_PAGE_SIZE, expect.anything(), false);
    });
});

describe('ProductListFacade favorites', () => {
    it('adds favorite product, syncs product state, and reloads favorites', () => {
        const product = createProduct({ id: 'product-1', isFavorite: false, favoriteProductId: null });
        facade.productData.setData(createPage([product]));

        facade.onProductFavoriteToggle(product);

        expect(favoriteProductService.add).toHaveBeenCalledWith('product-1', 'Test product');
        expect(favoriteProductService.getAll).toHaveBeenCalled();
        expect(facade.productData.items()[0]).toEqual(expect.objectContaining({ isFavorite: true, favoriteProductId: 'favorite-1' }));
        expect(facade.favoriteLoadingIds().size).toBe(0);
    });

    it('removes favorite product by looking up favorite id when product state has no favorite id', () => {
        const product = createProduct({ id: 'product-1', isFavorite: true, favoriteProductId: null });
        facade.productData.setData(createPage([product]));

        facade.onProductFavoriteToggle(product);

        expect(favoriteProductService.getAll).toHaveBeenCalled();
        expect(favoriteProductService.remove).toHaveBeenCalledWith('favorite-1');
        expect(facade.productData.items()[0]).toEqual(expect.objectContaining({ isFavorite: false, favoriteProductId: null }));
        expect(facade.favoriteLoadingIds().size).toBe(0);
    });
});

describe('ProductListFacade actions', () => {
    it('puts scanned barcode into search control', () => {
        dialogService.open.mockReturnValueOnce({ afterClosed: () => of('4600000000000') });

        facade.openBarcodeScanner();

        expect(facade.searchForm.controls.search.value).toBe('4600000000000');
    });

    it('adds selected product to quick meal draft', () => {
        const product = createProduct();

        facade.onAddToMeal(product);

        expect(quickMealService.addProduct).toHaveBeenCalledWith(product);
    });
});

async function flushDebounceAsync(): Promise<void> {
    await new Promise(resolve => {
        setTimeout(resolve, DEBOUNCE_FLUSH_MS);
    });
}

function createPage(data: Product[]): PageOf<Product> {
    return {
        data,
        page: 1,
        limit: PRODUCT_LIST_PAGE_SIZE,
        totalPages: 1,
        totalItems: data.length,
    };
}

function createProduct(overrides: Partial<Product> = {}): Product {
    return {
        id: 'product-1',
        name: 'Test product',
        barcode: null,
        brand: null,
        productType: ProductType.Other,
        category: null,
        description: null,
        comment: null,
        imageUrl: null,
        imageAssetId: null,
        baseUnit: MeasurementUnit.G,
        baseAmount: 100,
        defaultPortionAmount: 100,
        caloriesPerBase: PRODUCT_CALORIES,
        proteinsPerBase: PRODUCT_PROTEINS,
        fatsPerBase: PRODUCT_FATS,
        carbsPerBase: PRODUCT_CARBS,
        fiberPerBase: 1,
        alcoholPerBase: 0,
        usageCount: 0,
        visibility: ProductVisibility.Private,
        createdAt: new Date('2026-01-01T00:00:00Z'),
        isOwnedByCurrentUser: true,
        qualityScore: QUALITY_SCORE_GREEN,
        qualityGrade: 'green',
        ...overrides,
    };
}

function createFavoriteProduct(): FavoriteProduct {
    return {
        id: 'favorite-1',
        productId: 'product-1',
        name: 'Test product',
        createdAtUtc: '2026-01-01T00:00:00Z',
        productName: 'Test product',
        brand: null,
        imageUrl: null,
        caloriesPerBase: PRODUCT_CALORIES,
        baseUnit: MeasurementUnit.G,
        defaultPortionAmount: 100,
    };
}

function createOpenFoodFactsProduct(): OpenFoodFactsProduct {
    return {
        barcode: '4600000000000',
        name: 'Banana',
        brand: 'Open Food Facts',
        category: 'Fruit',
        imageUrl: null,
        caloriesPer100G: PRODUCT_CALORIES,
        proteinsPer100G: PRODUCT_PROTEINS,
        fatsPer100G: PRODUCT_FATS,
        carbsPer100G: PRODUCT_CARBS,
        fiberPer100G: 1,
    };
}
