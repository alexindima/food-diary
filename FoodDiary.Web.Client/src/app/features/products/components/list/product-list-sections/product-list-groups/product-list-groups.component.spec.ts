import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import { MeasurementUnit, type Product, ProductType, ProductVisibility } from '../../../../models/product.data';
import type { ProductCardViewModel } from '../../product-list.types';
import { ProductListGroupsComponent } from './product-list-groups.component';

describe('ProductListGroupsComponent', () => {
    it('should render recent and all product sections', async () => {
        const recent = createProduct('recent-product', 'Recent apple');
        const all = createProduct('all-product', 'All pear');
        const { fixture } = await setupComponentAsync({
            recentItems: [createItem(recent)],
            allItems: [createItem(all)],
        });
        const element = fixture.nativeElement as HTMLElement;

        expect(element.textContent).toContain('PRODUCT_LIST.RECENT_PRODUCTS');
        expect(element.textContent).toContain('PRODUCT_LIST.ALL_PRODUCTS');
    });

    it('should emit product actions', async () => {
        const product = createProduct('product-1', 'Apple');
        const { component } = await setupComponentAsync({ allItems: [createItem(product)] });
        const openHandler = vi.fn();
        const addHandler = vi.fn();
        const favoriteHandler = vi.fn();
        component.openProduct.subscribe(openHandler);
        component.addToMeal.subscribe(addHandler);
        component.favoriteToggle.subscribe(favoriteHandler);

        component.openProduct.emit(product);
        component.addToMeal.emit(product);
        component.favoriteToggle.emit(product);

        expect(openHandler).toHaveBeenCalledWith(product);
        expect(addHandler).toHaveBeenCalledWith(product);
        expect(favoriteHandler).toHaveBeenCalledWith(product);
    });
});

type ProductListGroupsSetupOptions = {
    allItems?: ProductCardViewModel[];
    recentItems?: ProductCardViewModel[];
};

async function setupComponentAsync(
    options: ProductListGroupsSetupOptions = {},
): Promise<{ component: ProductListGroupsComponent; fixture: ComponentFixture<ProductListGroupsComponent> }> {
    await TestBed.configureTestingModule({
        imports: [ProductListGroupsComponent, TranslateModule.forRoot()],
    }).compileComponents();

    const fixture = TestBed.createComponent(ProductListGroupsComponent);
    fixture.componentRef.setInput('recentItems', options.recentItems ?? []);
    fixture.componentRef.setInput('allItems', options.allItems ?? []);
    fixture.componentRef.setInput('allProductsSectionLabelKey', 'PRODUCT_LIST.ALL_PRODUCTS');
    fixture.componentRef.setInput('favoriteLoadingIds', new Set<string>());
    fixture.detectChanges();

    return {
        component: fixture.componentInstance,
        fixture,
    };
}

function createItem(product: Product): ProductCardViewModel {
    return {
        product,
        imageUrl: product.imageUrl ?? undefined,
    };
}

function createProduct(id: string, name: string): Product {
    return {
        id,
        name,
        productType: ProductType.Fruit,
        baseUnit: MeasurementUnit.G,
        baseAmount: 100,
        defaultPortionAmount: 100,
        caloriesPerBase: 52,
        proteinsPerBase: 0.3,
        fatsPerBase: 0.2,
        carbsPerBase: 14,
        fiberPerBase: 2.4,
        alcoholPerBase: 0,
        usageCount: 0,
        visibility: ProductVisibility.Private,
        createdAt: new Date('2026-04-05T10:30:00Z'),
        isOwnedByCurrentUser: true,
        qualityScore: 80,
        qualityGrade: 'green',
    };
}
