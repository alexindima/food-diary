import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../../../testing/async-testing';
import { APP_SEARCH_DEBOUNCE_MS } from '../../../../config/runtime-ui.tokens';
import { NavigationService } from '../../../../services/navigation.service';
import type { PageOf } from '../../../../shared/models/page-of.data';
import { ViewportService } from '../../../../shared/platform/viewport.service';
import { QuickMealService } from '../../../meals/lib/quick/quick-meal.service';
import { FavoriteProductService } from '../../api/favorite-product.service';
import { OpenFoodFactsService } from '../../api/open-food-facts.service';
import { ProductService } from '../../api/product.service';
import { ProductDetailActionResult } from '../../components/detail/product-detail-lib/product-detail.types';
import {
    PRODUCT_LIST_FAVORITE_LIMIT,
    PRODUCT_LIST_OFF_SEARCH_LIMIT,
    PRODUCT_LIST_PAGE_SIZE,
    PRODUCT_LIST_RECENT_LIMIT,
} from '../../components/list/product-list.config';
import type { OpenFoodFactsProduct } from '../../models/open-food-facts.data';
import { type FavoriteProduct, MeasurementUnit, type Product, ProductType, ProductVisibility } from '../../models/product.data';
import { ProductListFacade } from './product-list.facade';

const ZERO_DEBOUNCE_MS = 0;
const DEBOUNCE_FLUSH_MS = 20;
const PRODUCT_CALORIES = 120;
const PRODUCT_PROTEINS = 12;
const PRODUCT_FATS = 4;
const PRODUCT_CARBS = 8;
const QUALITY_SCORE_GREEN = 80;
const DEFAULT_PORTION_AMOUNT = 100;
const FAVORITE_DEFAULT_PORTION_AMOUNT = 180;

let facade: ProductListFacade;
let productService: {
    queryOverview: ReturnType<typeof vi.fn>;
    query: ReturnType<typeof vi.fn>;
    getById: ReturnType<typeof vi.fn>;
    deleteById: ReturnType<typeof vi.fn>;
};
let favoriteProductService: {
    getAll: ReturnType<typeof vi.fn>;
    add: ReturnType<typeof vi.fn>;
    update: ReturnType<typeof vi.fn>;
    remove: ReturnType<typeof vi.fn>;
};
let openFoodFactsService: { search: ReturnType<typeof vi.fn> };
let quickMealService: { addProduct: ReturnType<typeof vi.fn> };
let navigationService: {
    navigateToProductAddAsync: ReturnType<typeof vi.fn>;
    navigateToProductEditAsync: ReturnType<typeof vi.fn>;
};
let dialogService: { open: ReturnType<typeof vi.fn> };
let toastService: { error: ReturnType<typeof vi.fn> };
let translateService: { instant: ReturnType<typeof vi.fn> };
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
        update: vi.fn(),
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
        navigateToProductEditAsync: vi.fn().mockResolvedValue(true),
    };
    dialogService = {
        open: vi.fn(),
    };
    toastService = {
        error: vi.fn(),
    };
    translateService = {
        instant: vi.fn((key: string) => key),
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
    productService.deleteById.mockReturnValue(of(void 0));
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
            { provide: FdUiToastService, useValue: toastService },
            { provide: TranslateService, useValue: translateService },
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

describe('ProductListFacade product details', () => {
    it('reloads favorites and the current page after a favorite change', async () => {
        dialogService.open.mockReturnValue({
            afterClosed: () => of(new ProductDetailActionResult('product-1', 'FavoriteChanged')),
        });
        const loadFavoritesSpy = vi.spyOn(facade, 'loadFavorites');
        const reloadSpy = vi.spyOn(facade, 'reloadCurrentPage');

        await facade.handleProductDetailsAsync(createProduct());

        expect(loadFavoritesSpy).toHaveBeenCalledTimes(1);
        expect(reloadSpy).toHaveBeenCalledTimes(1);
    });

    it('navigates to edit and does not report deletion for edit actions', async () => {
        dialogService.open.mockReturnValue({ afterClosed: () => of(new ProductDetailActionResult('product-1', 'Edit')) });

        await expect(facade.handleProductDetailsAsync(createProduct())).resolves.toBe(false);

        expect(navigationService.navigateToProductEditAsync).toHaveBeenCalledWith('product-1');
    });

    it('deletes owned products and reports the UI scroll signal', async () => {
        dialogService.open.mockReturnValue({ afterClosed: () => of(new ProductDetailActionResult('product-1', 'Delete')) });

        await expect(facade.handleProductDetailsAsync(createProduct())).resolves.toBe(true);

        expect(productService.deleteById).toHaveBeenCalledWith('product-1');
    });

    it('does not delete products that are not owned by the current user', async () => {
        dialogService.open.mockReturnValue({ afterClosed: () => of(new ProductDetailActionResult('product-1', 'Delete')) });

        await expect(facade.handleProductDetailsAsync(createProduct({ isOwnedByCurrentUser: false }))).resolves.toBe(false);

        expect(productService.deleteById).not.toHaveBeenCalled();
    });

    it('clears loading and reports an error when deletion fails', async () => {
        dialogService.open.mockReturnValue({ afterClosed: () => of(new ProductDetailActionResult('product-1', 'Delete')) });
        productService.deleteById.mockReturnValueOnce(throwError(() => new Error('Delete failed')));

        await expect(facade.handleProductDetailsAsync(createProduct())).resolves.toBe(false);

        expect(facade.productData.isLoading()).toBe(false);
        expect(toastService.error).toHaveBeenCalledWith('PRODUCT_LIST.DELETE_ERROR');
    });
});

describe('ProductListFacade search and filters', () => {
    it('clears the current search', () => {
        facade.searchForm.search().value.set('banana');

        facade.clearSearch();

        expect(facade.searchValue()).toBe('');
    });

    it('loads searched products and Open Food Facts suggestions', async () => {
        await flushDebounceAsync();
        productService.query.mockClear();

        facade.searchForm.search().value.set('banana');
        await vi.waitFor(() => {
            expect(productService.query).toHaveBeenCalledWith(
                1,
                PRODUCT_LIST_PAGE_SIZE,
                expect.objectContaining({ search: 'banana' }),
                true,
            );
        });

        expect(openFoodFactsService.search).toHaveBeenCalledWith('banana', PRODUCT_LIST_OFF_SEARCH_LIMIT);
        expect(facade.searchValue()).toBe('banana');
        expect(facade.recentProducts()).toEqual([]);
        expect(facade.productData.items().map(product => product.id)).toEqual(['query-product']);
        expect(facade.offProducts()).toEqual([createOpenFoodFactsProduct()]);
    });

    it('reloads products without public items when only-mine filter is enabled', async () => {
        await flushDebounceAsync();
        productService.query.mockClear();

        facade.toggleOnlyMine();
        await flushDebounceAsync();

        expect(facade.onlyMineFilter()).toBe(true);
        expect(productService.query).toHaveBeenCalledWith(1, PRODUCT_LIST_PAGE_SIZE, expect.anything(), false);
    });

    it('ignores stale Open Food Facts responses from previous searches', async () => {
        await flushDebounceAsync();
        productService.query.mockClear();
        const firstSearch = new Subject<OpenFoodFactsProduct[]>();
        const secondSearch = new Subject<OpenFoodFactsProduct[]>();
        openFoodFactsService.search.mockReturnValueOnce(firstSearch.asObservable()).mockReturnValueOnce(secondSearch.asObservable());

        facade.searchForm.search().value.set('banana');
        await flushDebounceAsync();
        facade.searchForm.search().value.set('apple');
        await flushDebounceAsync();
        await vi.waitFor(() => {
            expect(openFoodFactsService.search).toHaveBeenCalledTimes(2);
        });

        firstSearch.next([createOpenFoodFactsProduct({ barcode: 'stale', name: 'Stale banana' })]);
        firstSearch.complete();
        expect(facade.offProducts()).toEqual([]);

        const latest = createOpenFoodFactsProduct({ barcode: 'latest', name: 'Fresh apple' });
        secondSearch.next([latest]);
        secondSearch.complete();

        expect(facade.offProducts()).toEqual([latest]);
        expect(facade.offLoading()).toBe(false);
    });
});

describe('ProductListFacade favorites', () => {
    it('adds favorite product, syncs product state, and reloads favorites', () => {
        const product = createProduct({ id: 'product-1', isFavorite: false, favoriteProductId: null });
        facade.productData.setData(createPage([product]));

        facade.onProductFavoriteToggle(product);

        expect(favoriteProductService.add).toHaveBeenCalledWith('product-1', 'Test product', DEFAULT_PORTION_AMOUNT);
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

    it('adds favorite product to quick meal draft from favorite snapshot', () => {
        const favorite = createFavoriteProduct({ defaultPortionAmount: FAVORITE_DEFAULT_PORTION_AMOUNT, preferredPortionAmount: 250 });

        facade.addFavoriteProductToMeal(favorite);

        expect(productService.getById).not.toHaveBeenCalled();
        expect(quickMealService.addProduct).toHaveBeenCalledWith(
            expect.objectContaining({
                id: favorite.productId,
                name: favorite.name,
                brand: favorite.brand,
                barcode: favorite.barcode,
                caloriesPerBase: favorite.caloriesPerBase,
                isFavorite: true,
                favoriteProductId: favorite.id,
            }),
            FAVORITE_DEFAULT_PORTION_AMOUNT,
        );
    });
});

describe('ProductListFacade actions', () => {
    it('puts scanned barcode into search control', () => {
        dialogService.open.mockReturnValueOnce({ afterClosed: () => of('4600000000000') });

        facade.openBarcodeScanner();

        expect(facade.searchModel().search).toBe('4600000000000');
    });

    it('adds selected product to quick meal draft', () => {
        const product = createProduct();

        facade.onAddToMeal(product);

        expect(quickMealService.addProduct).toHaveBeenCalledWith(product);
    });
});

async function flushDebounceAsync(): Promise<void> {
    await waitForAsyncTasksAsync();
    await new Promise(resolve => {
        setTimeout(resolve, DEBOUNCE_FLUSH_MS);
    });
    await waitForAsyncTasksAsync();
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
        baseAmount: DEFAULT_PORTION_AMOUNT,
        defaultPortionAmount: DEFAULT_PORTION_AMOUNT,
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

function createFavoriteProduct(overrides: Partial<FavoriteProduct> = {}): FavoriteProduct {
    return {
        id: 'favorite-1',
        productId: 'product-1',
        name: 'Test product',
        createdAtUtc: '2026-01-01T00:00:00Z',
        productName: 'Test product',
        brand: null,
        barcode: null,
        comment: null,
        imageUrl: null,
        caloriesPerBase: PRODUCT_CALORIES,
        proteinsPerBase: PRODUCT_PROTEINS,
        fatsPerBase: PRODUCT_FATS,
        carbsPerBase: PRODUCT_CARBS,
        fiberPerBase: 1,
        alcoholPerBase: 0,
        qualityScore: QUALITY_SCORE_GREEN,
        qualityGrade: 'green',
        isOwnedByCurrentUser: true,
        baseUnit: MeasurementUnit.G,
        preferredPortionAmount: DEFAULT_PORTION_AMOUNT,
        defaultPortionAmount: DEFAULT_PORTION_AMOUNT,
        ...overrides,
    };
}

function createOpenFoodFactsProduct(overrides: Partial<OpenFoodFactsProduct> = {}): OpenFoodFactsProduct {
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
        ...overrides,
    };
}
