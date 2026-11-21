import { ChangeDetectionStrategy, Component, type Provider, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import {
    type CanActivateFn,
    GuardsCheckEnd,
    type ParamMap,
    provideRouter,
    Router,
    type Routes,
    withComponentInputBinding,
} from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdTourService } from 'fd-tour';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { filter, firstValueFrom, type Observable, of, Subject, tap, throwError } from 'rxjs';
import { describe, expect, it, type Mock, onTestFinished, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../testing/async-testing';
import { provideTranslateTesting } from '../../../testing/translate-testing.module';
import { BarcodeScannerComponent } from '../../components/shared/barcode-scanner/barcode-scanner';
import { APP_SEARCH_DEBOUNCE_MS } from '../../config/runtime-ui.tokens';
import { AuthService } from '../../services/auth.service';
import { NavigationService } from '../../services/navigation.service';
import type { PageOf } from '../../shared/models/page-of.data';
import {
    type FavoriteProduct,
    MeasurementUnit,
    type Product,
    type ProductOverview,
    ProductType,
    ProductVisibility,
} from '../../shared/models/product.data';
import { utcInstant } from '../../shared/models/semantics/date-value';
import { entityId } from '../../shared/models/semantics/entity-id';
import { BrowserWindowService } from '../../shared/platform/browser-window.service';
import { ViewportService } from '../../shared/platform/viewport.service';
import { QuickMealService } from '../meals/contracts/quick-meal';
import { USDA_PRODUCT_LINK } from '../usda/contracts/usda-product-link';
import { FavoriteProductService } from './api/favorite-product.service';
import { OpenFoodFactsService } from './api/open-food-facts.service';
import { ProductService } from './api/product.service';
import { ProductDetailActionResult } from './components/detail/product-detail-lib/product-detail.types';
import { ProductAddDialogComponent } from './dialogs/product-add-dialog/product-add-dialog';
import { ProductFavoritesPickerComponent } from './dialogs/product-favorites-picker/product-favorites-picker';
import { ProductListDialogComponent } from './dialogs/product-list-dialog/product-list-dialog';
import productRoutes from './product.routes';

@Component({ template: '<h1>Other page</h1>', changeDetection: ChangeDetectionStrategy.OnPush })
class OtherPageComponent {}

const catalogRoutes: Routes = [
    { path: 'products', children: productRoutes },
    { path: 'other', component: OtherPageComponent },
];

@Component({
    selector: 'fd-test-independent-dialog',
    template: '<p>Independent dialog</p>',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
class IndependentDialogComponent {}

const PAGE_SIZE = 10;
const REQUESTED_PAGE = 5;
const LAST_AVAILABLE_PAGE = 2;
const CATALOG_ITEM = makeProduct('olive-oil', 'Olive oil');
const RECENT_ITEM = makeProduct('peanut-butter', 'Peanut butter');
const COFFEE_ITEM = makeProduct('filter-coffee', 'Filter coffee');
const SCANNED_BARCODE = '4006381333931';
const FILTER_MIN_CALORIES = 40;
const FILTER_MAX_CALORIES = 120;

type ProductMocks = {
    [Method in 'queryOverview' | 'query' | 'getById' | 'deleteById']: Mock<ProductService[Method]>;
};

type CatalogFixture = {
    products: ProductMocks;
    lookup: Mock<OpenFoodFactsService['search']>;
    navigateToEdit: Mock<NavigationService['navigateToProductEditAsync']>;
    navigateToAdd: Mock<NavigationService['navigateToProductAddAsync']>;
    navigateToProducts: Mock<NavigationService['navigateToProductListAsync']>;
    openDialog: Mock;
    closePicker: Mock<(result: Product | null) => void>;
    showError: Mock<FdUiToastService['error']>;
    removeFavorite: Mock<FavoriteProductService['remove']>;
    lookupFavorites: Mock<FavoriteProductService['getLookupPage']>;
    readFavorites: Mock<FavoriteProductService['getPage']>;
    addFavorite: Mock<FavoriteProductService['add']>;
    addToMeal: Mock<QuickMealService['addProduct']>;
};

function favoriteMocks(): Pick<CatalogFixture, 'removeFavorite' | 'addFavorite' | 'lookupFavorites' | 'readFavorites'> {
    return {
        removeFavorite: vi.fn<FavoriteProductService['remove']>(),
        addFavorite: vi.fn<FavoriteProductService['add']>(),
        lookupFavorites: vi.fn<FavoriteProductService['getLookupPage']>().mockReturnValue(of([])),
        readFavorites: vi
            .fn<FavoriteProductService['getPage']>()
            .mockReturnValue(of({ data: [], page: 1, limit: 1, totalItems: 0, totalPages: 1 })),
    };
}

function configureCatalog(realDialogs = false, queryGuard?: CanActivateFn): CatalogFixture {
    const products: ProductMocks = {
        queryOverview: vi
            .fn<ProductService['queryOverview']>()
            .mockReturnValue(
                of({ allProducts: page([CATALOG_ITEM]), recentItems: [RECENT_ITEM], favoriteItems: [], favoriteTotalCount: 1 }),
            ),
        query: vi.fn<ProductService['query']>().mockReturnValue(of(page([CATALOG_ITEM]))),
        getById: vi.fn<ProductService['getById']>().mockReturnValue(of(CATALOG_ITEM)),
        deleteById: vi.fn<ProductService['deleteById']>().mockReturnValue(of(void 0)),
    };
    const lookup = vi.fn<OpenFoodFactsService['search']>().mockReturnValue(of([]));
    const navigateToEdit = vi.fn<NavigationService['navigateToProductEditAsync']>().mockResolvedValue(undefined);
    const navigateToAdd = vi.fn<NavigationService['navigateToProductAddAsync']>().mockResolvedValue(undefined);
    const navigateToProducts = vi.fn<NavigationService['navigateToProductListAsync']>().mockImplementation(async () => {
        await TestBed.inject(Router).navigateByUrl('/products');
    });
    const openDialog = vi.fn().mockReturnValue({ afterClosed: () => of(null) });
    const closePicker = vi.fn<(result: Product | null) => void>();
    const showError = vi.fn<FdUiToastService['error']>();
    const favorites = favoriteMocks();
    const addToMeal = vi.fn<QuickMealService['addProduct']>();
    const routes: Routes =
        queryGuard === undefined
            ? catalogRoutes
            : [{ ...catalogRoutes[0], canActivate: [queryGuard], runGuardsAndResolvers: 'paramsOrQueryParamsChange' }, catalogRoutes[1]];
    TestBed.configureTestingModule({
        providers: [
            provideRouter(routes, withComponentInputBinding()),
            provideTranslateTesting(),
            ...boundaryProviders(addToMeal),
            { provide: ProductService, useValue: products },
            { provide: OpenFoodFactsService, useValue: { search: lookup } },
            {
                provide: NavigationService,
                useValue: {
                    navigateToProductEditAsync: navigateToEdit,
                    navigateToProductAddAsync: navigateToAdd,
                    navigateToProductListAsync: navigateToProducts,
                },
            },
            ...(realDialogs ? [] : [{ provide: FdUiDialogService, useValue: { open: openDialog } }]),
            { provide: FdUiDialogRef, useValue: { close: closePicker } },
            { provide: FdUiToastService, useValue: { error: showError } },
            { provide: AuthService, useValue: { isAuthenticated: signal(true) } },
            {
                provide: FavoriteProductService,
                useValue: {
                    getPage: favorites.readFavorites,
                    isFavorite: vi.fn<FavoriteProductService['isFavorite']>().mockReturnValue(of(false)),
                    remove: favorites.removeFavorite,
                    getLookupPage: favorites.lookupFavorites,
                    add: favorites.addFavorite,
                },
            },
        ],
    });
    const translate = TestBed.inject(TranslateService);
    translate.setFallbackLang('en');
    translate.use('en');
    return {
        products,
        lookup,
        navigateToEdit,
        navigateToAdd,
        navigateToProducts,
        openDialog,
        closePicker,
        showError,
        addToMeal,
        ...favorites,
    };
}

function boundaryProviders(addToMeal: QuickMealService['addProduct']): Provider[] {
    return [
        { provide: APP_SEARCH_DEBOUNCE_MS, useValue: 0 },
        { provide: ViewportService, useValue: { isMobile: signal(false) } },
        { provide: QuickMealService, useValue: { addProduct: addToMeal } },
        { provide: USDA_PRODUCT_LINK, useValue: { getFoodDetail: vi.fn(), linkProduct: vi.fn(), unlinkProduct: vi.fn() } },
    ];
}

function makeProduct(id: string, name: string): Product {
    return {
        id: entityId<'product'>(id),
        name,
        baseUnit: MeasurementUnit.G,
        productType: ProductType.Other,
        baseAmount: 100,
        defaultPortionAmount: 100,
        caloriesPerBase: 120,
        proteinsPerBase: 2,
        fatsPerBase: 6,
        carbsPerBase: 14,
        fiberPerBase: 0,
        alcoholPerBase: 0,
        usageCount: 1,
        visibility: ProductVisibility.Private,
        createdAt: new Date('2026-05-01T00:00:00Z'),
        isOwnedByCurrentUser: true,
        qualityScore: 80,
        qualityGrade: 'green',
    };
}

function favoriteFor(product: Product): FavoriteProduct {
    return {
        ...product,
        id: entityId<'favorite-product'>(`favorite-${product.id}`),
        productId: product.id,
        name: null,
        productName: product.name,
        createdAtUtc: utcInstant('2026-05-01T00:00:00Z'),
        preferredPortionAmount: 150,
    };
}

function page(data: Product[], number = 1, totalPages = 1): PageOf<Product> {
    return { data, page: number, totalPages, limit: PAGE_SIZE, totalItems: (totalPages - 1) * PAGE_SIZE + data.length };
}

function catalogRoot(harness: RouterTestingHarness): HTMLElement {
    const host = harness.routeNativeElement;
    if (host === null) {
        throw new Error('Expected the catalog route to be rendered');
    }
    return host;
}

function button(host: HTMLElement, selector: string): HTMLButtonElement {
    const found = host.querySelector(selector);
    if (!(found instanceof HTMLButtonElement)) {
        throw new TypeError(`Expected a catalog button matching ${selector}`);
    }
    return found;
}

function searchInput(harness: RouterTestingHarness): HTMLInputElement {
    const found = catalogRoot(harness).querySelector('.product-list__desktop-search input');
    if (!(found instanceof HTMLInputElement)) {
        throw new TypeError('Expected the catalog search input');
    }
    return found;
}

async function waitForCatalogAsync(harness: RouterTestingHarness, text: string): Promise<void> {
    await vi.waitFor(() => {
        harness.detectChanges();
        expect(catalogRoot(harness).textContent).toContain(text);
    });
}

it('reopens a completed tour from the catalogue help action and highlights the toolbar again', async () => {
    configureCatalog();
    const harness = await RouterTestingHarness.create('/products');
    await waitForCatalogAsync(harness, CATALOG_ITEM.name);
    const tour = TestBed.inject(FdTourService);
    onTestFinished(() => {
        tour.close();
    });
    const help = button(catalogRoot(harness), 'button[aria-label="PRODUCT_LIST.TOUR.START_TOUR"]');

    help.click();
    harness.detectChanges();
    expect(tour.activeTour()?.id).toBe('product-list');
    expect(tour.target()?.getAttribute('data-tour-id')).toBe('product-list-actions');
    tour.complete();
    expect(tour.activeTour()).toBeNull();

    help.click();
    harness.detectChanges();
    expect(tour.activeStep()?.id).toBe('actions');
    expect(tour.target()?.getAttribute('data-tour-id')).toBe('product-list-actions');
});

describe('Routed product catalog state', () => {
    it('opens with URL filters and refreshes the unfiltered first page when the user clears them', async () => {
        const { products } = configureCatalog();
        const harness = await RouterTestingHarness.create('/products?search=olive&onlyMine=true&campaign=summer');
        const router = TestBed.inject(Router);
        await waitForCatalogAsync(harness, 'PRODUCT_LIST.SEARCH_RESULTS');
        const host = catalogRoot(harness);
        expect(searchInput(harness).value).toBe('olive');
        expect(host.querySelector('.active-filters__chip')).not.toBeNull();
        expect(host.textContent).not.toContain(RECENT_ITEM.name);
        expect(products.queryOverview.mock.calls[0][0].includePublic).toBe(false);

        button(host, '.active-filters__reset button').click();
        await vi.waitFor(() => {
            expect(router.parseUrl(router.url).queryParams).toEqual({ search: 'olive', campaign: 'summer' });
        });
        button(host, 'button[aria-label="PRODUCT_LIST.SEARCH_CLEAR"]').click();
        await vi.waitFor(() => {
            expect(router.parseUrl(router.url).queryParams).toEqual({ campaign: 'summer' });
        });
        await waitForCatalogAsync(harness, RECENT_ITEM.name);

        expect(searchInput(harness).value).toBe('');
        expect(host.querySelector('.active-filters__chip')).toBeNull();
        expect(host.textContent).toContain('PRODUCT_LIST.ALL_PRODUCTS');
        expect(products.queryOverview.mock.lastCall?.[0].includePublic).toBe(true);
    });

    it('replaces an out-of-range page with the server’s last page and renders only that page', async () => {
        const { products } = configureCatalog();
        products.queryOverview.mockReturnValueOnce(
            of({
                allProducts: page([], REQUESTED_PAGE, LAST_AVAILABLE_PAGE),
                recentItems: [RECENT_ITEM],
                favoriteItems: [],
                favoriteTotalCount: 0,
            }),
        );
        products.query.mockReturnValueOnce(of(page([COFFEE_ITEM], LAST_AVAILABLE_PAGE, LAST_AVAILABLE_PAGE)));
        const router = TestBed.inject(Router);
        const navigate = vi.spyOn(router, 'navigate');
        const harness = await RouterTestingHarness.create(`/products?page=${REQUESTED_PAGE}&campaign=summer`);
        await waitForCatalogAsync(harness, COFFEE_ITEM.name);

        expect(router.parseUrl(router.url).queryParams).toEqual({ page: String(LAST_AVAILABLE_PAGE), campaign: 'summer' });
        expect(navigate).toHaveBeenCalledWith([], expect.objectContaining({ replaceUrl: true, queryParamsHandling: 'merge' }));
        expect(products.query.mock.calls[0][0]).toBe(LAST_AVAILABLE_PAGE);
        expect(catalogRoot(harness).textContent).not.toContain(RECENT_ITEM.name);
    });
});

describe('Routed catalog pagination', () => {
    it('scrolls the catalog header into view and shows the selected page', async () => {
        const { products } = configureRealCatalogDialogs();
        products.queryOverview.mockReturnValueOnce(
            of({
                allProducts: page([CATALOG_ITEM], 1, LAST_AVAILABLE_PAGE),
                recentItems: [],
                favoriteItems: [],
                favoriteTotalCount: 0,
            }),
        );
        products.query.mockReturnValueOnce(of(page([COFFEE_ITEM], LAST_AVAILABLE_PAGE, LAST_AVAILABLE_PAGE)));
        const harness = await RouterTestingHarness.create('/products');
        const host = catalogRoot(harness);
        const header = host.querySelector('fd-page-header');
        const pagination = host.querySelector('fd-ui-pagination');
        if (!(header instanceof HTMLElement) || !(pagination instanceof HTMLElement)) {
            throw new Error('Expected a catalog header and pagination');
        }
        const scroll = vi.fn<HTMLElement['scrollIntoView']>();
        Object.defineProperty(header, 'scrollIntoView', { value: scroll });

        dialogButton(pagination, String(LAST_AVAILABLE_PAGE)).click();
        await waitForCatalogAsync(harness, COFFEE_ITEM.name);

        expect(scroll).toHaveBeenCalledExactlyOnceWith({ behavior: 'smooth', block: 'start' });
        expect(products.query).toHaveBeenCalledWith(LAST_AVAILABLE_PAGE, PAGE_SIZE, expect.any(Object), expect.any(Object));
        expect(host.textContent).not.toContain(CATALOG_ITEM.name);
        const router = TestBed.inject(Router);
        expect(router.parseUrl(router.url).queryParams).toEqual({ page: String(LAST_AVAILABLE_PAGE) });
    });
});

describe('Routed catalog quick meal actions', () => {
    it('adds a recent product to the meal draft from its separate add action', async () => {
        const { addToMeal, openDialog } = configureCatalog();
        const harness = await RouterTestingHarness.create('/products');
        const host = catalogRoot(harness);
        expect(host.querySelector('.recent-products__open')?.textContent).toContain(RECENT_ITEM.name);

        button(host, '.recent-products__item fd-ui-button button').click();

        expect(addToMeal).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ id: RECENT_ITEM.id, name: RECENT_ITEM.name }));
        expect(openDialog).not.toHaveBeenCalled();
    });
});

describe('Routed catalog advanced filters', () => {
    it('keeps an invalid range local and updates the URL only when the corrected filters are applied', async () => {
        vi.stubGlobal('matchMedia', vi.fn().mockReturnValue({ matches: false }));
        configureCatalog(true);
        onTestFinished(() => {
            vi.unstubAllGlobals();
        });
        const router = TestBed.inject(Router);
        const harness = await RouterTestingHarness.create('/products?search=olive&campaign=summer');
        button(catalogRoot(harness), 'button[aria-label="PRODUCT_LIST.FILTERS_BUTTON"]').click();
        TestBed.tick();
        const dialog = document.querySelector('fd-product-list-filters-dialog');
        if (!(dialog instanceof HTMLElement)) {
            throw new Error('Product filters dialog did not open');
        }
        button(dialog, 'button[aria-label="PRODUCT_MANAGE.PRODUCT_TYPE_OPTIONS.FRUIT"]').click();
        dialogButton(dialog, 'PRODUCT_LIST.FILTER_MY_PRODUCTS').click();
        dialogButton(dialog, 'PRODUCT_LIST.FILTER_IMAGE_WITH').click();
        setFilterNumber(dialog, 'PRODUCT_LIST.FILTER_CALORIES_FROM', FILTER_MAX_CALORIES);
        setFilterNumber(dialog, 'PRODUCT_LIST.FILTER_CALORIES_TO', FILTER_MIN_CALORIES);
        dialogButton(dialog, 'PRODUCT_LIST.FILTER_APPLY_BUTTON').click();
        TestBed.tick();

        expect(dialog.textContent).toContain('PRODUCT_LIST.FILTER_CALORIES_ORDER');
        expect(router.parseUrl(router.url).queryParams).toEqual({ search: 'olive', campaign: 'summer' });
        setFilterNumber(dialog, 'PRODUCT_LIST.FILTER_CALORIES_FROM', FILTER_MIN_CALORIES);
        setFilterNumber(dialog, 'PRODUCT_LIST.FILTER_CALORIES_TO', FILTER_MAX_CALORIES);
        dialogButton(dialog, 'PRODUCT_LIST.FILTER_APPLY_BUTTON').click();
        await vi.waitFor(() => {
            expect(router.parseUrl(router.url).queryParams).toEqual({
                search: 'olive',
                campaign: 'summer',
                onlyMine: 'true',
                types: ProductType.Fruit,
                caloriesFrom: String(FILTER_MIN_CALORIES),
                caloriesTo: String(FILTER_MAX_CALORIES),
                hasImage: 'true',
            });
        });
        expect(dialog.isConnected).toBe(false);
    });
});

function dialogButton(dialog: HTMLElement, label: string): HTMLButtonElement {
    const action = Array.from(dialog.querySelectorAll('button')).find(candidate => {
        const labelText = candidate.querySelector('.fd-ui-button__label')?.textContent ?? candidate.textContent;
        return labelText.trim() === label;
    });
    if (action === undefined) {
        throw new Error(`Expected a product filter action with label ${label}`);
    }
    return action;
}

describe('Routed catalog filter ownership', () => {
    it('closes the drafted filters on route departure and leaves an unrelated dialog open', async () => {
        const { products } = configureRealCatalogDialogs();
        const route = '/products?search=olive&campaign=summer';
        const harness = await RouterTestingHarness.create(route);
        await waitForCatalogAsync(harness, CATALOG_ITEM.name);
        products.query.mockClear();
        const filters = openCatalogFilters(harness);
        setFilterNumber(filters, 'PRODUCT_LIST.FILTER_CALORIES_FROM', FILTER_MIN_CALORIES);
        harness.detectChanges();
        const independent = TestBed.inject(FdUiDialogService).open(IndependentDialogComponent, { closeOnNavigation: false });
        expect(document.querySelector('fd-test-independent-dialog')?.textContent).toContain('Independent dialog');

        await harness.navigateByUrl('/other', OtherPageComponent);
        expect(catalogRoot(harness).textContent).toContain('Other page');
        expect(filters.isConnected).toBe(false);
        expect(document.querySelector('fd-test-independent-dialog')).not.toBeNull();
        expect(products.query).not.toHaveBeenCalled();

        independent.close();
        await harness.navigateByUrl(route);
        const reopened = openCatalogFilters(harness);
        expect(reopened.querySelector<HTMLInputElement>('input[aria-label="PRODUCT_LIST.FILTER_CALORIES_FROM"]')?.value).toBe('');
    });

    it('does not fetch on an unchanged apply and keeps existing filters when switching catalog ownership', async () => {
        const { products } = configureRealCatalogDialogs();
        const router = TestBed.inject(Router);
        const filters = { search: 'olive', types: ProductType.Fruit, caloriesFrom: String(FILTER_MIN_CALORIES), hasImage: 'false' };
        const url = router.createUrlTree(['/products'], { queryParams: filters });
        const harness = await RouterTestingHarness.create(router.serializeUrl(url));
        await waitForCatalogAsync(harness, CATALOG_ITEM.name);
        products.query.mockClear();

        const unchanged = openCatalogFilters(harness);
        dialogButton(unchanged, 'PRODUCT_LIST.FILTER_APPLY_BUTTON').click();
        TestBed.tick();
        expect(unchanged.isConnected).toBe(false);
        expect(router.parseUrl(router.url).queryParams).toEqual(filters);
        expect(products.query).not.toHaveBeenCalled();

        const changed = openCatalogFilters(harness);
        dialogButton(changed, 'PRODUCT_LIST.FILTER_MY_PRODUCTS').click();
        dialogButton(changed, 'PRODUCT_LIST.FILTER_APPLY_BUTTON').click();
        await vi.waitFor(() => {
            expect(router.parseUrl(router.url).queryParams).toEqual({ ...filters, onlyMine: 'true' });
            expect(products.query).toHaveBeenCalledExactlyOnceWith(
                1,
                PAGE_SIZE,
                expect.objectContaining({ productTypes: [ProductType.Fruit], caloriesFrom: FILTER_MIN_CALORIES, hasImage: false }),
                expect.objectContaining({ includePublic: false }),
            );
        });
    });
});

function configureRealCatalogDialogs(): CatalogFixture {
    vi.stubGlobal('matchMedia', vi.fn().mockReturnValue({ matches: false }));
    const fixture = configureCatalog(true);
    const dialogs = TestBed.inject(FdUiDialogService);
    onTestFinished(() => {
        dialogs.closeAll();
        vi.unstubAllGlobals();
    });
    return fixture;
}

function openCatalogFilters(harness: RouterTestingHarness): HTMLElement {
    button(catalogRoot(harness), 'button[aria-label="PRODUCT_LIST.FILTERS_BUTTON"]').click();
    TestBed.tick();
    const dialog = document.querySelector('fd-product-list-filters-dialog');
    if (!(dialog instanceof HTMLElement)) {
        throw new Error('Expected the current catalog filters');
    }
    return dialog;
}

function favoriteCount(host: HTMLElement): string | undefined {
    return host.querySelector('.product-list__favorites-trigger .fd-ui-button__label')?.textContent.trim();
}

function setFilterNumber(dialog: HTMLElement, label: string, value: number): void {
    const control = dialog.querySelector(`input[aria-label="${label}"]`);
    if (!(control instanceof HTMLInputElement)) {
        throw new Error(`Expected a product filter control with label ${label}`);
    }
    control.value = String(value);
    control.dispatchEvent(new Event('input', { bubbles: true }));
}

describe('Routed catalog barcode scanner', () => {
    it('keeps a cancelled scan from changing the search and applies a successful scan to the same route', async () => {
        const { openDialog, products } = configureCatalog();
        openDialog.mockReturnValueOnce({ afterClosed: () => of(null) }).mockReturnValueOnce({ afterClosed: () => of(SCANNED_BARCODE) });
        const harness = await RouterTestingHarness.create('/products?search=olive&campaign=summer');
        const router = TestBed.inject(Router);
        const scan = button(catalogRoot(harness), 'button[aria-label="PRODUCT_LIST.SCAN_BARCODE"]');
        scan.click();
        expect(searchInput(harness).value).toBe('olive');
        expect(products.query).not.toHaveBeenCalled();
        expect(openDialog).toHaveBeenCalledExactlyOnceWith(BarcodeScannerComponent, { size: 'lg' });

        scan.click();
        await vi.waitFor(() => {
            expect(router.parseUrl(router.url).queryParams).toEqual({ search: SCANNED_BARCODE, campaign: 'summer' });
            expect(products.query).toHaveBeenCalledExactlyOnceWith(
                1,
                PAGE_SIZE,
                expect.objectContaining({ search: SCANNED_BARCODE }),
                expect.any(Object),
            );
        });
        expect(searchInput(harness).value).toBe(SCANNED_BARCODE);
    });

    it('releases a pending scan when the catalog route is left', async () => {
        const { openDialog, products } = configureCatalog();
        const closed = new Subject<string | null>();
        const close = vi.fn<() => void>();
        openDialog.mockReturnValueOnce({ afterClosed: () => closed, close });
        const harness = await RouterTestingHarness.create('/products');
        button(catalogRoot(harness), 'button[aria-label="PRODUCT_LIST.SCAN_BARCODE"]').click();
        expect(closed.observed).toBe(true);

        await harness.navigateByUrl('/other');

        expect(closed.observed).toBe(false);
        expect(close).toHaveBeenCalledOnce();
        closed.next(SCANNED_BARCODE);
        closed.complete();
        expect(products.query).not.toHaveBeenCalled();
        expect(catalogRoot(harness).textContent).toContain('Other page');
    });
});

describe('Routed catalog favorite removal', () => {
    it('preserves the starred card and count after failure, then removes it by its stored favorite id on retry', async () => {
        const { products, removeFavorite, lookupFavorites, showError } = configureCatalog();
        const favoriteId = entityId<'favorite-product'>('favorite-olive-oil');
        const starred = { ...CATALOG_ITEM, isFavorite: true, favoriteProductId: favoriteId };
        products.queryOverview.mockReturnValueOnce(
            of({ allProducts: page([starred, COFFEE_ITEM]), recentItems: [RECENT_ITEM], favoriteItems: [], favoriteTotalCount: 1 }),
        );
        const request = new Subject<void>();
        removeFavorite.mockReturnValueOnce(request).mockReturnValueOnce(of(void 0));
        const harness = await RouterTestingHarness.create('/products');
        const host = catalogRoot(harness);
        const remove = button(host, 'button[aria-label="PRODUCT_DETAIL.REMOVE_FAVORITE"]');
        const count = button(host, '.product-list__favorites-trigger button');

        remove.click();
        harness.detectChanges();
        expect(remove.disabled).toBe(true);
        expect(count.textContent).toContain('1');
        request.error(new Error('Favorite removal unavailable'));
        harness.detectChanges();
        expect(showError).toHaveBeenCalledExactlyOnceWith('PRODUCT_FAVORITES.REMOVE_ERROR');
        expect(remove.disabled).toBe(false);
        expect(count.textContent).toContain('1');

        remove.click();
        harness.detectChanges();
        expect(removeFavorite).toHaveBeenCalledTimes(2);
        expect(removeFavorite).toHaveBeenLastCalledWith(favoriteId);
        expect(lookupFavorites).not.toHaveBeenCalled();
        expect(host.querySelector('button[aria-label="PRODUCT_DETAIL.REMOVE_FAVORITE"]')).toBeNull();
        expect(count.textContent).toContain('0');
        expect(host.textContent).toContain(COFFEE_ITEM.name);
    });
});

describe('Routed catalog stale and pending favorite state', () => {
    it('clears a stale star without subtracting another saved product from the count', async () => {
        const { products, lookupFavorites, removeFavorite, showError } = configureCatalog();
        const stale: Product = {
            ...CATALOG_ITEM,
            isFavorite: true,
            favoriteProductId: null,
        };
        const saved = favoriteFor(COFFEE_ITEM);
        products.queryOverview.mockReturnValueOnce(
            of({
                allProducts: page([
                    stale,
                    {
                        ...COFFEE_ITEM,
                        isFavorite: true,
                        favoriteProductId: saved.id,
                    },
                ]),
                recentItems: [stale],
                favoriteItems: [saved],
                favoriteTotalCount: 1,
            }),
        );
        lookupFavorites.mockReturnValueOnce(of([saved]));
        const harness = await RouterTestingHarness.create('/products');
        const host = catalogRoot(harness);

        button(host, 'fd-product-card button[aria-label="PRODUCT_DETAIL.REMOVE_FAVORITE"]').click();
        harness.detectChanges();

        expect(lookupFavorites).toHaveBeenCalledOnce();
        expect(removeFavorite).not.toHaveBeenCalled();
        expect(host.querySelectorAll('fd-product-card button[aria-label="PRODUCT_DETAIL.REMOVE_FAVORITE"]')).toHaveLength(1);
        expect(favoriteCount(host)).toBe('1');
        expect(showError).not.toHaveBeenCalled();
    });

    it('starts only one removal when the same star is activated twice before rendering', async () => {
        const { products, removeFavorite } = configureCatalog();
        const favorite = favoriteFor(CATALOG_ITEM);
        const starred: Product = {
            ...CATALOG_ITEM,
            isFavorite: true,
            favoriteProductId: favorite.id,
        };
        products.queryOverview.mockReturnValueOnce(
            of({
                allProducts: page([starred]),
                recentItems: [starred],
                favoriteItems: [favorite],
                favoriteTotalCount: 1,
            }),
        );
        const request = new Subject<void>();
        removeFavorite.mockReturnValueOnce(request);
        const harness = await RouterTestingHarness.create('/products');
        const host = catalogRoot(harness);
        const star = button(host, 'button[aria-label="PRODUCT_DETAIL.REMOVE_FAVORITE"]');

        star.click();
        star.click();
        expect(removeFavorite).toHaveBeenCalledExactlyOnceWith(favorite.id);
        harness.detectChanges();
        expect(star.disabled).toBe(true);
        request.next();
        request.complete();
        harness.detectChanges();

        expect(host.querySelector('button[aria-label="PRODUCT_DETAIL.REMOVE_FAVORITE"]')).toBeNull();
        expect(favoriteCount(host)).toBe('0');
    });
});

function openFavoritesPicker(harness: RouterTestingHarness): HTMLElement {
    button(catalogRoot(harness), '.product-list__favorites-trigger button').click();
    TestBed.tick();
    const picker = document.querySelector('fd-product-favorites-picker');
    if (!(picker instanceof HTMLElement)) {
        throw new TypeError('Expected the favorites dialog to open');
    }
    return picker;
}

function configureFavoritesPicker(): CatalogFixture {
    vi.stubGlobal('matchMedia', vi.fn().mockReturnValue({ matches: false }));
    onTestFinished(() => {
        vi.unstubAllGlobals();
    });
    const catalog = configureCatalog(true);
    const favorite = favoriteFor(CATALOG_ITEM);
    catalog.readFavorites.mockReturnValue(
        of({
            data: [favorite],
            page: 1,
            limit: PAGE_SIZE,
            totalPages: 1,
            totalItems: 1,
        }),
    );
    return catalog;
}

describe('Rendered catalog favorites picker', () => {
    it('restores a favorite with no custom name and uses the restored id on the next removal', async () => {
        const { products, removeFavorite, addFavorite } = configureFavoritesPicker();
        const favorite = favoriteFor(CATALOG_ITEM);
        const restored = {
            ...favorite,
            id: entityId<'favorite-product'>('restored-olive-oil'),
        };
        const starred: Product = {
            ...CATALOG_ITEM,
            isFavorite: true,
            favoriteProductId: favorite.id,
        };
        products.queryOverview.mockReturnValueOnce(
            of({
                allProducts: page([starred]),
                recentItems: [starred],
                favoriteItems: [favorite],
                favoriteTotalCount: 1,
            }),
        );
        removeFavorite.mockReturnValue(of(void 0));
        addFavorite.mockReturnValue(of(restored));
        const harness = await RouterTestingHarness.create('/products');
        const host = catalogRoot(harness);
        const picker = openFavoritesPicker(harness);
        const removeSelector = 'button[aria-label="PRODUCT_DETAIL.REMOVE_FAVORITE"]';

        button(picker, removeSelector).click();
        TestBed.tick();
        expect(removeFavorite).toHaveBeenCalledExactlyOnceWith(favorite.id);
        expect(favoriteCount(host)).toBe('0');
        expect(host.querySelector(removeSelector)).toBeNull();

        dialogButton(picker, 'PRODUCT_FAVORITES.UNDO').click();
        TestBed.tick();
        expect(addFavorite).toHaveBeenCalledExactlyOnceWith(favorite.productId, undefined, favorite.preferredPortionAmount);
        expect(favoriteCount(host)).toBe('1');
        expect(host.querySelector(removeSelector)).not.toBeNull();

        button(picker, removeSelector).click();
        expect(removeFavorite).toHaveBeenLastCalledWith(restored.id);
    });

    it('keeps an inaccessible favorite in the picker and uses the current product when access returns', async () => {
        const { products, addToMeal } = configureFavoritesPicker();
        const harness = await RouterTestingHarness.create('/products');
        const picker = openFavoritesPicker(harness);
        const currentProduct: Product = {
            ...CATALOG_ITEM,
            name: 'Olive oil with updated packaging',
            imageUrl: '/current-front.jpg',
            images: [
                { imageAssetId: null, imageUrl: '/current-front.jpg' },
                { imageAssetId: null, imageUrl: '/current-back.jpg' },
            ],
        };
        products.getById.mockReturnValueOnce(of(null)).mockReturnValueOnce(of(currentProduct));

        dialogButton(picker, 'PRODUCT_FAVORITES.ADD').click();
        TestBed.tick();
        expect(picker.querySelector('[role="alert"]')?.textContent.trim()).toBe('PRODUCT_FAVORITES.ADD_ERROR');
        expect(addToMeal).not.toHaveBeenCalled();

        dialogButton(picker, 'PRODUCT_FAVORITES.ADD').click();
        TestBed.tick();
        expect(products.getById).toHaveBeenCalledTimes(2);
        expect(addToMeal).toHaveBeenCalledExactlyOnceWith(currentProduct, favoriteFor(CATALOG_ITEM).preferredPortionAmount);
        expect(picker.isConnected).toBe(false);
    });
});

describe('Catalog favorites picker ownership', () => {
    it('closes the picker and cancels a pending add when the user leaves the catalog', async () => {
        const { products, addToMeal } = configureFavoritesPicker();
        const pending = new Subject<Product | null>();
        products.getById.mockReturnValueOnce(pending);
        const harness = await RouterTestingHarness.create('/products');
        const picker = openFavoritesPicker(harness);

        try {
            dialogButton(picker, 'PRODUCT_FAVORITES.ADD').click();
            expect(pending.observed).toBe(true);

            await harness.navigateByUrl('/other');
            expect(catalogRoot(harness).textContent).toContain('Other page');
            await vi.waitFor(() => {
                expect(picker.isConnected).toBe(false);
            });
            expect(pending.observed).toBe(false);
            pending.next(CATALOG_ITEM);
            pending.complete();
            expect(addToMeal).not.toHaveBeenCalled();
        } finally {
            TestBed.inject(FdUiDialogService).closeAll();
        }
    });
});

describe('Catalog favorite load recovery', () => {
    it('keeps the current header count when the detail refresh fails and restores it on the next refresh', async () => {
        const { openDialog, products, readFavorites, showError } = configureCatalog();
        const afterClosed = (): Observable<ProductDetailActionResult> =>
            of(new ProductDetailActionResult(RECENT_ITEM.id, 'FavoriteChanged'));
        openDialog.mockReturnValue({ afterClosed });
        readFavorites.mockReturnValueOnce(throwError(() => new Error('Favorites temporarily unavailable')));
        const harness = await RouterTestingHarness.create('/products');
        const host = catalogRoot(harness);
        const detailButton = button(host, '.recent-products__open');

        detailButton.click();
        await vi.waitFor(() => {
            expect(showError).toHaveBeenCalledExactlyOnceWith('PRODUCT_FAVORITES.LOAD_ERROR');
            expect(readFavorites).toHaveBeenCalledExactlyOnceWith(1, 1);
        });
        harness.detectChanges();
        expect(favoriteCount(host)).toBe('1');
        expect(host.textContent).toContain(CATALOG_ITEM.name);

        products.queryOverview.mockReturnValueOnce(
            of({ allProducts: page([CATALOG_ITEM]), recentItems: [RECENT_ITEM], favoriteItems: [], favoriteTotalCount: 0 }),
        );
        detailButton.click();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(favoriteCount(host)).toBe('0');
        });

        expect(readFavorites.mock.calls).toEqual([
            [1, 1],
            [1, 1],
        ]);
        const expectedOverviewLoads = 3;
        expect(products.queryOverview).toHaveBeenCalledTimes(expectedOverviewLoads);
        expect(showError).toHaveBeenCalledOnce();
    });

    it('retries a failed favorites request from its alert and reports a subsequent empty search', async () => {
        const { readFavorites } = configureFavoritesPicker();
        const initial = new Subject<PageOf<FavoriteProduct>>();
        readFavorites.mockReturnValueOnce(initial);
        const harness = await RouterTestingHarness.create('/products');
        const picker = openFavoritesPicker(harness);
        expect(picker.textContent).toContain('PRODUCT_FAVORITES.LOADING');
        initial.error(new Error('Favorites temporarily unavailable'));
        TestBed.tick();

        expect(picker.querySelector('[role="alert"]')?.textContent.trim()).toBe('PRODUCT_FAVORITES.LOAD_ERROR');
        expect(picker.querySelector('fd-favorite-product-row')).toBeNull();
        dialogButton(picker, 'PRODUCT_FAVORITES.RETRY').click();
        TestBed.tick();

        expect(readFavorites.mock.calls).toEqual([
            [1, PAGE_SIZE, ''],
            [1, PAGE_SIZE, ''],
        ]);
        expect(picker.querySelector('[role="alert"]')).toBeNull();
        expect(picker.querySelector('fd-favorite-product-row')?.textContent).toContain(CATALOG_ITEM.name);
        const input = picker.querySelector<HTMLInputElement>('input');
        if (input === null) {
            throw new Error('Expected the favorites search input');
        }
        readFavorites.mockReturnValueOnce(of({ data: [], page: 1, limit: PAGE_SIZE, totalItems: 0, totalPages: 1 }));
        input.value = '  missing product  ';
        input.dispatchEvent(new Event('input', { bubbles: true }));
        await vi.waitFor(() => {
            TestBed.tick();
            expect(picker.textContent).toContain('PRODUCT_FAVORITES.NO_RESULTS');
        });

        expect(readFavorites).toHaveBeenLastCalledWith(1, PAGE_SIZE, 'missing product');
        expect(picker.querySelector('fd-favorite-product-row')).toBeNull();
        expect(picker.querySelector('[role="alert"]')).toBeNull();
    });
});

describe('Catalog favorite row presentation', () => {
    it('keeps the saved name after a failed photo and leaves an unsuccessful undo available for retry', async () => {
        const { readFavorites, removeFavorite, addFavorite } = configureFavoritesPicker();
        const favorite = { ...favoriteFor(CATALOG_ITEM), name: '  Pantry oil  ', imageUrl: '/saved-oil.jpg' };
        readFavorites.mockReturnValue(of({ data: [favorite], page: 1, limit: PAGE_SIZE, totalItems: 1, totalPages: 1 }));
        removeFavorite.mockReturnValue(of(void 0));
        addFavorite.mockReturnValueOnce(throwError(() => new Error('Restore temporarily unavailable'))).mockReturnValue(of(favorite));
        const harness = await RouterTestingHarness.create('/products');
        const picker = openFavoritesPicker(harness);
        const row = picker.querySelector('fd-favorite-product-row');
        const image = row?.querySelector('img');
        if (row === null || image === undefined || image === null) {
            throw new Error('Expected a favorites row with its saved photo');
        }
        expect(row.querySelector('strong')?.textContent.trim()).toBe('Pantry oil');
        expect(image.getAttribute('src')).toBe('/saved-oil.jpg');

        image.dispatchEvent(new Event('error'));
        TestBed.tick();

        expect(row.querySelector('img')).toBeNull();
        expect(row.querySelector('.favorite-row__media fd-ui-icon')).not.toBeNull();
        expect(row.querySelector('strong')?.textContent.trim()).toBe('Pantry oil');

        button(picker, 'button[aria-label="PRODUCT_DETAIL.REMOVE_FAVORITE"]').click();
        TestBed.tick();
        dialogButton(picker, 'PRODUCT_FAVORITES.UNDO').click();
        TestBed.tick();

        const status = row.querySelector('[role="status"]')?.textContent;
        expect(status).toContain('PRODUCT_FAVORITES.RESTORE_ERROR');
        expect(status).toContain('Pantry oil');
        dialogButton(picker, 'PRODUCT_FAVORITES.UNDO').click();
        TestBed.tick();

        expect(addFavorite).toHaveBeenCalledTimes(2);
        expect(row.querySelector('[role="status"]')).toBeNull();
        expect(row.querySelector('strong')?.textContent.trim()).toBe('Pantry oil');
    });
});

describe('Catalog favorite undo pagination', () => {
    it('still reaches the last page when undo restores a favorite under a new ID', async () => {
        const { readFavorites, removeFavorite, addFavorite } = configureFavoritesPicker();
        const lastPage = 3;
        const favorites = Array.from({ length: PAGE_SIZE * 2 + 1 }, (_, index) =>
            favoriteFor(makeProduct(`saved-${index + 1}`, `Saved product ${index + 1}`)),
        );
        const firstOnSecondPage = favorites[PAGE_SIZE];
        const originalId = firstOnSecondPage.id;
        const replacement = { ...firstOnSecondPage, id: entityId<'favorite-product'>('restored-page-two-favorite') };
        readFavorites.mockImplementation((current = 1, limit = PAGE_SIZE) => {
            const offset = (current - 1) * limit;
            return of({
                data: favorites.slice(offset, offset + limit),
                page: current,
                limit,
                totalItems: favorites.length,
                totalPages: lastPage,
            });
        });
        removeFavorite.mockReturnValue(of(void 0));
        addFavorite.mockReturnValue(of(replacement));
        const harness = await RouterTestingHarness.create('/products');
        const picker = openFavoritesPicker(harness);

        dialogButton(picker, 'PAGINATION.NEXT').click();
        TestBed.tick();
        expect(readFavorites).toHaveBeenLastCalledWith(2, PAGE_SIZE, '');
        expect(picker.querySelector('fd-favorite-product-row strong')?.textContent.trim()).toBe(firstOnSecondPage.productName);

        button(picker, 'button[aria-label="PRODUCT_DETAIL.REMOVE_FAVORITE"]').click();
        TestBed.tick();
        expect(removeFavorite).toHaveBeenCalledExactlyOnceWith(originalId);
        dialogButton(picker, 'PRODUCT_FAVORITES.UNDO').click();
        TestBed.tick();
        expect(addFavorite).toHaveBeenCalledExactlyOnceWith(
            firstOnSecondPage.productId,
            undefined,
            firstOnSecondPage.preferredPortionAmount,
        );

        dialogButton(picker, 'PAGINATION.NEXT').click();
        TestBed.tick();

        expect(readFavorites).toHaveBeenLastCalledWith(lastPage, PAGE_SIZE, '');
        expect(picker.querySelector('[aria-current="page"]')?.textContent.trim()).toBe(String(lastPage));
        expect(picker.querySelector('fd-favorite-product-row strong')?.textContent.trim()).toBe(favorites.at(-1)?.productName);
    });
});

describe('Routed catalog language changes', () => {
    it('refreshes localized products without changing the active search or ownership filter', async () => {
        const { products } = configureCatalog();
        const localized = makeProduct(CATALOG_ITEM.id, 'Оливковое масло');
        products.queryOverview.mockReturnValueOnce(
            of({ allProducts: page([CATALOG_ITEM]), recentItems: [], favoriteItems: [], favoriteTotalCount: 0 }),
        );
        const harness = await RouterTestingHarness.create('/products?search=olive&onlyMine=true&campaign=summer');
        await waitForCatalogAsync(harness, CATALOG_ITEM.name);
        products.queryOverview.mockReturnValueOnce(
            of({ allProducts: page([localized]), recentItems: [], favoriteItems: [], favoriteTotalCount: 0 }),
        );

        await firstValueFrom(TestBed.inject(TranslateService).use('ru'));
        await waitForCatalogAsync(harness, localized.name);

        expect(products.queryOverview).toHaveBeenCalledTimes(2);
        expect(products.queryOverview.mock.lastCall?.[0]).toMatchObject({
            includePublic: false,
            filters: { search: 'olive' },
        });
        const router = TestBed.inject(Router);
        expect(router.parseUrl(router.url).queryParams).toEqual({ search: 'olive', onlyMine: 'true', campaign: 'summer' });
        expect(catalogRoot(harness).textContent).not.toContain(CATALOG_ITEM.name);
    });
});

describe('Catalog request lifecycle', () => {
    it('cancels superseded search requests and keeps the current results when the external lookup fails', async () => {
        const { products, lookup } = configureCatalog();
        const oldProducts = new Subject<PageOf<Product>>();
        const oldLookup = new Subject<never[]>();
        products.query.mockReturnValueOnce(oldProducts).mockReturnValueOnce(of(page([COFFEE_ITEM])));
        lookup.mockReturnValueOnce(oldLookup).mockReturnValueOnce(throwError(() => new Error('External lookup failed')));
        const harness = await RouterTestingHarness.create('/products');
        await harness.navigateByUrl('/products?search=olive');
        expect(oldProducts.observed).toBe(true);
        expect(oldLookup.observed).toBe(true);

        await harness.navigateByUrl('/products?search=coffee');
        await waitForCatalogAsync(harness, COFFEE_ITEM.name);
        expect(oldProducts.observed).toBe(false);
        expect(oldLookup.observed).toBe(false);
        oldProducts.next(page([CATALOG_ITEM]));
        harness.detectChanges();

        expect(catalogRoot(harness).textContent).not.toContain(CATALOG_ITEM.name);
        expect(catalogRoot(harness).querySelector('fd-error-state')).toBeNull();
        expect(catalogRoot(harness).querySelector('fd-product-list-off-section fd-skeleton-card')).toBeNull();
    });

    it('shows a failed catalog search and retries the same committed query from its error action', async () => {
        const { products } = configureCatalog();
        const harness = await RouterTestingHarness.create('/products');
        products.query.mockReturnValueOnce(throwError(() => new Error('Catalog unavailable'))).mockReturnValueOnce(of(page([COFFEE_ITEM])));
        await harness.navigateByUrl('/products?search=coffee');
        const host = catalogRoot(harness);
        expect(host.querySelector('fd-error-state [role="alert"]')).not.toBeNull();
        expect(searchInput(harness).value).toBe('coffee');

        button(host, 'fd-error-state button').click();
        await waitForCatalogAsync(harness, COFFEE_ITEM.name);
        expect(host.querySelector('fd-error-state')).toBeNull();
        expect(products.query.mock.calls.map(call => call[2]?.search)).toEqual(['coffee', 'coffee']);
    });
});

describe('Catalog responses before a typed search commits', () => {
    it.each(['results', 'error'] as const)('keeps obsolete %s hidden while the latest search loads', async previousOutcome => {
        const { products } = configureCatalog();
        const oldSearch = new Subject<PageOf<Product>>();
        const oldResult = makeProduct('aged-olives', 'Aged green olives');
        products.query.mockReturnValueOnce(oldSearch).mockReturnValueOnce(of(page([COFFEE_ITEM])));
        const harness = await RouterTestingHarness.create('/products');
        const router = TestBed.inject(Router);
        await harness.navigateByUrl('/products?search=olive');
        expect(oldSearch.observed).toBe(true);

        const input = searchInput(harness);
        input.value = 'coffee';
        input.dispatchEvent(new Event('input', { bubbles: true }));
        harness.detectChanges();
        expect(router.parseUrl(router.url).queryParams).toEqual({ search: 'olive' });
        expect(oldSearch.observed).toBe(true);

        if (previousOutcome === 'results') {
            oldSearch.next(page([oldResult]));
            oldSearch.complete();
        } else {
            oldSearch.error(new Error('Previous catalog search failed'));
        }
        harness.detectChanges();

        const host = catalogRoot(harness);
        expect(host.textContent).not.toContain(oldResult.name);
        expect(host.querySelector('fd-error-state')).toBeNull();
        expect(searchInput(harness).value).toBe('coffee');

        await waitForCatalogAsync(harness, COFFEE_ITEM.name);
        expect(router.parseUrl(router.url).queryParams).toEqual({ search: 'coffee' });
        expect(products.query.mock.calls.map(call => call[2]?.search)).toEqual(['olive', 'coffee']);
        expect(host.querySelector('fd-error-state')).toBeNull();
        expect(host.textContent).not.toContain(oldResult.name);
    });

    it('keeps an obsolete overview failure hidden after the user starts searching', async () => {
        const { products } = configureCatalog();
        const overview = new Subject<ProductOverview>();
        products.queryOverview.mockReturnValueOnce(overview);
        products.query.mockReturnValueOnce(of(page([COFFEE_ITEM])));
        const harness = await RouterTestingHarness.create('/products');
        const router = TestBed.inject(Router);

        const input = searchInput(harness);
        input.value = 'coffee';
        input.dispatchEvent(new Event('input', { bubbles: true }));
        harness.detectChanges();
        expect(router.url).toBe('/products');
        expect(overview.observed).toBe(true);
        overview.error(new Error('Previous catalog overview failed'));
        harness.detectChanges();

        expect(catalogRoot(harness).querySelector('fd-error-state')).toBeNull();
        expect(searchInput(harness).value).toBe('coffee');
        await waitForCatalogAsync(harness, COFFEE_ITEM.name);
        expect(products.queryOverview).toHaveBeenCalledOnce();
        expect(products.query.mock.calls.map(call => call[2]?.search)).toEqual(['coffee']);
        expect(router.parseUrl(router.url).queryParams).toEqual({ search: 'coffee' });
    });
});

describe('Retried catalog search and filter navigation', () => {
    it('retries a rejected search route using the term the user typed', async () => {
        const { products } = configureCatalogWithRejectedNavigation(query => query.get('search') === 'coffee');
        products.query.mockReturnValueOnce(of(page([COFFEE_ITEM])));
        const harness = await RouterTestingHarness.create('/products?campaign=summer');
        const router = TestBed.inject(Router);
        await waitForCatalogAsync(harness, RECENT_ITEM.name);

        const input = searchInput(harness);
        input.value = 'coffee';
        input.dispatchEvent(new Event('input', { bubbles: true }));
        await waitForCatalogNavigationErrorAsync(harness);
        expect(router.parseUrl(router.url).queryParams).toEqual({ campaign: 'summer' });
        expect(searchInput(harness).value).toBe('coffee');
        expect(products.query).not.toHaveBeenCalled();

        button(catalogRoot(harness), 'fd-error-state button').click();
        await waitForCatalogAsync(harness, COFFEE_ITEM.name);
        expect(router.parseUrl(router.url).queryParams).toEqual({ search: 'coffee', campaign: 'summer' });
        expect(products.query).toHaveBeenCalledExactlyOnceWith(
            1,
            PAGE_SIZE,
            expect.objectContaining({ search: 'coffee' }),
            expect.objectContaining({ includePublic: true }),
        );
        expect(catalogRoot(harness).querySelector('fd-error-state')).toBeNull();
    });

    it('keeps the committed ownership filter after its reset fails and lets the user clear it again', async () => {
        const { products } = configureCatalogWithRejectedNavigation(query => !query.has('onlyMine'));
        const harness = await RouterTestingHarness.create('/products?onlyMine=true&campaign=summer');
        const router = TestBed.inject(Router);
        await waitForCatalogAsync(harness, CATALOG_ITEM.name);

        button(catalogRoot(harness), '.active-filters__reset button').click();
        await waitForCatalogNavigationErrorAsync(harness);
        expect(router.parseUrl(router.url).queryParams).toEqual({ onlyMine: 'true', campaign: 'summer' });
        expect(catalogRoot(harness).querySelector('.active-filters__chip')).not.toBeNull();
        expect(products.query).not.toHaveBeenCalled();

        button(catalogRoot(harness), 'fd-error-state button').click();
        await waitForCatalogAsync(harness, CATALOG_ITEM.name);
        expect(products.query).toHaveBeenCalledExactlyOnceWith(
            1,
            PAGE_SIZE,
            expect.anything(),
            expect.objectContaining({ includePublic: false }),
        );

        button(catalogRoot(harness), '.active-filters__reset button').click();
        await waitForCatalogAsync(harness, RECENT_ITEM.name);
        expect(router.parseUrl(router.url).queryParams).toEqual({ campaign: 'summer' });
        expect(catalogRoot(harness).querySelector('.active-filters__chip')).toBeNull();
    });
});

describe('Retried catalog page normalization', () => {
    it('retries an invalid page cleanup while preserving unrelated URL parameters', async () => {
        const { products } = configureCatalogWithRejectedNavigation(query => !query.has('page'));
        const harness = await RouterTestingHarness.create('/products?page=invalid&campaign=summer');
        const router = TestBed.inject(Router);
        await waitForCatalogNavigationErrorAsync(harness);
        expect(router.parseUrl(router.url).queryParams).toEqual({ page: 'invalid', campaign: 'summer' });

        button(catalogRoot(harness), 'fd-error-state button').click();
        await waitForCatalogAsync(harness, RECENT_ITEM.name);
        await vi.waitFor(() => {
            expect(router.parseUrl(router.url).queryParams).toEqual({ campaign: 'summer' });
        });
        expect(products.queryOverview.mock.calls.map(([request]) => request.page)).toEqual([1, 1]);
        expect(products.query).not.toHaveBeenCalled();
    });

    it('retries the server page correction and shows the newly fetched last catalog page', async () => {
        const { products } = configureCatalogWithRejectedNavigation(query => query.get('page') === String(LAST_AVAILABLE_PAGE));
        const outOfRange = page([], REQUESTED_PAGE, LAST_AVAILABLE_PAGE);
        products.queryOverview.mockReturnValueOnce(
            of({ allProducts: outOfRange, recentItems: [], favoriteItems: [], favoriteTotalCount: 0 }),
        );
        products.query
            .mockReturnValueOnce(of(outOfRange))
            .mockReturnValueOnce(of(page([COFFEE_ITEM], LAST_AVAILABLE_PAGE, LAST_AVAILABLE_PAGE)));
        const harness = await RouterTestingHarness.create(`/products?page=${REQUESTED_PAGE}&campaign=summer`);
        const router = TestBed.inject(Router);
        await waitForCatalogNavigationErrorAsync(harness);
        expect(router.parseUrl(router.url).queryParams).toEqual({ page: String(REQUESTED_PAGE), campaign: 'summer' });
        expect(products.query).not.toHaveBeenCalled();

        button(catalogRoot(harness), 'fd-error-state button').click();
        await waitForCatalogAsync(harness, COFFEE_ITEM.name);
        expect(router.parseUrl(router.url).queryParams).toEqual({ page: String(LAST_AVAILABLE_PAGE), campaign: 'summer' });
        expect(products.query.mock.calls.map(([requestedPage]) => requestedPage)).toEqual([REQUESTED_PAGE, LAST_AVAILABLE_PAGE]);
        expect(catalogRoot(harness).querySelector('fd-error-state')).toBeNull();
    });
});

function configureCatalogWithRejectedNavigation(shouldReject: (query: ParamMap) => boolean): CatalogFixture {
    let rejected = false;
    return configureCatalog(false, route => {
        if (!rejected && shouldReject(route.queryParamMap)) {
            rejected = true;
            throw new Error('Catalog navigation interrupted');
        }
        return true;
    });
}

async function waitForCatalogNavigationErrorAsync(harness: RouterTestingHarness): Promise<void> {
    await vi.waitFor(() => {
        harness.detectChanges();
        expect(catalogRoot(harness).querySelector('fd-error-state [role="alert"]')).not.toBeNull();
    });
}

describe('Catalog deletion and route ownership', () => {
    it('keeps the product visible on a failed deletion and refreshes the list on retry', async () => {
        const { openDialog, products, showError } = configureCatalog();
        const detailResult = new ProductDetailActionResult(RECENT_ITEM.id, 'Delete');
        openDialog.mockReturnValue({ afterClosed: () => of(detailResult) });
        products.deleteById.mockReturnValueOnce(throwError(() => new Error('Product could not be deleted')));
        const harness = await RouterTestingHarness.create('/products');
        const host = catalogRoot(harness);

        button(host, '.recent-products__open').click();
        await vi.waitFor(() => {
            expect(showError).toHaveBeenCalledExactlyOnceWith('PRODUCT_LIST.DELETE_ERROR');
        });
        harness.detectChanges();
        expect(host.textContent).toContain(RECENT_ITEM.name);
        expect(products.queryOverview).toHaveBeenCalledOnce();

        products.queryOverview.mockReturnValueOnce(
            of({ allProducts: page([COFFEE_ITEM]), recentItems: [], favoriteItems: [], favoriteTotalCount: 0 }),
        );
        button(host, '.recent-products__open').click();
        await waitForCatalogAsync(harness, COFFEE_ITEM.name);

        expect(products.deleteById.mock.calls).toEqual([[RECENT_ITEM.id], [RECENT_ITEM.id]]);
        expect(host.textContent).not.toContain(RECENT_ITEM.name);
        expect(showError).toHaveBeenCalledOnce();
    });

    it('cancels a pending deletion on route departure without reporting a save failure', async () => {
        const { openDialog, products, showError } = configureCatalog();
        const deletion = new Subject<void>();
        products.deleteById.mockReturnValueOnce(deletion);
        openDialog.mockReturnValueOnce({ afterClosed: () => of(new ProductDetailActionResult(RECENT_ITEM.id, 'Delete')) });
        const harness = await RouterTestingHarness.create('/products');
        button(catalogRoot(harness), '.recent-products__open').click();
        await vi.waitFor(() => {
            expect(products.deleteById).toHaveBeenCalledExactlyOnceWith(RECENT_ITEM.id);
            expect(deletion.observed).toBe(true);
        });

        await harness.navigateByUrl('/other');
        await harness.fixture.whenStable();
        expect(deletion.observed).toBe(false);
        expect(showError).not.toHaveBeenCalled();
        expect(products.queryOverview).toHaveBeenCalledOnce();
    });

    it('refreshes the catalog after a confirmed deletion while the route remains open', async () => {
        const { openDialog, products, showError } = configureCatalog();
        const deletion = new Subject<void>();
        products.deleteById.mockReturnValueOnce(deletion);
        openDialog.mockReturnValueOnce({ afterClosed: () => of(new ProductDetailActionResult(RECENT_ITEM.id, 'Delete')) });
        const harness = await RouterTestingHarness.create('/products');
        products.queryOverview.mockReturnValueOnce(
            of({ allProducts: page([COFFEE_ITEM]), recentItems: [], favoriteItems: [], favoriteTotalCount: 0 }),
        );
        button(catalogRoot(harness), '.recent-products__open').click();
        await vi.waitFor(() => {
            expect(deletion.observed).toBe(true);
        });

        deletion.next();
        await waitForCatalogAsync(harness, COFFEE_ITEM.name);
        expect(products.deleteById).toHaveBeenCalledExactlyOnceWith(RECENT_ITEM.id);
        expect(catalogRoot(harness).textContent).not.toContain(RECENT_ITEM.name);
        expect(showError).not.toHaveBeenCalled();
    });
});

it('ignores a deletion failure delivered while the router leaves the catalog', async () => {
    const { openDialog, products, showError } = configureCatalog();
    const deletion = new Subject<void>();
    products.deleteById.mockReturnValueOnce(deletion);
    openDialog.mockReturnValueOnce({ afterClosed: () => of(new ProductDetailActionResult(RECENT_ITEM.id, 'Delete')) });
    const harness = await RouterTestingHarness.create('/products');
    const catalog = catalogRoot(harness);
    button(catalog, '.recent-products__open').click();
    await vi.waitFor(() => {
        expect(products.deleteById).toHaveBeenCalledExactlyOnceWith(RECENT_ITEM.id);
        expect(deletion.observed).toBe(true);
    });

    const router = TestBed.inject(Router);
    const responseDelivered = firstValueFrom(
        router.events.pipe(
            filter(event => event instanceof GuardsCheckEnd && event.urlAfterRedirects === '/other' && event.shouldActivate),
            tap(() => {
                expect(harness.routeNativeElement).toBe(catalog);
                expect(deletion.observed).toBe(true);
                deletion.error(new Error('Product could not be deleted'));
            }),
        ),
    );
    await harness.navigateByUrl('/other');
    await responseDelivered;
    await harness.fixture.whenStable();

    expect(catalogRoot(harness).textContent).toContain('Other page');
    expect(router.url).toBe('/other');
    expect(showError).not.toHaveBeenCalled();
    expect(products.queryOverview).toHaveBeenCalledOnce();
});

describe('Creating a product from the catalog', () => {
    it.each([
        {
            empty: false,
            source: 'header',
            selector: 'button[aria-label="PRODUCT_LIST.ADD_PRODUCT_BUTTON"]',
            label: 'PRODUCT_LIST.ADD_PRODUCT_BUTTON',
        },
        {
            empty: true,
            source: 'empty state',
            selector: 'fd-product-list-empty-state button',
            label: 'PRODUCT_LIST.ADD_FIRST_PRODUCT_BUTTON',
        },
    ])('opens the shipped product editor from the $source action', async ({ empty, selector, label }) => {
        const { products, navigateToAdd } = configureCatalog();
        if (empty) {
            products.queryOverview.mockReturnValueOnce(
                of({ allProducts: page([]), recentItems: [], favoriteItems: [], favoriteTotalCount: 0 }),
            );
        }
        const router = TestBed.inject(Router);
        navigateToAdd.mockImplementation(async () => {
            await router.navigateByUrl('/products/add');
        });
        const harness = await RouterTestingHarness.create('/products');
        const trigger = button(catalogRoot(harness), selector);
        expect(trigger.textContent).toContain(label);

        trigger.click();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(router.url).toBe('/products/add');
            expect(catalogRoot(harness).querySelector('fd-product-add')).not.toBeNull();
        });
        expect(navigateToAdd).toHaveBeenCalledExactlyOnceWith();
        expect(products.deleteById).not.toHaveBeenCalled();
    });
});

describe('Catalog external suggestions', () => {
    it('passes the selected Open Food Facts result to the normal create-product route', async () => {
        const { lookup, navigateToAdd } = configureCatalog();
        const suggestion = { barcode: '5000112637922', name: 'Oat flakes', brand: 'Example', caloriesPer100G: 380 };
        lookup.mockReturnValueOnce(of([suggestion]));
        const harness = await RouterTestingHarness.create('/products?search=oat');
        await waitForCatalogAsync(harness, suggestion.name);

        button(catalogRoot(harness), '.product-list__off-card').click();

        expect(navigateToAdd).toHaveBeenCalledExactlyOnceWith({ state: { barcode: suggestion.barcode, offProduct: suggestion } });
    });
});

describe('Product dialog actions', () => {
    it('opens the current favorites picker from the catalog’s favorites button', async () => {
        const { openDialog } = configureCatalog();
        const harness = await RouterTestingHarness.create('/products');
        button(catalogRoot(harness), '.product-list__favorites-trigger button').click();

        expect(openDialog).toHaveBeenCalledExactlyOnceWith(ProductFavoritesPickerComponent, expect.objectContaining({ size: 'md' }));
    });

    it('releases a pending product detail result when the catalog route is left', async () => {
        const { openDialog, navigateToEdit } = configureCatalog();
        const closed = new Subject<ProductDetailActionResult>();
        const close = vi.fn<() => void>();
        openDialog.mockReturnValueOnce({ afterClosed: () => closed, close });
        const harness = await RouterTestingHarness.create('/products');
        button(catalogRoot(harness), '.recent-products__open').click();
        await vi.waitFor(() => {
            expect(openDialog).toHaveBeenCalledOnce();
            expect(closed.observed).toBe(true);
        });

        await harness.navigateByUrl('/other');
        const listenerSurvived = closed.observed;
        closed.next(new ProductDetailActionResult(RECENT_ITEM.id, 'Edit'));
        await harness.fixture.whenStable();

        expect(navigateToEdit).not.toHaveBeenCalled();
        expect(listenerSurvived).toBe(false);
        expect(close).toHaveBeenCalledOnce();
        expect(catalogRoot(harness).textContent).toContain('Other page');
    });

    it('abandons an open request when the catalog is destroyed before the detail component loads', async () => {
        const { openDialog, navigateToEdit } = configureCatalog();
        const harness = await RouterTestingHarness.create('/products');
        button(catalogRoot(harness), '.recent-products__open').click();
        harness.fixture.destroy();
        await import('./components/detail/product-detail/product-detail');
        await waitForAsyncTasksAsync();

        expect(openDialog).not.toHaveBeenCalled();
        expect(navigateToEdit).not.toHaveBeenCalled();
    });
});

describe('Catalog product gallery details', () => {
    const cover = 'https://images.example.test/product-cover.png';
    const side = 'https://images.example.test/product-side.png';

    it.each([
        { description: 'a legacy cover without a gallery', images: undefined, thumbnails: [], finalCover: cover },
        {
            description: 'a gallery that repeats its main cover',
            images: [
                { imageAssetId: null, imageUrl: cover },
                { imageAssetId: null, imageUrl: side },
            ],
            thumbnails: [cover, side],
            finalCover: side,
        },
    ])('opens readonly details with $description', async ({ images, thumbnails, finalCover }) => {
        const { products, navigateToEdit } = configureRealCatalogDialogs();
        const product: Product = {
            ...RECENT_ITEM,
            productType: undefined,
            imageUrl: cover,
            images,
            isOwnedByCurrentUser: false,
            visibility: ProductVisibility.Public,
        };
        products.queryOverview.mockReturnValueOnce(
            of({ allProducts: page([]), recentItems: [product], favoriteItems: [], favoriteTotalCount: 0 }),
        );
        const harness = await RouterTestingHarness.create('/products');
        button(catalogRoot(harness), '.recent-products__open').click();
        const detail = await vi.waitFor(() => {
            const opened = document.querySelector('fd-product-detail');
            if (!(opened instanceof HTMLElement)) {
                throw new Error('Expected the opened product details');
            }
            return opened;
        });
        const gallery = detail.querySelector('fd-public-product-gallery');
        if (gallery === null) {
            throw new Error('Expected a photo gallery for the selected product');
        }

        expect(detail.querySelector('h2')?.textContent).toContain(product.name);
        expect(detail.textContent).toContain('PRODUCT_DETAIL.WARNING_NOT_OWNER');
        expect(detail.textContent).not.toContain('PRODUCT_DETAIL.EDIT_BUTTON');
        expect(detail.textContent).not.toContain('PRODUCT_DETAIL.DELETE_BUTTON');
        expect(gallery.querySelector('.cover img')?.getAttribute('src')).toBe(cover);
        const choices = [...gallery.querySelectorAll<HTMLButtonElement>('.thumbnails button')];
        expect(choices.map(choice => choice.querySelector('img')?.getAttribute('src'))).toEqual(thumbnails);
        choices.at(-1)?.click();
        TestBed.tick();
        expect(gallery.querySelector('.cover img')?.getAttribute('src')).toBe(finalCover);

        button(detail, 'button[aria-label="COMMON.CLOSE"]').click();
        await vi.waitFor(() => {
            expect(document.querySelector('fd-product-detail')).toBeNull();
        });
        expect(TestBed.inject(Router).url).toBe('/products');
        expect(navigateToEdit).not.toHaveBeenCalled();
    });
});

describe('Product catalog owned dialogs on route departure', () => {
    it('closes the current product details and leaves an independent dialog open', async () => {
        const { navigateToEdit } = configureRealCatalogDialogs();
        const harness = await RouterTestingHarness.create('/products');
        button(catalogRoot(harness), '.recent-products__open').click();
        const detail = await vi.waitFor(() => {
            const opened = document.querySelector('fd-product-detail');
            expect(opened?.querySelector('h2')?.textContent).toContain(RECENT_ITEM.name);
            return opened;
        });
        const independent = TestBed.inject(FdUiDialogService).open(IndependentDialogComponent, { closeOnNavigation: false });

        await harness.navigateByUrl('/other', OtherPageComponent);

        expect(catalogRoot(harness).textContent).toContain('Other page');
        expect(detail?.isConnected).toBe(false);
        expect(document.querySelector('fd-product-detail')).toBeNull();
        expect(document.querySelector('fd-test-independent-dialog')?.textContent).toContain('Independent dialog');
        expect(navigateToEdit).not.toHaveBeenCalled();
        independent.close();
    });

    it('closes the scanner and stops camera tracks granted after leaving the catalog', async () => {
        const { products } = configureRealCatalogDialogs();
        const browserWindow = TestBed.inject(BrowserWindowService);
        const camera = new Subject<MediaStream>();
        const stopTrack = vi.fn<MediaStreamTrack['stop']>();
        const stream = { getTracks: () => [{ stop: stopTrack }] } as unknown as MediaStream;
        vi.spyOn(browserWindow, 'createBarcodeDetector').mockReturnValue({ detect: vi.fn() });
        const requestCamera = vi.spyOn(browserWindow, 'getUserMediaAsync').mockReturnValue(firstValueFrom(camera));
        const harness = await RouterTestingHarness.create('/products');
        button(catalogRoot(harness), 'button[aria-label="PRODUCT_LIST.SCAN_BARCODE"]').click();
        TestBed.tick();
        expect(document.querySelector('fd-barcode-scanner')).not.toBeNull();
        expect(requestCamera).toHaveBeenCalledExactlyOnceWith({ video: { facingMode: 'environment' } });

        await harness.navigateByUrl('/other', OtherPageComponent);

        expect(catalogRoot(harness).textContent).toContain('Other page');
        expect(document.querySelector('fd-barcode-scanner')).toBeNull();
        camera.next(stream);
        camera.complete();
        await vi.waitFor(() => {
            expect(stopTrack).toHaveBeenCalledOnce();
        });
        expect(products.query).not.toHaveBeenCalled();
    });
});

describe('Product picker catalog load recovery', () => {
    it('offers retry after an initial failure and shows the empty state only after a successful response', () => {
        const { products, closePicker } = configureCatalog();
        products.queryOverview
            .mockReturnValueOnce(throwError(() => new Error('Catalog unavailable')))
            .mockReturnValueOnce(of({ allProducts: page([]), recentItems: [], favoriteItems: [], favoriteTotalCount: 0 }));
        const fixture = TestBed.createComponent(ProductListDialogComponent);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('fd-error-state [role="alert"]')).not.toBeNull();
        expect(host.textContent).toContain('ERRORS.LOAD_FAILED_TITLE');
        expect(host.querySelector('.product-select__no-results')).toBeNull();

        button(host, 'fd-error-state button').click();
        fixture.detectChanges();
        expect(products.queryOverview.mock.calls.map(([query]) => query.page)).toEqual([1, 1]);
        expect(products.query).not.toHaveBeenCalled();
        expect(host.querySelector('fd-error-state')).toBeNull();
        expect(host.querySelector('.product-select__no-results')?.textContent).toContain('PRODUCT_LIST.NO_PRODUCTS_FOUND');
        expect(closePicker).not.toHaveBeenCalled();
    });

    it('retries page one with the chosen filters after a failed change from a later picker page', () => {
        const { products, closePicker } = configureRealCatalogDialogs();
        const fruit = { ...makeProduct('apples', 'Apples'), productType: ProductType.Fruit };
        products.queryOverview.mockReturnValueOnce(
            of({
                allProducts: page([CATALOG_ITEM], 1, LAST_AVAILABLE_PAGE),
                recentItems: [],
                favoriteItems: [],
                favoriteTotalCount: 0,
            }),
        );
        products.query
            .mockReturnValueOnce(of(page([COFFEE_ITEM], LAST_AVAILABLE_PAGE, LAST_AVAILABLE_PAGE)))
            .mockReturnValueOnce(throwError(() => new Error('Filtered catalog unavailable')))
            .mockReturnValueOnce(of(page([fruit])));
        const fixture = TestBed.createComponent(ProductListDialogComponent);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        const pagination = host.querySelector('fd-ui-pagination');
        if (!(pagination instanceof HTMLElement)) {
            throw new Error('Expected pagination in the product picker');
        }
        dialogButton(pagination, String(LAST_AVAILABLE_PAGE)).click();
        fixture.detectChanges();
        expect(host.textContent).toContain(COFFEE_ITEM.name);

        button(host, '.product-select__filter-button button').click();
        TestBed.tick();
        const filters = document.querySelector('fd-product-list-filters-dialog');
        if (!(filters instanceof HTMLElement)) {
            throw new Error('Expected the product picker filter dialog');
        }
        button(filters, 'button[aria-label="PRODUCT_MANAGE.PRODUCT_TYPE_OPTIONS.FRUIT"]').click();
        dialogButton(filters, 'PRODUCT_LIST.FILTER_APPLY_BUTTON').click();
        fixture.detectChanges();
        expect(host.querySelector('fd-error-state [role="alert"]')).not.toBeNull();
        expect(host.textContent).toContain('ERRORS.LOAD_FAILED_TITLE');
        expect(host.querySelector('.product-select__item button')).toBeNull();
        expect(host.querySelector('.product-select__filter-count')?.textContent.trim()).toBe('1');

        button(host, 'fd-error-state button').click();
        fixture.detectChanges();
        expect(products.query.mock.calls.map(([pageNumber, , queryFilters]) => [pageNumber, queryFilters?.productTypes ?? []])).toEqual([
            [LAST_AVAILABLE_PAGE, []],
            [1, [ProductType.Fruit]],
            [1, [ProductType.Fruit]],
        ]);
        expect(host.querySelector('fd-error-state')).toBeNull();
        expect(host.textContent).toContain(fruit.name);
        button(host, '.product-select__item button').click();
        expect(closePicker).toHaveBeenCalledExactlyOnceWith(fruit);
    });
});

describe('Rendered product selection', () => {
    it('selects an existing product through the rendered picker row', () => {
        const { closePicker } = configureCatalog();
        const picker = TestBed.createComponent(ProductListDialogComponent);
        picker.detectChanges();

        button(picker.nativeElement as HTMLElement, '.product-select__item button').click();

        expect(closePicker).toHaveBeenCalledExactlyOnceWith(CATALOG_ITEM);
    });

    it('shows the new last page when the requested picker page no longer exists', () => {
        const { products, closePicker } = configureCatalog();
        products.queryOverview.mockReturnValueOnce(
            of({
                allProducts: page([CATALOG_ITEM], 1, REQUESTED_PAGE),
                recentItems: [],
                favoriteItems: [],
                favoriteTotalCount: 0,
            }),
        );
        products.query
            .mockReturnValueOnce(of(page([], REQUESTED_PAGE, LAST_AVAILABLE_PAGE)))
            .mockReturnValueOnce(of(page([COFFEE_ITEM], LAST_AVAILABLE_PAGE, LAST_AVAILABLE_PAGE)));
        const picker = TestBed.createComponent(ProductListDialogComponent);
        picker.detectChanges();
        const host = picker.nativeElement as HTMLElement;
        const pagination = host.querySelector('fd-ui-pagination');
        if (!(pagination instanceof HTMLElement)) {
            throw new Error('Expected catalog picker pagination');
        }
        dialogButton(pagination, String(REQUESTED_PAGE)).click();
        picker.detectChanges();

        expect(products.query.mock.calls.map(([number]) => number)).toEqual([REQUESTED_PAGE, LAST_AVAILABLE_PAGE]);
        expect(host.querySelector('fd-ui-pagination button[aria-current="page"]')?.textContent.trim()).toBe(String(LAST_AVAILABLE_PAGE));
        button(host, '.product-select__item button').click();
        expect(closePicker).toHaveBeenCalledExactlyOnceWith(COFFEE_ITEM);
    });

    it('selects a newly created product while its parent picker is still open', () => {
        const { closePicker, openDialog } = configureCatalog();
        const closed = new Subject<Product>();
        openDialog.mockReturnValueOnce({ afterClosed: () => closed });
        const picker = TestBed.createComponent(ProductListDialogComponent);
        picker.detectChanges();
        button(picker.nativeElement as HTMLElement, '.product-select__footer button').click();

        closed.next(COFFEE_ITEM);

        expect(closePicker).toHaveBeenCalledExactlyOnceWith(COFFEE_ITEM);
        expect(closed.observed).toBe(false);
    });

    it('does not select a newly created item after its original picker has closed', () => {
        const { closePicker, openDialog } = configureCatalog();
        const closed = new Subject<Product>();
        const closeEditor = vi.fn();
        openDialog.mockReturnValueOnce({ afterClosed: () => closed, close: closeEditor });
        const picker = TestBed.createComponent(ProductListDialogComponent);
        picker.detectChanges();
        button(picker.nativeElement as HTMLElement, '.product-select__footer button').click();
        expect(openDialog).toHaveBeenCalledExactlyOnceWith(ProductAddDialogComponent, expect.objectContaining({ preset: 'fullscreen' }));
        expect(closed.observed).toBe(true);

        picker.destroy();
        const listenerSurvived = closed.observed;
        closed.next(COFFEE_ITEM);

        expect(closePicker).not.toHaveBeenCalled();
        expect(listenerSurvived).toBe(false);
        expect(closeEditor).toHaveBeenCalledExactlyOnceWith();
    });
});

describe('Nested product creation owned by the catalogue picker', () => {
    it('closes the open creation editor when its picker is closed and leaves another overlay open', async () => {
        configureRealCatalogDialogs();
        const dialogs = TestBed.inject(FdUiDialogService);
        const picker = dialogs.open<ProductListDialogComponent, undefined, Product | null>(ProductListDialogComponent, { preset: 'list' });
        TestBed.tick();
        const pickerElement = document.querySelector('fd-product-list-dialog');
        if (!(pickerElement instanceof HTMLElement)) {
            throw new Error('Expected the native product picker');
        }

        button(pickerElement, '.product-select__footer button').click();
        TestBed.tick();
        const editor = document.querySelector('fd-product-add-dialog');
        if (!(editor instanceof HTMLElement)) {
            throw new Error('Expected the nested product editor');
        }
        expect(editor.querySelector('fd-product-manage-form')).not.toBeNull();
        expect(editor.isConnected).toBe(true);
        const independent = dialogs.open(IndependentDialogComponent, { closeOnNavigation: false });

        picker.close();

        expect(pickerElement.isConnected).toBe(false);
        await vi.waitFor(() => {
            expect(editor.isConnected).toBe(false);
        });
        expect(document.querySelector('fd-test-independent-dialog')?.textContent).toContain('Independent dialog');
        independent.close();
    });
});

describe('Product editor deletion on the real route', () => {
    it('returns to the product list after deleting the open product', async () => {
        const { harness, deletion, products, navigateToProducts } = await confirmEditorDeletionAsync();

        deletion.next();
        deletion.complete();

        await vi.waitFor(() => {
            expect(TestBed.inject(Router).url).toBe('/products');
            expect(catalogRoot(harness).querySelector('fd-product-edit')).toBeNull();
        });
        expect(products.deleteById).toHaveBeenCalledExactlyOnceWith(CATALOG_ITEM.id);
        expect(navigateToProducts).toHaveBeenCalledOnce();
    });

    it('keeps the newly opened page after deletion returns from the editor the user left', async () => {
        const { harness, deletion, navigateToProducts } = await confirmEditorDeletionAsync();

        await harness.navigateByUrl('/other');
        expect(catalogRoot(harness).textContent).toContain('Other page');
        deletion.next();
        deletion.complete();
        await waitForAsyncTasksAsync();
        await harness.fixture.whenStable();
        harness.detectChanges();

        expect(TestBed.inject(Router).url).toBe('/other');
        expect(catalogRoot(harness).textContent).toContain('Other page');
        expect(navigateToProducts).not.toHaveBeenCalled();
    });
});

async function confirmEditorDeletionAsync(): Promise<{
    harness: RouterTestingHarness;
    deletion: Subject<void>;
    products: ProductMocks;
    navigateToProducts: CatalogFixture['navigateToProducts'];
}> {
    const { products, navigateToProducts } = configureRealCatalogDialogs();
    const deletion = new Subject<void>();
    products.getById.mockReturnValueOnce(of({ ...CATALOG_ITEM, usageCount: 0 }));
    products.deleteById.mockReturnValueOnce(deletion);
    const harness = await RouterTestingHarness.create(`/products/${CATALOG_ITEM.id}/edit`);
    const remove = button(catalogRoot(harness), '.product-manage__delete-button button');
    expect(remove.disabled).toBe(false);
    remove.click();
    const dialog = await vi.waitFor(() => {
        harness.detectChanges();
        const rendered = document.querySelector('fd-confirm-delete-dialog');
        if (!(rendered instanceof HTMLElement)) {
            throw new Error('Expected the product editor deletion confirmation');
        }
        return rendered;
    });
    button(dialog, '.confirm-delete-dialog__footer fd-ui-button:last-child button').click();
    await vi.waitFor(() => {
        expect(deletion.observed).toBe(true);
    });
    return { harness, deletion, products, navigateToProducts };
}
