import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import type { OpenFoodFactsProduct } from '../../../../api/open-food-facts.service';
import { ProductListOffSectionComponent } from './product-list-off-section.component';

describe('ProductListOffSectionComponent', () => {
    it('should render open food facts products', async () => {
        const product = createOffProduct();
        const { fixture } = await setupComponentAsync({ products: [product] });
        const element = fixture.nativeElement as HTMLElement;

        expect(element.textContent).toContain('PRODUCT_LIST.OFF_RESULTS');
        expect(element.textContent).toContain(product.name);
        expect(element.textContent).toContain(product.brand);
    });

    it('should emit opened open food facts products', async () => {
        const product = createOffProduct();
        const { component } = await setupComponentAsync({ products: [product] });
        const handler = vi.fn();
        component.productOpen.subscribe(handler);

        component.productOpen.emit(product);

        expect(handler).toHaveBeenCalledWith(product);
    });

    it('should render loading state without products', async () => {
        const { fixture } = await setupComponentAsync({ isLoading: true });
        const element = fixture.nativeElement as HTMLElement;

        expect(element.textContent).toContain('PRODUCT_LIST.OFF_RESULTS');
    });
});

type ProductListOffSectionSetupOptions = {
    isLoading?: boolean;
    products?: OpenFoodFactsProduct[];
};

async function setupComponentAsync(
    options: ProductListOffSectionSetupOptions = {},
): Promise<{ component: ProductListOffSectionComponent; fixture: ComponentFixture<ProductListOffSectionComponent> }> {
    await TestBed.configureTestingModule({
        imports: [ProductListOffSectionComponent, TranslateModule.forRoot()],
    }).compileComponents();

    const fixture = TestBed.createComponent(ProductListOffSectionComponent);
    fixture.componentRef.setInput('products', options.products ?? []);
    fixture.componentRef.setInput('isLoading', options.isLoading ?? false);
    fixture.detectChanges();

    return {
        component: fixture.componentInstance,
        fixture,
    };
}

function createOffProduct(): OpenFoodFactsProduct {
    return {
        barcode: '123456789',
        name: 'Chocolate',
        brand: 'Sweet',
        imageUrl: 'https://example.test/chocolate.jpg',
        caloriesPer100G: 540,
    };
}
