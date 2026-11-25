import { HttpContext } from '@angular/common/http';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import type { Observable } from 'rxjs';
import { of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, onTestFinished, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../../../testing/async-testing';
import { APP_SEARCH_DEBOUNCE_MS } from '../../../../config/runtime-ui.tokens';
import { NavigationService } from '../../../../services/navigation.service';
import type { PageOf } from '../../../../shared/models/page-of.data';
import {
    type FavoriteProduct,
    MeasurementUnit,
    type Product,
    type ProductOverview,
    ProductType,
    ProductVisibility,
} from '../../../../shared/models/product.data';
import { utcInstant } from '../../../../shared/models/semantics/date-value';
import { entityId } from '../../../../shared/models/semantics/entity-id';
import { ViewportService } from '../../../../shared/platform/viewport.service';
import { QuickMealService } from '../../../meals/contracts/quick-meal';
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
import type { ProductListFiltersDialogResult } from '../../components/list/product-list-filters-dialog/product-list-filters-dialog.types';
import type { OpenFoodFactsProduct } from '../../models/open-food-facts.data';
import { ProductListFacade } from './product-list.facade';

const ZERO_DEBOUNCE_MS = 0;
const DEBOUNCE_FLUSH_MS = 20;
const PRODUCT_CALORIES = 120;
const PRODUCT_PROTEINS = 12;
const PRODUCT_FATS = 4;
const PRODUCT_CARBS = 8;
const QUALITY_SCORE_GREEN = 80;
const DEFAULT_PORTION_AMOUNT = 100;

let facade: ProductListFacade;
let productService: {
    queryOverview: ReturnType<typeof vi.fn>;
    query: ReturnType<typeof vi.fn>;
    getById: ReturnType<typeof vi.fn>;
    deleteById: ReturnType<typeof vi.fn>;
};
let favoriteProductService: {
    getLookupPage: ReturnType<typeof vi.fn>;
    getPage: ReturnType<typeof vi.fn>;
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
let translateService: {
    instant: ReturnType<typeof vi.fn>;
    onLangChange: Subject<{ lang: string }>;
    getCurrentLang: () => string;
};
let isMobile: ReturnType<typeof signal<boolean>>;

beforeEach(() => {
    productService = {
        queryOverview: vi.fn(),
        query: vi.fn(),
        getById: vi.fn(),
        deleteById: vi.fn(),
    };
    favoriteProductService = {
        getLookupPage: vi.fn(),
        getPage: vi.fn().mockReturnValue(of({ data: [], totalItems: 1 })),
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
        open: vi.fn().mockReturnValue({ afterClosed: () => of(undefined), close: vi.fn() }),
    };
    toastService = {
        error: vi.fn(),
    };
    translateService = {
        instant: vi.fn((key: string) => key),
        onLangChange: new Subject<{ lang: string }>(),
        getCurrentLang: (): string => 'en',
    };
    isMobile = signal(false);

    productService.queryOverview.mockReturnValue(
        of({
            recentItems: [createProduct({ id: entityId<'product'>('recent-product'), name: 'Recent product' })],
            allProducts: createPage([
                createProduct({ id: entityId<'product'>('recent-product'), name: 'Recent product' }),
                createProduct({ id: entityId<'product'>('all-product'), name: 'All product' }),
            ]),
            favoriteItems: [createFavoriteProduct()],
            favoriteTotalCount: 1,
        }),
    );
    productService.query.mockReturnValue(
        of(createPage([createProduct({ id: entityId<'product'>('query-product'), name: 'Query product' })])),
    );
    productService.getById.mockReturnValue(of(createProduct()));
    productService.deleteById.mockReturnValue(of(void 0));
    favoriteProductService.getLookupPage.mockReturnValue(of([createFavoriteProduct()]));
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
    TestBed.tick();
});

describe('ProductListFacade overview', () => {
    it('loads recent shortcuts without removing products from the catalog page', () => {
        expect(productService.queryOverview).toHaveBeenCalledWith(
            {
                page: 1,
                limit: PRODUCT_LIST_PAGE_SIZE,
                includePublic: true,
                recentLimit: PRODUCT_LIST_RECENT_LIMIT,
                favoriteLimit: PRODUCT_LIST_FAVORITE_LIMIT,
            },
            expect.any(HttpContext),
        );
        expect(facade.recentProducts().map(product => product.id)).toEqual(['recent-product']);
        expect(facade.allProductItems().map(item => item.product.id)).toEqual(['recent-product', 'all-product']);
        expect(facade.favorites()).toEqual([createFavoriteProduct()]);
        expect(facade.favoriteTotalCount()).toBe(1);
        expect(facade.errorKey()).toBeNull();
    });

    it('sets error state when overview loading fails', () => {
        productService.queryOverview.mockReturnValueOnce(throwError(() => new Error('Load failed')));

        facade.retryLoad();
        TestBed.tick();

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

    it('reports the UI scroll signal after the catalog refresh finishes for an owned product deletion', async () => {
        dialogService.open.mockReturnValue({ afterClosed: () => of(new ProductDetailActionResult('product-1', 'Delete')) });
        const refresh = new Subject<ProductOverview>();
        productService.queryOverview.mockReturnValueOnce(refresh);
        const completed = vi.fn();
        const deletion = facade.handleProductDetailsAsync(createProduct()).then(completed);

        await vi.waitFor(() => {
            expect(refresh.observed).toBe(true);
        });
        expect(productService.deleteById).toHaveBeenCalledWith('product-1');
        expect(completed).not.toHaveBeenCalled();
        expect(facade.productData.isLoading()).toBe(true);

        refresh.next({ allProducts: createPage([]), recentItems: [], favoriteItems: [], favoriteTotalCount: 0 });
        await deletion;

        expect(completed).toHaveBeenCalledExactlyOnceWith(true);
        expect(facade.productData.items()).toEqual([]);
        expect(facade.productData.isLoading()).toBe(false);
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

describe('ProductListFacade filter dialog', () => {
    it('leaves the current overview in place when filters are dismissed or reapplied unchanged', async () => {
        await flushDebounceAsync();
        const defaults: ProductListFiltersDialogResult = {
            onlyMine: false,
            productTypes: [],
            caloriesFrom: null,
            caloriesTo: null,
            hasImage: null,
        };
        dialogService.open.mockReturnValueOnce({ afterClosed: () => of(null) }).mockReturnValueOnce({ afterClosed: () => of(defaults) });

        facade.openFilters();
        facade.openFilters();
        await flushDebounceAsync();

        expect(dialogService.open).toHaveBeenLastCalledWith(expect.any(Function), { preset: 'form', data: defaults });
        expect(productService.query).not.toHaveBeenCalled();
        expect(productService.queryOverview).toHaveBeenCalledOnce();
        expect(facade.showRecentSection()).toBe(true);
    });

    it.each([false, true])('applies all catalog filters in one request, onlyMine=%s', async onlyMine => {
        await flushDebounceAsync();
        const filters: ProductListFiltersDialogResult = {
            onlyMine,
            productTypes: [ProductType.Fruit, ProductType.Vegetable],
            caloriesFrom: 20,
            caloriesTo: 150,
            hasImage: false,
        };
        dialogService.open.mockReturnValueOnce({ afterClosed: () => of(filters) });

        facade.openFilters();
        await flushDebounceAsync();

        expect(productService.query).toHaveBeenCalledExactlyOnceWith(
            1,
            PRODUCT_LIST_PAGE_SIZE,
            expect.objectContaining({
                productTypes: filters.productTypes,
                caloriesFrom: filters.caloriesFrom,
                caloriesTo: filters.caloriesTo,
                hasImage: filters.hasImage,
            }),
            expect.objectContaining({ includePublic: !onlyMine }),
        );
        expect(facade.showRecentSection()).toBe(false);
        expect(facade.activeFilterChips().map(chip => chip.id)).toEqual([
            ...(onlyMine ? ['onlyMine'] : []),
            'type:Fruit',
            'type:Vegetable',
            'caloriesFrom',
            'caloriesTo',
            'hasImage',
        ]);

        facade.openFilters();

        expect(dialogService.open).toHaveBeenLastCalledWith(expect.any(Function), { preset: 'form', data: filters });
    });
});

describe('ProductListFacade search', () => {
    it('keeps a mobile search visible with a query and closes an empty search after switching viewport', () => {
        isMobile.set(true);
        TestBed.tick();
        expect(facade.isMobileSearchVisible()).toBe(false);

        facade.toggleMobileSearch();
        expect(facade.isMobileSearchVisible()).toBe(true);
        facade.searchForm.search().value.set('pear');
        facade.toggleMobileSearch();
        expect(facade.isMobileSearchVisible()).toBe(true);
        facade.clearSearch();
        expect(facade.isMobileSearchVisible()).toBe(false);

        facade.toggleMobileSearch();
        isMobile.set(false);
        TestBed.tick();
        isMobile.set(true);
        TestBed.tick();
        expect(facade.isMobileSearchVisible()).toBe(false);
    });

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
                expect.objectContaining({ includePublic: true }),
            );
        });

        expect(openFoodFactsService.search).toHaveBeenCalledWith('banana', PRODUCT_LIST_OFF_SEARCH_LIMIT);
        expect(facade.searchValue()).toBe('banana');
        expect(facade.showRecentSection()).toBe(false);
        expect(facade.productData.items().map(product => product.id)).toEqual(['query-product']);
        expect(facade.offProducts()).toEqual([createOpenFoodFactsProduct()]);
    });

    it('reloads products without public items when only-mine filter is enabled', async () => {
        await flushDebounceAsync();
        productService.query.mockClear();

        facade.toggleOnlyMine();
        await flushDebounceAsync();

        expect(facade.onlyMineFilter()).toBe(true);
        expect(productService.query).toHaveBeenCalledWith(
            1,
            PRODUCT_LIST_PAGE_SIZE,
            expect.anything(),
            expect.objectContaining({ includePublic: false }),
        );
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

        await vi.waitFor(() => {
            expect(facade.offProducts()).toEqual([latest]);
            expect(facade.offLoading()).toBe(false);
        });
    });
});

describe('ProductListFacade favorites', () => {
    it('adds favorite product, syncs product state, and reloads favorites', () => {
        const product = createProduct({ id: entityId<'product'>('product-1'), isFavorite: false, favoriteProductId: null });
        facade.productData.setData(createPage([product]));

        facade.onProductFavoriteToggle(product);

        expect(favoriteProductService.add).toHaveBeenCalledWith('product-1', 'Test product', DEFAULT_PORTION_AMOUNT);
        expect(favoriteProductService.getPage).not.toHaveBeenCalled();
        expect(facade.productData.items()[0]).toEqual(expect.objectContaining({ isFavorite: true, favoriteProductId: 'favorite-1' }));
        expect(facade.favoriteLoadingIds().size).toBe(0);
    });

    it('removes favorite product by looking up favorite id when product state has no favorite id', () => {
        const product = createProduct({ id: entityId<'product'>('product-1'), isFavorite: true, favoriteProductId: null });
        facade.productData.setData(createPage([product]));

        facade.onProductFavoriteToggle(product);

        expect(favoriteProductService.getPage).not.toHaveBeenCalled();
        expect(favoriteProductService.remove).toHaveBeenCalledWith('favorite-1');
        expect(facade.productData.items()[0]).toEqual(expect.objectContaining({ isFavorite: false, favoriteProductId: null }));
        expect(facade.favoriteLoadingIds().size).toBe(0);
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
        id: entityId<'product'>('product-1'),
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
        id: entityId<'favorite-product'>('favorite-1'),
        productId: entityId<'product'>('product-1'),
        name: 'Test product',
        createdAtUtc: utcInstant('2026-01-01T00:00:00Z'),
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

describe('ProductListFacade request lifecycle', () => {
    it('cancels an obsolete overview when a search starts', () => {
        const old = new Subject<ProductOverview>();
        productService.queryOverview.mockReturnValue(old);
        translateService.onLangChange.next({ lang: 'en' });
        TestBed.tick();
        expect(old.observed).toBe(true);
        vi.useFakeTimers({ toFake: ['setInterval', 'clearInterval'] });
        onTestFinished(() => {
            vi.useRealTimers();
        });

        facade.searchForm.search().value.set('rice');
        TestBed.tick();
        expect(old.observed).toBe(true);
        vi.advanceTimersByTime(ZERO_DEBOUNCE_MS);
        TestBed.tick();

        expect(old.observed).toBe(false);
        expect(facade.productData.items()[0]?.id).toBe('query-product');
        expect(facade.productData.isLoading()).toBe(false);
    });
    it('preserves the star and releases the busy state when adding fails', () => {
        const product = createProduct({ isFavorite: false });
        facade.productData.setData(createPage([product]));
        favoriteProductService.add.mockReturnValue(throwError(() => new Error('offline')));
        facade.onProductFavoriteToggle(product);
        expect(facade.productData.items()[0].isFavorite).toBe(false);
        expect(facade.favoriteLoadingIds().size).toBe(0);
        expect(toastService.error).toHaveBeenCalledWith('PRODUCT_FAVORITES.ADD_ERROR');
    });
    it('opens favorites without fetching the full collection', () => {
        facade.openFavorites();
        expect(favoriteProductService.getLookupPage).not.toHaveBeenCalled();
        expect(favoriteProductService.getPage).not.toHaveBeenCalled();
        const options = dialogService.open.mock.calls[0][1] as {
            size: string;
            data: { repeat: unknown; remove: unknown; restore: unknown };
        };
        expect(options.size).toBe('md');
        expect(typeof options.data.repeat).toBe('function');
        expect(typeof options.data.remove).toBe('function');
        expect(typeof options.data.restore).toBe('function');
    });
});

describe('Product favorites picker callbacks', () => {
    function actions(): {
        repeat: (favorite: FavoriteProduct) => Observable<boolean>;
        remove: (favorite: FavoriteProduct) => Observable<boolean>;
        restore: (favorite: FavoriteProduct) => Observable<boolean>;
    } {
        facade.openFavorites();
        return (
            dialogService.open.mock.calls.at(-1)?.[1] as {
                data: {
                    repeat: (favorite: FavoriteProduct) => Observable<boolean>;
                    remove: (favorite: FavoriteProduct) => Observable<boolean>;
                    restore: (favorite: FavoriteProduct) => Observable<boolean>;
                };
            }
        ).data;
    }
    it('adds the current product with the saved portion amount', () => {
        const favorite = createFavoriteProduct({ preferredPortionAmount: DEFAULT_PORTION_AMOUNT });
        const product = createProduct();
        productService.getById.mockReturnValue(of(product));
        let result: boolean | undefined;
        actions()
            .repeat(favorite)
            .subscribe(value => {
                result = value;
            });
        expect(result).toBe(true);
        expect(quickMealService.addProduct).toHaveBeenCalledWith(product, DEFAULT_PORTION_AMOUNT);
    });
    it('does not add a product that is no longer accessible', () => {
        productService.getById.mockReturnValue(of(null));
        let result: boolean | undefined;
        actions()
            .repeat(createFavoriteProduct())
            .subscribe(value => {
                result = value;
            });
        expect(result).toBe(false);
        expect(quickMealService.addProduct).not.toHaveBeenCalled();
    });
    it('synchronizes the count and card and uses the new favorite id after undo', () => {
        const favorite = createFavoriteProduct();
        facade.favoriteTotalCount.set(1);
        facade.productData.setData(createPage([createProduct({ isFavorite: true, favoriteProductId: favorite.id })]));
        const callbacks = actions();
        callbacks.remove(favorite).subscribe();
        expect(facade.favoriteTotalCount()).toBe(0);
        expect(facade.productData.items()[0].isFavorite).toBe(false);
        favoriteProductService.add.mockReturnValue(of(createFavoriteProduct({ id: entityId<'favorite-product'>('restored-id') })));
        callbacks.restore(favorite).subscribe();
        expect(facade.favoriteTotalCount()).toBe(1);
        expect(favorite.id).toBe('restored-id');
        expect(facade.productData.items()[0].favoriteProductId).toBe('restored-id');
        callbacks.remove(favorite).subscribe();
        expect(favoriteProductService.remove).toHaveBeenLastCalledWith('restored-id');
    });
});

describe('recent product visibility', () => {
    it('shows recent products only on the default first page and reloads them on return', () => {
        expect(facade.showRecentSection()).toBe(true);
        facade.currentPageIndex = 1;
        expect(facade.showRecentSection()).toBe(false);
        facade.currentPageIndex = 0;
        facade.searchModel.set({ search: 'apple', onlyMine: false });
        expect(facade.showRecentSection()).toBe(false);
        facade.searchModel.set({ search: null, onlyMine: true });
        expect(facade.showRecentSection()).toBe(false);
        facade.searchModel.set({ search: null, onlyMine: false });
        facade.hasImageFilter.set(false);
        expect(facade.showRecentSection()).toBe(false);
        facade.hasImageFilter.set(null);
        facade.onPageChange(0);
        TestBed.tick();
        expect(productService.queryOverview).toHaveBeenCalledTimes(2);
        expect(facade.showRecentSection()).toBe(true);
        expect(facade.allProductItems()).toHaveLength(2);
    });
});
