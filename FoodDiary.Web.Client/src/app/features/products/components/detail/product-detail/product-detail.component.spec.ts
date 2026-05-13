import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { FavoriteProductService } from '../../../api/favorite-product.service';
import { ProductService } from '../../../api/product.service';
import { MeasurementUnit, type Product, ProductVisibility } from '../../../models/product.data';
import { ProductDetailComponent } from './product-detail.component';

const PRODUCT_CALORIES = 165;
const PRODUCT_PROTEINS = 31;
const PRODUCT_FATS = 3.6;
const USED_PRODUCT_USAGE_COUNT = 5;
const MACRO_SUMMARY_BLOCK_COUNT = 3;
const FAVORITE_ID = 'favorite-1';

const mockProduct: Product = {
    id: '1',
    name: 'Test Product',
    isOwnedByCurrentUser: true,
    baseUnit: MeasurementUnit.G,
    baseAmount: 100,
    defaultPortionAmount: 100,
    caloriesPerBase: PRODUCT_CALORIES,
    proteinsPerBase: PRODUCT_PROTEINS,
    fatsPerBase: PRODUCT_FATS,
    carbsPerBase: 0,
    fiberPerBase: 0,
    alcoholPerBase: 0,
    visibility: ProductVisibility.Private,
    usageCount: 0,
    createdAt: new Date('2024-01-01'),
    qualityScore: 80,
    qualityGrade: 'green',
};

const mockFavoriteProduct = {
    id: FAVORITE_ID,
    productId: mockProduct.id,
    createdAtUtc: '2024-01-01T00:00:00Z',
    productName: mockProduct.name,
    caloriesPerBase: PRODUCT_CALORIES,
    baseUnit: MeasurementUnit.G,
    defaultPortionAmount: 100,
};

let component: ProductDetailComponent;
let fixture: ComponentFixture<ProductDetailComponent>;

const mockDialogRef = {
    close: vi.fn(),
};

const mockConfirmDialogRef = {
    afterClosed: vi.fn().mockReturnValue(of(true)),
};

const mockFdDialogService = {
    open: vi.fn().mockReturnValue(mockConfirmDialogRef),
};

const mockProductService = {
    duplicate: vi.fn().mockReturnValue(of({ ...mockProduct, id: '2', name: 'Test Product (Copy)' })),
};

const mockFavoriteProductService = {
    isFavorite: vi.fn().mockReturnValue(of(false)),
    add: vi.fn().mockReturnValue(of(mockFavoriteProduct)),
    remove: vi.fn().mockReturnValue(of(undefined)),
    getAll: vi.fn().mockReturnValue(of([mockFavoriteProduct])),
};

async function createComponentAsync(product: Product = mockProduct): Promise<ProductDetailComponent> {
    await TestBed.resetTestingModule()
        .configureTestingModule({
            imports: [ProductDetailComponent, TranslateModule.forRoot()],
            providers: [
                { provide: FD_UI_DIALOG_DATA, useValue: product },
                { provide: FdUiDialogRef, useValue: mockDialogRef },
                { provide: FdUiDialogService, useValue: mockFdDialogService },
                { provide: ProductService, useValue: mockProductService },
                { provide: FavoriteProductService, useValue: mockFavoriteProductService },
            ],
        })
        .compileComponents();

    fixture = TestBed.createComponent(ProductDetailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    return component;
}

beforeEach(async () => {
    vi.clearAllMocks();
    mockConfirmDialogRef.afterClosed.mockReturnValue(of(true));
    mockProductService.duplicate.mockReturnValue(of({ ...mockProduct, id: '2', name: 'Test Product (Copy)' }));
    mockFavoriteProductService.isFavorite.mockReturnValue(of(false));
    mockFavoriteProductService.add.mockReturnValue(of(mockFavoriteProduct));
    mockFavoriteProductService.remove.mockReturnValue(of(undefined));
    mockFavoriteProductService.getAll.mockReturnValue(of([mockFavoriteProduct]));
    await createComponentAsync();
});

describe('ProductDetailComponent summary state', () => {
    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should display product nutrition', () => {
        expect(component.calories).toBe(PRODUCT_CALORIES);
        expect(component.nutritionForm.controls.proteins.value).toBe(PRODUCT_PROTEINS);
        expect(component.nutritionForm.controls.fats.value).toBe(PRODUCT_FATS);
        expect(component.nutritionForm.controls.carbs.value).toBe(0);
    });

    it('should build macro blocks and macro bar state with positive segments only', () => {
        expect(component.macroBlocks.length).toBe(USED_PRODUCT_USAGE_COUNT);
        expect(component.macroSummaryBlocks.length).toBe(MACRO_SUMMARY_BLOCK_COUNT);
        expect(component.macroBlocks[0].value).toBe(PRODUCT_PROTEINS);
        expect(component.macroBlocks[1].value).toBe(PRODUCT_FATS);
        expect(component.macroBlocks[2].value).toBe(0);
        expect(component.macroBarState.isEmpty).toBe(false);
        expect(component.macroBarState.segments.map(segment => segment.key)).toEqual(['proteins', 'fats']);
    });

    it('should have summary and nutrients tabs', () => {
        expect(component.tabs.length).toBe(2);
        expect(component.tabs[0].value).toBe('summary');
        expect(component.tabs[1].value).toBe('nutrients');
    });

    it('should change active tab', () => {
        expect(component.activeTab()).toBe('summary');

        component.onTabChange('nutrients');
        expect(component.activeTab()).toBe('nutrients');

        component.onTabChange('summary');
        expect(component.activeTab()).toBe('summary');
    });
});

describe('ProductDetailComponent actions', () => {
    it('should emit edit action', () => {
        component.onEdit();

        expect(mockDialogRef.close).toHaveBeenCalledWith(expect.objectContaining({ id: '1', action: 'Edit' }));
    });

    it('should emit delete action after confirmation', () => {
        component.onDelete();

        expect(mockFdDialogService.open).toHaveBeenCalled();
        expect(mockDialogRef.close).toHaveBeenCalledWith(expect.objectContaining({ id: '1', action: 'Delete' }));
    });

    it('should not emit delete action when confirmation is cancelled', () => {
        mockConfirmDialogRef.afterClosed.mockReturnValueOnce(of(false));

        component.onDelete();

        expect(mockFdDialogService.open).toHaveBeenCalled();
        expect(mockDialogRef.close).not.toHaveBeenCalled();
    });

    it('should detect if user can modify (owned, no usage)', () => {
        expect(component.canModify()).toBe(true);
        expect(component.isEditDisabled()).toBe(false);
        expect(component.isDeleteDisabled()).toBe(false);
    });
});

describe('ProductDetailComponent disabled states', () => {
    it('should disable edit and delete when not owned by current user', async () => {
        const notOwnedProduct: Product = { ...mockProduct, isOwnedByCurrentUser: false };

        const notOwnedComponent = await createComponentAsync(notOwnedProduct);

        expect(notOwnedComponent.canModify()).toBe(false);
        expect(notOwnedComponent.isEditDisabled()).toBe(true);
        expect(notOwnedComponent.isDeleteDisabled()).toBe(true);
    });

    it('should disable edit and delete when product has usage', async () => {
        const usedProduct: Product = { ...mockProduct, usageCount: USED_PRODUCT_USAGE_COUNT };

        const usedComponent = await createComponentAsync(usedProduct);

        expect(usedComponent.canModify()).toBe(false);
    });

    it('should not emit edit when edit is disabled', async () => {
        const usedProduct: Product = { ...mockProduct, usageCount: USED_PRODUCT_USAGE_COUNT };

        vi.clearAllMocks();
        const usedComponent = await createComponentAsync(usedProduct);

        usedComponent.onEdit();
        expect(mockDialogRef.close).not.toHaveBeenCalled();
    });

    it('should not emit delete when delete is disabled', async () => {
        const usedProduct: Product = { ...mockProduct, usageCount: USED_PRODUCT_USAGE_COUNT };

        vi.clearAllMocks();
        const usedComponent = await createComponentAsync(usedProduct);

        usedComponent.onDelete();
        expect(mockFdDialogService.open).not.toHaveBeenCalled();
    });
});

describe('ProductDetailComponent duplicate flow', () => {
    it('should handle duplicate', () => {
        component.onDuplicate();

        expect(mockProductService.duplicate).toHaveBeenCalledWith('1');
        expect(mockDialogRef.close).toHaveBeenCalledWith(expect.objectContaining({ id: '2', action: 'Duplicate' }));
    });

    it('should ignore duplicate while request is in progress', () => {
        mockProductService.duplicate.mockReturnValueOnce(throwError(() => new Error('duplicate failed')));

        component.onDuplicate();
        expect(component.isDuplicateInProgress()).toBe(false);

        component.isDuplicateInProgress.set(true);
        component.onDuplicate();

        expect(mockProductService.duplicate).toHaveBeenCalledTimes(1);
    });
});

describe('ProductDetailComponent favorite flow', () => {
    it('should add product to favorites', () => {
        component.toggleFavorite();

        expect(mockFavoriteProductService.add).toHaveBeenCalledWith(mockProduct.id);
        expect(component.isFavorite()).toBe(true);
        expect(component.isFavoriteLoading()).toBe(false);
    });

    it('should remove product from favorites by known favorite id', async () => {
        mockFavoriteProductService.isFavorite.mockReturnValue(of(true));
        const favoriteProduct: Product = { ...mockProduct, isFavorite: true, favoriteProductId: FAVORITE_ID };
        const favoriteComponent = await createComponentAsync(favoriteProduct);

        favoriteComponent.toggleFavorite();

        expect(mockFavoriteProductService.remove).toHaveBeenCalledWith(FAVORITE_ID);
        expect(favoriteComponent.isFavorite()).toBe(false);
        expect(favoriteComponent.isFavoriteLoading()).toBe(false);
    });

    it('should remove product from favorites through fallback lookup when favorite id is missing', async () => {
        mockFavoriteProductService.isFavorite.mockReturnValue(of(true));
        const favoriteProduct: Product = { ...mockProduct, isFavorite: true, favoriteProductId: null };
        const favoriteComponent = await createComponentAsync(favoriteProduct);

        favoriteComponent.toggleFavorite();

        expect(mockFavoriteProductService.getAll).toHaveBeenCalled();
        expect(mockFavoriteProductService.remove).toHaveBeenCalledWith(FAVORITE_ID);
        expect(favoriteComponent.isFavorite()).toBe(false);
    });

    it('should reset favorite loading after add error', () => {
        mockFavoriteProductService.add.mockReturnValueOnce(throwError(() => new Error('favorite failed')));

        component.toggleFavorite();

        expect(component.isFavorite()).toBe(false);
        expect(component.isFavoriteLoading()).toBe(false);
    });

    it('should close with favorite changed result', () => {
        component.toggleFavorite();
        vi.clearAllMocks();

        component.close();

        expect(mockDialogRef.close).toHaveBeenCalledWith(
            expect.objectContaining({ id: mockProduct.id, action: 'FavoriteChanged', favoriteChanged: true }),
        );
    });
});

describe('ProductDetailComponent metadata', () => {
    it('should show warning message when product is not owned', async () => {
        const notOwnedProduct: Product = { ...mockProduct, isOwnedByCurrentUser: false };

        const notOwnedComponent = await createComponentAsync(notOwnedProduct);

        expect(notOwnedComponent.warningMessage()).toBe('PRODUCT_DETAIL.WARNING_NOT_OWNER');
    });

    it('should have no warning message when product can be modified', () => {
        expect(component.warningMessage()).toBeNull();
    });
});
