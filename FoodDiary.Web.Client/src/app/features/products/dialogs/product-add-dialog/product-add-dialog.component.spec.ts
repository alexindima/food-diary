import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { MeasurementUnit, type Product, ProductType, ProductVisibility } from '../../models/product.data';
import { ProductAddDialogComponent } from './product-add-dialog.component';

const PRODUCT_CALORIES = 120;
const PRODUCT_PROTEINS = 12;
const PRODUCT_FATS = 4;
const PRODUCT_CARBS = 8;
const QUALITY_SCORE_GREEN = 80;

let fixture: ComponentFixture<ProductAddDialogComponent>;
let component: ProductAddDialogComponent;
let dialogRef: { close: ReturnType<typeof vi.fn> };

describe('ProductAddDialogComponent without initial product', () => {
    beforeEach(() => {
        setupComponent();
    });

    it('closes with saved product', () => {
        const product = createProduct();

        component.onSaved(product);

        expect(dialogRef.close).toHaveBeenCalledWith(product);
    });

    it('closes with null when cancelled without initial product', () => {
        component.onCancel();

        expect(dialogRef.close).toHaveBeenCalledWith(null);
    });
});

describe('ProductAddDialogComponent with initial product', () => {
    beforeEach(() => {
        setupComponent(createProduct({ id: 'initial-product' }));
    });

    it('returns initial product when cancelled', () => {
        component.onCancel();

        expect(dialogRef.close).toHaveBeenCalledWith(createProduct({ id: 'initial-product' }));
    });
});

function setupComponent(initialProduct?: Product): void {
    dialogRef = {
        close: vi.fn(),
    };

    TestBed.configureTestingModule({
        imports: [ProductAddDialogComponent],
        providers: [
            { provide: FdUiDialogRef, useValue: dialogRef },
            { provide: FD_UI_DIALOG_DATA, useValue: initialProduct },
        ],
    });
    TestBed.overrideComponent(ProductAddDialogComponent, {
        set: { template: '' },
    });

    fixture = TestBed.createComponent(ProductAddDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
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
