import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, FormGroup } from '@angular/forms';
import { describe, expect, it, vi } from 'vitest';

import { PagedData } from '../../../../../shared/lib/paged-data.data';
import type { OpenFoodFactsProduct } from '../../../api/open-food-facts.service';
import { type FavoriteProduct, MeasurementUnit, type Product, ProductType, ProductVisibility } from '../../../models/product.data';
import { ProductListFacade } from '../product-list-lib/product-list.facade';
import { ProductListBaseComponent } from './product-list-base.component';

const PRODUCT_CALORIES = 120;
const PRODUCT_PROTEINS = 12;
const PRODUCT_FATS = 4;
const PRODUCT_CARBS = 8;
const QUALITY_SCORE_GREEN = 80;
const PAGE_INDEX = 2;
const PAGE_SIZE = 10;

describe('ProductListBaseComponent', () => {
    it('delegates list actions to facade', () => {
        const { component, facade } = setupComponent();

        component.retryLoad();
        component.onAddProductClick();
        component.openBarcodeScanner();
        component.toggleOnlyMine();
        component.toggleMobileSearch();
        component.openFilters();
        component.onOffProductClick(createOpenFoodFactsProduct());
        component.onAddToMeal(createProduct());
        component.loadFavorites();
        component.onProductFavoriteToggle(createProduct());
        component.toggleFavorites();
        component.addFavoriteProductToMeal(createFavoriteProduct());
        component.removeFavorite(createFavoriteProduct());

        expect(facade.retryLoad).toHaveBeenCalled();
        expect(facade.onAddProductClick).toHaveBeenCalled();
        expect(facade.openBarcodeScanner).toHaveBeenCalled();
        expect(facade.toggleOnlyMine).toHaveBeenCalled();
        expect(facade.toggleMobileSearch).toHaveBeenCalled();
        expect(facade.openFilters).toHaveBeenCalled();
        expect(facade.onOffProductClick).toHaveBeenCalledWith(createOpenFoodFactsProduct());
        expect(facade.onAddToMeal).toHaveBeenCalledWith(createProduct());
        expect(facade.loadFavorites).toHaveBeenCalled();
        expect(facade.onProductFavoriteToggle).toHaveBeenCalledWith(createProduct());
        expect(facade.toggleFavorites).toHaveBeenCalled();
        expect(facade.addFavoriteProductToMeal).toHaveBeenCalledWith(createFavoriteProduct());
        expect(facade.removeFavorite).toHaveBeenCalledWith(createFavoriteProduct());
    });

    it('scrolls to top before loading selected page', () => {
        const { component, facade } = setupComponent();
        const scrollSpy = vi.spyOn(component as unknown as { scrollToTop: () => void }, 'scrollToTop').mockImplementation(() => undefined);

        component.onPageChange(PAGE_INDEX);

        expect(scrollSpy).toHaveBeenCalled();
        expect(facade.onPageChange).toHaveBeenCalledWith(PAGE_INDEX);
    });

    it('opens favorite product through regular product click flow', () => {
        const { component, facade } = setupComponent();
        const product = createProduct({ id: 'favorite-product' });
        const clickSpy = vi.spyOn(component, 'onProductClick');
        facade.openFavoriteProduct.mockImplementation((_favorite: FavoriteProduct, openProduct: (value: Product) => void) => {
            openProduct(product);
        });

        component.openFavoriteProduct(createFavoriteProduct());

        expect(facade.openFavoriteProduct).toHaveBeenCalledWith(createFavoriteProduct(), expect.any(Function));
        expect(clickSpy).toHaveBeenCalledWith(product);
    });

    it('exposes current page index and image resolver from facade', () => {
        const { component, facade } = setupComponent();
        facade.currentPageIndex = PAGE_INDEX;
        facade.resolveImage.mockReturnValue('https://example.test/image.jpg');

        expect(component.currentPageIndex).toBe(PAGE_INDEX);
        expect(component.resolveImage(createProduct())).toBe('https://example.test/image.jpg');
    });
});

function setupComponent(): {
    fixture: ComponentFixture<ProductListBaseComponent>;
    component: ProductListBaseComponent;
    facade: ProductListFacadeMock;
} {
    const facade = createProductListFacadeMock();
    TestBed.configureTestingModule({
        imports: [ProductListBaseComponent],
    });
    TestBed.overrideComponent(ProductListBaseComponent, {
        set: {
            template: '',
            providers: [{ provide: ProductListFacade, useValue: facade }],
        },
    });

    const fixture = TestBed.createComponent(ProductListBaseComponent);
    fixture.detectChanges();

    return { fixture, component: fixture.componentInstance, facade };
}

type ProductListFacadeMock = Omit<ProductListFacade, 'fdDialogService' | 'navigationService'> & {
    addFavoriteProductToMeal: ReturnType<typeof vi.fn>;
    fdDialogService: object;
    loadFavorites: ReturnType<typeof vi.fn>;
    navigationService: object;
    onAddProductClick: ReturnType<typeof vi.fn>;
    onAddToMeal: ReturnType<typeof vi.fn>;
    onOffProductClick: ReturnType<typeof vi.fn>;
    onPageChange: ReturnType<typeof vi.fn>;
    onProductFavoriteToggle: ReturnType<typeof vi.fn>;
    openBarcodeScanner: ReturnType<typeof vi.fn>;
    openFavoriteProduct: ReturnType<typeof vi.fn>;
    openFilters: ReturnType<typeof vi.fn>;
    removeFavorite: ReturnType<typeof vi.fn>;
    resolveImage: ReturnType<typeof vi.fn>;
    retryLoad: ReturnType<typeof vi.fn>;
    toggleFavorites: ReturnType<typeof vi.fn>;
    toggleMobileSearch: ReturnType<typeof vi.fn>;
    toggleOnlyMine: ReturnType<typeof vi.fn>;
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
        fdDialogService: {},
        navigationService: {},
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
    } as unknown as ProductListFacadeMock;
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

function createFavoriteProduct(overrides: Partial<FavoriteProduct> = {}): FavoriteProduct {
    return {
        id: 'favorite-1',
        productId: 'product-1',
        name: 'Favorite apple',
        createdAtUtc: '2026-01-01T00:00:00Z',
        productName: 'Apple',
        brand: 'Garden',
        imageUrl: null,
        caloriesPerBase: PRODUCT_CALORIES,
        baseUnit: MeasurementUnit.G,
        defaultPortionAmount: 100,
        ...overrides,
    };
}

function createOpenFoodFactsProduct(): OpenFoodFactsProduct {
    return {
        barcode: '4600000000000',
        name: 'Open product',
        brand: 'Open brand',
        category: 'Snacks',
        imageUrl: 'https://example.test/open.jpg',
        caloriesPer100G: PRODUCT_CALORIES,
        proteinsPer100G: PRODUCT_PROTEINS,
        fatsPer100G: PRODUCT_FATS,
        carbsPer100G: PRODUCT_CARBS,
        fiberPer100G: 1,
    };
}
