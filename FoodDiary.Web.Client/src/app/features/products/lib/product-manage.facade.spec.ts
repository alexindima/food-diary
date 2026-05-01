import { TestBed } from '@angular/core/testing';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from '../../../services/auth.service';
import { NavigationService } from '../../../services/navigation.service';
import { ProductService } from '../api/product.service';
import { CreateProductRequest, MeasurementUnit, Product, ProductType, ProductVisibility } from '../models/product.data';
import { ProductManageFacade } from './product-manage.facade';

describe('ProductManageFacade', () => {
    let facade: ProductManageFacade;
    let productService: { create: ReturnType<typeof vi.fn>; update: ReturnType<typeof vi.fn>; deleteById: ReturnType<typeof vi.fn> };
    let dialogService: { open: ReturnType<typeof vi.fn> };
    let navigationService: {
        navigateToHome: ReturnType<typeof vi.fn>;
        navigateToProductList: ReturnType<typeof vi.fn>;
        navigateToPremiumAccess: ReturnType<typeof vi.fn>;
    };
    let authService: { isPremium: ReturnType<typeof vi.fn> };

    const product: Product = {
        id: 'p1',
        name: 'Test product',
        barcode: null,
        brand: null,
        productType: ProductType.Unknown,
        category: null,
        description: null,
        comment: null,
        imageUrl: null,
        imageAssetId: null,
        baseUnit: MeasurementUnit.G,
        baseAmount: 100,
        defaultPortionAmount: 100,
        caloriesPerBase: 100,
        proteinsPerBase: 10,
        fatsPerBase: 5,
        carbsPerBase: 12,
        fiberPerBase: 1,
        alcoholPerBase: 0,
        usageCount: 0,
        visibility: ProductVisibility.Private,
        createdAt: new Date('2026-01-01T00:00:00Z'),
        isOwnedByCurrentUser: true,
        qualityScore: 80,
        qualityGrade: 'green',
    };

    const request: CreateProductRequest = {
        name: 'Test product',
        barcode: null,
        brand: null,
        productType: ProductType.Unknown,
        category: null,
        description: null,
        comment: null,
        imageUrl: null,
        imageAssetId: null,
        baseAmount: 100,
        defaultPortionAmount: 100,
        baseUnit: MeasurementUnit.G,
        caloriesPerBase: 100,
        proteinsPerBase: 10,
        fatsPerBase: 5,
        carbsPerBase: 12,
        fiberPerBase: 1,
        alcoholPerBase: 0,
        visibility: ProductVisibility.Private,
    };

    beforeEach(() => {
        productService = {
            create: vi.fn(),
            update: vi.fn(),
            deleteById: vi.fn(),
        };
        dialogService = {
            open: vi.fn(),
        };
        navigationService = {
            navigateToHome: vi.fn(),
            navigateToProductList: vi.fn(),
            navigateToPremiumAccess: vi.fn(),
        };
        authService = {
            isPremium: vi.fn(),
        };

        navigationService.navigateToHome.mockResolvedValue(true);
        navigationService.navigateToProductList.mockResolvedValue(true);
        navigationService.navigateToPremiumAccess.mockResolvedValue(true);
        dialogService.open.mockReturnValue({ afterClosed: () => of(false) });
        productService.create.mockReturnValue(of(product));
        productService.update.mockReturnValue(of(product));
        productService.deleteById.mockReturnValue(of(undefined));
        authService.isPremium.mockReturnValue(true);

        TestBed.configureTestingModule({
            providers: [
                ProductManageFacade,
                { provide: ProductService, useValue: productService },
                { provide: FdUiDialogService, useValue: dialogService },
                { provide: NavigationService, useValue: navigationService },
                { provide: AuthService, useValue: authService },
            ],
        });

        facade = TestBed.inject(ProductManageFacade);
    });

    it('should create product on submit when product is null', async () => {
        const result = await facade.submitProduct(null, request, true);

        expect(productService.create).toHaveBeenCalledWith(request);
        expect(result.product).toEqual(product);
        expect(result.error).toBeNull();
    });

    it('should update product on submit when editing existing product', async () => {
        const result = await facade.submitProduct(product, request, true);

        expect(productService.update).toHaveBeenCalledWith(
            'p1',
            expect.objectContaining({
                name: 'Test product',
                clearBarcode: true,
                clearBrand: true,
            }),
        );
        expect(result.product).toEqual(product);
        expect(result.error).toBeNull();
    });

    it('should return error when save fails', async () => {
        productService.create.mockReturnValueOnce(throwError(() => ({ status: 400 })));

        const result = await facade.submitProduct(null, request, true);

        expect(result.product).toBeNull();
        expect(result.error?.status).toBe(400);
    });

    it('should delete product and navigate after confirmation', async () => {
        dialogService.open.mockReturnValueOnce({ afterClosed: () => of(true) });

        const result = await facade.deleteProduct(product, {
            title: 'Delete',
            message: 'Confirm',
        });

        expect(productService.deleteById).toHaveBeenCalledWith('p1');
        expect(navigationService.navigateToProductList).toHaveBeenCalled();
        expect(result).toBe('deleted');
    });
});
