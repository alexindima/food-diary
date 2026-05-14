import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { MeasurementUnit, type Product, ProductType, ProductVisibility } from '../../../models/product.data';
import { ProductEditComponent } from './product-edit.component';

const PRODUCT_CALORIES = 120;
const PRODUCT_PROTEINS = 12;
const PRODUCT_FATS = 4;
const PRODUCT_CARBS = 8;
const QUALITY_SCORE_GREEN = 80;

describe('ProductEditComponent', () => {
    it('defaults product input to null', () => {
        const { component } = setupComponent();

        expect(component.product()).toBeNull();
    });

    it('accepts product input for edit form wrapper', () => {
        const product = createProduct();
        const { fixture, component } = setupComponent();

        fixture.componentRef.setInput('product', product);
        fixture.detectChanges();

        expect(component.product()).toEqual(product);
    });
});

function setupComponent(): { fixture: ComponentFixture<ProductEditComponent>; component: ProductEditComponent } {
    TestBed.configureTestingModule({
        imports: [ProductEditComponent],
    });
    TestBed.overrideComponent(ProductEditComponent, {
        set: { template: '' },
    });

    const fixture = TestBed.createComponent(ProductEditComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    return { fixture, component };
}

function createProduct(): Product {
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
    };
}
