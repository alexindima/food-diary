import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { FavoriteProductService } from '../../../api/favorite-product.service';
import { ProductService } from '../../../api/product.service';
import { type FavoriteProduct, MeasurementUnit, type Product, ProductType, ProductVisibility } from '../../../models/product.data';
import { ProductDetailFacade } from './product-detail.facade';
import { ProductDetailActionResult } from './product-detail.types';

const PRODUCT_CALORIES = 120;
const PRODUCT_PROTEINS = 12;
const PRODUCT_FATS = 4;
const PRODUCT_CARBS = 8;
const QUALITY_SCORE_GREEN = 80;

let facade: ProductDetailFacade;
let productService: { duplicate: ReturnType<typeof vi.fn> };
let favoriteProductService: {
    isFavorite: ReturnType<typeof vi.fn>;
    add: ReturnType<typeof vi.fn>;
    remove: ReturnType<typeof vi.fn>;
    getAll: ReturnType<typeof vi.fn>;
};
let dialogRef: { close: ReturnType<typeof vi.fn> };
let dialogService: { open: ReturnType<typeof vi.fn> };

beforeEach(() => {
    productService = {
        duplicate: vi.fn(),
    };
    favoriteProductService = {
        isFavorite: vi.fn(),
        add: vi.fn(),
        remove: vi.fn(),
        getAll: vi.fn(),
    };
    dialogRef = {
        close: vi.fn(),
    };
    dialogService = {
        open: vi.fn(),
    };

    productService.duplicate.mockReturnValue(of(createProduct({ id: 'duplicated-product' })));
    favoriteProductService.isFavorite.mockReturnValue(of(false));
    favoriteProductService.add.mockReturnValue(of(createFavoriteProduct()));
    favoriteProductService.remove.mockReturnValue(of(null));
    favoriteProductService.getAll.mockReturnValue(of([createFavoriteProduct()]));
    dialogService.open.mockReturnValue({ afterClosed: () => of(false) });

    TestBed.configureTestingModule({
        providers: [
            ProductDetailFacade,
            { provide: ProductService, useValue: productService },
            { provide: FavoriteProductService, useValue: favoriteProductService },
            { provide: FdUiDialogRef, useValue: dialogRef },
            { provide: FdUiDialogService, useValue: dialogService },
            {
                provide: TranslateService,
                useValue: {
                    instant: vi.fn((key: string) => key),
                },
            },
        ],
    });

    facade = TestBed.inject(ProductDetailFacade);
});

describe('ProductDetailFacade favorites', () => {
    it('initializes favorite state from API result', () => {
        favoriteProductService.isFavorite.mockReturnValueOnce(of(true));

        facade.initialize(createProduct({ isFavorite: false }));

        expect(favoriteProductService.isFavorite).toHaveBeenCalledWith('product-1');
        expect(facade.isFavorite()).toBe(true);
        expect(facade.hasFavoriteChanged()).toBe(false);
    });

    it('adds favorite and tracks changed state', () => {
        const product = createProduct({ isFavorite: false, favoriteProductId: null });
        facade.initialize(product);

        facade.toggleFavorite(product);

        expect(favoriteProductService.add).toHaveBeenCalledWith('product-1');
        expect(facade.isFavorite()).toBe(true);
        expect(facade.isFavoriteLoading()).toBe(false);
        expect(facade.hasFavoriteChanged()).toBe(true);
    });

    it('removes favorite by current favorite id', () => {
        const product = createProduct({ isFavorite: true, favoriteProductId: 'favorite-1' });
        favoriteProductService.isFavorite.mockReturnValueOnce(of(true));
        facade.initialize(product);

        facade.toggleFavorite(product);

        expect(favoriteProductService.remove).toHaveBeenCalledWith('favorite-1');
        expect(favoriteProductService.getAll).not.toHaveBeenCalled();
        expect(facade.isFavorite()).toBe(false);
        expect(facade.isFavoriteLoading()).toBe(false);
    });

    it('falls back to favorites lookup when favorite id is missing', () => {
        const product = createProduct({ isFavorite: true, favoriteProductId: null });
        favoriteProductService.isFavorite.mockReturnValueOnce(of(true));
        facade.initialize(product);

        facade.toggleFavorite(product);

        expect(favoriteProductService.getAll).toHaveBeenCalled();
        expect(favoriteProductService.remove).toHaveBeenCalledWith('favorite-1');
        expect(facade.isFavorite()).toBe(false);
    });
});

describe('ProductDetailFacade dialog actions', () => {
    it('closes with no result when favorite state did not change', () => {
        const product = createProduct();
        facade.initialize(product);

        facade.close(product);

        expect(dialogRef.close).toHaveBeenCalledWith();
    });

    it('closes with favorite changed result when favorite state changed', () => {
        const product = createProduct();
        facade.initialize(product);
        facade.toggleFavorite(product);

        facade.close(product);

        expect(dialogRef.close).toHaveBeenCalledWith(new ProductDetailActionResult('product-1', 'FavoriteChanged', true));
    });

    it('closes with delete action only after confirmation', () => {
        const product = createProduct();
        dialogService.open.mockReturnValueOnce({ afterClosed: () => of(true) });

        facade.delete(product);

        expect(dialogRef.close).toHaveBeenCalledWith(new ProductDetailActionResult('product-1', 'Delete', false));
    });
});

describe('ProductDetailFacade duplicate', () => {
    it('duplicates product and closes with duplicated product id', () => {
        const product = createProduct();

        facade.duplicate(product);

        expect(productService.duplicate).toHaveBeenCalledWith('product-1');
        expect(dialogRef.close).toHaveBeenCalledWith(new ProductDetailActionResult('duplicated-product', 'Duplicate', false));
        expect(facade.isDuplicateInProgress()).toBe(true);
    });

    it('releases duplicate loading state when duplicate request fails', () => {
        productService.duplicate.mockReturnValueOnce(throwError(() => new Error('Duplicate failed')));

        facade.duplicate(createProduct());

        expect(dialogRef.close).not.toHaveBeenCalled();
        expect(facade.isDuplicateInProgress()).toBe(false);
    });
});

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
        isFavorite: false,
        favoriteProductId: null,
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
