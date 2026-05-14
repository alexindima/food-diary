import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, FormGroup } from '@angular/forms';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { PagedData } from '../../../../shared/lib/paged-data.data';
import type { OpenFoodFactsProduct } from '../../api/open-food-facts.service';
import { ProductListFacade } from '../../components/list/product-list-lib/product-list.facade';
import { type FavoriteProduct, MeasurementUnit, type Product, ProductType, ProductVisibility } from '../../models/product.data';
import { ProductAddDialogComponent } from '../product-add-dialog/product-add-dialog.component';
import { ProductListDialogComponent } from './product-list-dialog.component';
import type { ProductSelectItemViewModel } from './product-list-dialog.types';

const PRODUCT_CALORIES = 120;
const PRODUCT_PROTEINS = 12;
const PRODUCT_FATS = 4;
const PRODUCT_CARBS = 8;
const QUALITY_SCORE_GREEN = 80;
const PAGE_SIZE = 10;

describe('ProductListDialogComponent', () => {
    it('maps products to selectable dialog items with resolved images', () => {
        const product = createProduct();
        const { component, facade } = setupComponent();
        facade.productData.items.set([product]);
        facade.resolveImage.mockReturnValue('https://example.test/apple.jpg');

        expect(readProductItems(component)).toEqual([
            {
                product,
                imageUrl: 'https://example.test/apple.jpg',
            },
        ]);
    });

    it('closes dialog with selected product when used as a dialog', () => {
        const product = createProduct();
        const { component, dialogRef } = setupComponent();

        component.onProductClick(product);

        expect(dialogRef.close).toHaveBeenCalledWith(product);
    });

    it('emits selected product when embedded', () => {
        const product = createProduct();
        const { component, fixture, dialogRef } = setupComponent();
        const selected: Product[] = [];
        component.productSelected.subscribe(value => {
            selected.push(value);
        });
        fixture.componentRef.setInput('embedded', true);
        fixture.detectChanges();

        component.onProductClick(product);

        expect(dialogRef.close).not.toHaveBeenCalled();
        expect(selected).toEqual([product]);
    });

    it('opens add product dialog and forwards created product to selection flow', () => {
        const product = createProduct();
        const { component, dialogService, dialogRef } = setupComponent();
        dialogService.open.mockReturnValue({ afterClosed: () => of(product) });

        component.onAddProductClick();

        expect(dialogService.open).toHaveBeenCalledWith(ProductAddDialogComponent, { preset: 'fullscreen' });
        expect(dialogRef.close).toHaveBeenCalledWith(product);
    });

    it('ignores canceled add product dialog', () => {
        const { component, dialogService, dialogRef } = setupComponent();
        dialogService.open.mockReturnValue({ afterClosed: () => of(null) });

        component.onAddProductClick();

        expect(dialogRef.close).not.toHaveBeenCalled();
    });
});

function setupComponent(): {
    fixture: ComponentFixture<ProductListDialogComponent>;
    component: ProductListDialogComponent;
    facade: ProductListFacadeMock;
    dialogService: { open: ReturnType<typeof vi.fn> };
    dialogRef: { close: ReturnType<typeof vi.fn> };
} {
    const facade = createProductListFacadeMock();
    const dialogService = { open: vi.fn() };
    const dialogRef = { close: vi.fn() };
    facade.fdDialogService = dialogService;

    TestBed.configureTestingModule({
        imports: [ProductListDialogComponent],
        providers: [{ provide: FdUiDialogRef, useValue: dialogRef }],
    });
    TestBed.overrideComponent(ProductListDialogComponent, {
        set: {
            template: '',
            providers: [
                { provide: ProductListFacade, useValue: facade },
                { provide: FdUiDialogService, useValue: dialogService },
            ],
        },
    });

    const fixture = TestBed.createComponent(ProductListDialogComponent);
    fixture.detectChanges();

    return { fixture, component: fixture.componentInstance, facade, dialogService, dialogRef };
}

type ProductListFacadeMock = Omit<ProductListFacade, 'fdDialogService' | 'navigationService'> & {
    fdDialogService: { open: ReturnType<typeof vi.fn> };
    navigationService: object;
    resolveImage: ReturnType<typeof vi.fn>;
};

function readProductItems(component: ProductListDialogComponent): ProductSelectItemViewModel[] {
    return (component as unknown as { productItems: () => ProductSelectItemViewModel[] }).productItems();
}

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
