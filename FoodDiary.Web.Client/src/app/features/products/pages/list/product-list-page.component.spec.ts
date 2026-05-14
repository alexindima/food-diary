import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, FormGroup } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { EMPTY, type Observable, of, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { PagedData } from '../../../../shared/lib/paged-data.data';
import type { OpenFoodFactsProduct } from '../../api/open-food-facts.service';
import { ProductDetailActionResult } from '../../components/detail/product-detail-lib/product-detail.types';
import { ProductListFacade } from '../../components/list/product-list-lib/product-list.facade';
import { type FavoriteProduct, MeasurementUnit, type Product, ProductType, ProductVisibility } from '../../models/product.data';
import { ProductListPageComponent } from './product-list-page.component';

const PRODUCT_CALORIES = 120;
const PRODUCT_PROTEINS = 12;
const PRODUCT_FATS = 4;
const PRODUCT_CARBS = 8;
const QUALITY_SCORE_GREEN = 80;
const PAGE_SIZE = 10;
const ASYNC_IMPORT_FLUSH_DELAY_MS = 0;
const WAIT_ATTEMPTS = 20;

describe('ProductListPageComponent', () => {
    it('reloads favorites and current page after favorite change in detail dialog', async () => {
        const { component, facade, dialogService } = setupComponent();
        dialogService.open.mockReturnValue({ afterClosed: () => of(new ProductDetailActionResult('product-1', 'FavoriteChanged')) });

        component.onProductClick(createProduct());
        await waitForAsync(() => facade.loadFavorites.mock.calls.length > 0);

        expect(facade.loadFavorites).toHaveBeenCalled();
        expect(facade.reloadCurrentPage).toHaveBeenCalled();
    });

    it('navigates to product edit after edit action in detail dialog', async () => {
        const { component, facade, dialogService } = setupComponent();
        dialogService.open.mockReturnValue({ afterClosed: () => of(new ProductDetailActionResult('product-1', 'Edit')) });

        component.onProductClick(createProduct());
        await waitForAsync(() => facade.navigationService.navigateToProductEditAsync.mock.calls.length > 0);

        expect(facade.navigationService.navigateToProductEditAsync).toHaveBeenCalledWith('product-1');
    });

    it('deletes owned product and scrolls to top after delete action in detail dialog', async () => {
        const { component, facade, dialogService } = setupComponent();
        const scrollSpy = vi.spyOn(component as unknown as { scrollToTop: () => void }, 'scrollToTop').mockImplementation(() => undefined);
        dialogService.open.mockReturnValue({ afterClosed: () => of(new ProductDetailActionResult('product-1', 'Delete')) });
        facade.deleteProductAndReload.mockReturnValue(of(undefined));

        component.onProductClick(createProduct({ isOwnedByCurrentUser: true }));
        await waitForAsync(() => facade.deleteProductAndReload.mock.calls.length > 0);

        expect(facade.deleteProductAndReload).toHaveBeenCalledWith('product-1');
        expect(scrollSpy).toHaveBeenCalled();
    });

    it('does not delete public product after delete action in detail dialog', async () => {
        const { component, facade, dialogService } = setupComponent();
        dialogService.open.mockReturnValue({ afterClosed: () => of(new ProductDetailActionResult('product-1', 'Delete')) });

        component.onProductClick(createProduct({ isOwnedByCurrentUser: false }));
        await waitForAsync(() => dialogService.open.mock.calls.length > 0);

        expect(facade.deleteProductAndReload).not.toHaveBeenCalled();
    });

    it('shows delete error and clears loading state when delete fails', async () => {
        const { component, facade, dialogService, toastService } = setupComponent();
        dialogService.open.mockReturnValue({ afterClosed: () => of(new ProductDetailActionResult('product-1', 'Delete')) });
        facade.deleteProductAndReload.mockReturnValue(throwError(() => new Error('Delete failed')));
        facade.productData.setLoading(true);

        component.onProductClick(createProduct());
        await waitForAsync(() => toastService.error.mock.calls.length > 0);

        expect(facade.productData.isLoading()).toBe(false);
        expect(toastService.error).toHaveBeenCalledWith('PRODUCT_LIST.DELETE_ERROR');
    });
});

function setupComponent(): {
    fixture: ComponentFixture<ProductListPageComponent>;
    component: ProductListPageComponent;
    facade: ProductListFacadeMock;
    dialogService: { open: ReturnType<typeof vi.fn> };
    toastService: { error: ReturnType<typeof vi.fn> };
} {
    const facade = createProductListFacadeMock();
    const dialogService = { open: vi.fn().mockReturnValue({ afterClosed: (): Observable<never> => EMPTY }) };
    const toastService = { error: vi.fn() };
    facade.fdDialogService = dialogService;

    TestBed.configureTestingModule({
        imports: [ProductListPageComponent],
        providers: [
            { provide: TranslateService, useValue: { instant: (key: string): string => key } },
            { provide: FdUiToastService, useValue: toastService },
        ],
    });
    TestBed.overrideComponent(ProductListPageComponent, {
        set: {
            template: '',
            providers: [{ provide: ProductListFacade, useValue: facade }],
        },
    });

    const fixture = TestBed.createComponent(ProductListPageComponent);
    fixture.detectChanges();

    return { fixture, component: fixture.componentInstance, facade, dialogService, toastService };
}

type ProductListFacadeMock = Omit<ProductListFacade, 'fdDialogService' | 'navigationService'> & {
    deleteProductAndReload: ReturnType<typeof vi.fn>;
    fdDialogService: { open: ReturnType<typeof vi.fn> };
    loadFavorites: ReturnType<typeof vi.fn>;
    navigationService: {
        navigateToProductEditAsync: ReturnType<typeof vi.fn>;
    };
    reloadCurrentPage: ReturnType<typeof vi.fn>;
};

function createProductListFacadeMock(): ProductListFacadeMock {
    return {
        searchForm: new FormGroup({
            search: new FormControl<string | null>(null),
            onlyMine: new FormControl<boolean>(false, { nonNullable: true }),
        }),
        productData: new PagedData<Product>(),
        favorites: signal<FavoriteProduct[]>([]),
        favoriteTotalCount: signal(0),
        isFavoritesOpen: signal(false),
        favoriteLoadingIds: signal<ReadonlySet<string>>(new Set<string>()),
        isFavoritesLoadingMore: signal(false),
        errorKey: signal<string | null>(null),
        onlyMineFilter: signal(false),
        isMobileView: signal(false),
        recentProductItems: signal([]),
        allProductItems: signal([]),
        hasVisibleProducts: signal(false),
        hasActiveFilters: signal(false),
        isEmptyState: signal(true),
        allProductsSectionLabelKey: signal('PRODUCT_LIST.ALL_PRODUCTS'),
        isMobileSearchVisible: signal(false),
        offProducts: signal<OpenFoodFactsProduct[]>([]),
        offLoading: signal(false),
        pageSize: PAGE_SIZE,
        fdDialogService: { open: vi.fn() },
        navigationService: {
            navigateToProductEditAsync: vi.fn().mockResolvedValue(true),
        },
        currentPageIndex: 0,
        resolveImage: vi.fn(),
        retryLoad: vi.fn(),
        onPageChange: vi.fn(),
        onAddProductClick: vi.fn(),
        openBarcodeScanner: vi.fn(),
        toggleOnlyMine: vi.fn(),
        toggleMobileSearch: vi.fn(),
        openFilters: vi.fn(),
        onOffProductClick: vi.fn(),
        onAddToMeal: vi.fn(),
        loadFavorites: vi.fn(),
        onProductFavoriteToggle: vi.fn(),
        toggleFavorites: vi.fn(),
        openFavoriteProduct: vi.fn(),
        addFavoriteProductToMeal: vi.fn(),
        removeFavorite: vi.fn(),
        reloadCurrentPage: vi.fn(),
        deleteProductAndReload: vi.fn().mockReturnValue(of(undefined)),
    } as unknown as ProductListFacadeMock;
}

async function flushPromisesAsync(): Promise<void> {
    await Promise.resolve();
    await new Promise(resolve => {
        setTimeout(resolve, ASYNC_IMPORT_FLUSH_DELAY_MS);
    });
}

async function waitForAsync(predicate: () => boolean): Promise<void> {
    for (let attempt = 0; attempt < WAIT_ATTEMPTS; attempt++) {
        if (predicate()) {
            return;
        }

        await flushPromisesAsync();
    }
}

function createProduct(overrides: Partial<Product> = {}): Product {
    return {
        id: 'product-1',
        name: 'Apple',
        barcode: null,
        brand: 'Garden',
        productType: ProductType.Fruit,
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
