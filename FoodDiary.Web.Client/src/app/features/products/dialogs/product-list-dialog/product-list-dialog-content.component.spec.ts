import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import { MeasurementUnit, type Product, ProductType, ProductVisibility } from '../../models/product.data';
import { ProductListDialogContentComponent } from './product-list-dialog-content.component';

const PRODUCT_CALORIES = 120;
const PRODUCT_PROTEINS = 12;
const PRODUCT_FATS = 4;
const PRODUCT_CARBS = 8;
const QUALITY_SCORE_GREEN = 80;

describe('ProductListDialogContentComponent', () => {
    it('renders product rows and emits selected product', () => {
        const { fixture, component } = setupComponent([
            {
                product: createProduct(),
                imageUrl: 'https://example.test/apple.jpg',
            },
        ]);
        const selected: Product[] = [];
        component.productSelected.subscribe(product => {
            selected.push(product);
        });

        fixture.debugElement.query(By.css('.product-select__item')).triggerEventHandler('click');

        expect(getText(fixture)).toContain('Garden');
        expect(getText(fixture)).toContain('·');
        expect(selected).toEqual([createProduct()]);
    });

    it('renders no-results state when loaded list is empty', () => {
        const { fixture } = setupComponent([]);

        expect(fixture.debugElement.query(By.css('.product-select__no-results'))).not.toBeNull();
        expect(getText(fixture)).toContain('PRODUCT_LIST.NO_PRODUCTS_FOUND');
    });

    it('renders loader when loading', () => {
        const { fixture } = setupComponent([], true);

        expect(fixture.debugElement.query(By.css('.product-select__loader'))).not.toBeNull();
    });
});

function setupComponent(
    items: ReadonlyArray<{ product: Product; imageUrl: string | undefined }>,
    isLoading = false,
): { fixture: ComponentFixture<ProductListDialogContentComponent>; component: ProductListDialogContentComponent } {
    TestBed.configureTestingModule({
        imports: [ProductListDialogContentComponent, TranslateModule.forRoot()],
    });

    const fixture = TestBed.createComponent(ProductListDialogContentComponent);
    const component = fixture.componentInstance;
    fixture.componentRef.setInput('isLoading', isLoading);
    fixture.componentRef.setInput('items', items);
    fixture.detectChanges();

    return { fixture, component };
}

function getText(fixture: ComponentFixture<ProductListDialogContentComponent>): string {
    return (fixture.nativeElement as HTMLElement).textContent;
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
