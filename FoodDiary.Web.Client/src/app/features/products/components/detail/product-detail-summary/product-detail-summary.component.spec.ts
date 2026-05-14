import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import { CHART_COLORS } from '../../../../../constants/chart-colors';
import { MeasurementUnit, type Product, ProductType, ProductVisibility } from '../../../models/product.data';
import type { ProductDetailMacroBlock } from '../product-detail-lib/product-detail-nutrition.mapper';
import { ProductDetailSummaryComponent } from './product-detail-summary.component';

const PRODUCT_CALORIES = 120;
const PRODUCT_PROTEINS = 12;
const PRODUCT_FATS = 4;
const PRODUCT_CARBS = 8;
const QUALITY_SCORE_GREEN = 80;
const MACRO_PERCENT = 50;

describe('ProductDetailSummaryComponent', () => {
    it('renders product summary, quality score, and macro blocks', () => {
        const { fixture } = setupComponent(createProduct());
        const text = getText(fixture);

        expect(text).toContain(String(PRODUCT_CALORIES));
        expect(text).toContain(String(QUALITY_SCORE_GREEN));
        expect(text).toContain('PRODUCT_TYPES.FRUIT');
        expect(text).toContain('GENERAL.NUTRIENTS.PROTEIN');
        expect(text).toContain('4600000000000');
    });

    it('keeps barcode section hidden when product has no barcode', () => {
        const { fixture } = setupComponent(createProduct({ barcode: null }));
        const element = fixture.nativeElement as HTMLElement;
        const barcodeSurface = element.querySelector('.product-detail__surface--summary[hidden]');

        expect(barcodeSurface).not.toBeNull();
    });
});

function setupComponent(product: Product): { fixture: ComponentFixture<ProductDetailSummaryComponent> } {
    TestBed.configureTestingModule({
        imports: [ProductDetailSummaryComponent, TranslateModule.forRoot()],
    });

    const fixture = TestBed.createComponent(ProductDetailSummaryComponent);
    fixture.componentRef.setInput('product', product);
    fixture.componentRef.setInput('calories', product.caloriesPerBase);
    fixture.componentRef.setInput('baseUnitKey', 'GENERAL.UNITS.G');
    fixture.componentRef.setInput('productTypeKey', 'PRODUCT_TYPES.FRUIT');
    fixture.componentRef.setInput('qualityScore', product.qualityScore);
    fixture.componentRef.setInput('qualityGrade', product.qualityGrade);
    fixture.componentRef.setInput('macroSummaryBlocks', createMacroBlocks());
    fixture.detectChanges();

    return { fixture };
}

function getText(fixture: ComponentFixture<ProductDetailSummaryComponent>): string {
    return (fixture.nativeElement as HTMLElement).textContent;
}

function createMacroBlocks(): ProductDetailMacroBlock[] {
    return [
        {
            labelKey: 'GENERAL.NUTRIENTS.PROTEIN',
            value: PRODUCT_PROTEINS,
            unitKey: 'GENERAL.UNITS.G',
            color: CHART_COLORS.proteins,
            percent: MACRO_PERCENT,
        },
    ];
}

function createProduct(overrides: Partial<Product> = {}): Product {
    return {
        id: 'product-1',
        name: 'Apple',
        barcode: '4600000000000',
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
