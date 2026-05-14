import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import type { OpenFoodFactsProduct } from '../../../api/open-food-facts.service';
import { ProductAddComponent } from './product-add.component';

const PRODUCT_CALORIES = 120;

let fixture: ComponentFixture<ProductAddComponent>;
let component: ProductAddComponent;
let router: { currentNavigation: ReturnType<typeof vi.fn> };

beforeEach(() => {
    router = {
        currentNavigation: vi.fn(),
    };
});

describe('ProductAddComponent prefill', () => {
    it('reads barcode and Open Food Facts product from current navigation state', () => {
        const offProduct = createOpenFoodFactsProduct();
        router.currentNavigation.mockReturnValue({
            extras: {
                state: {
                    barcode: '4600000000000',
                    offProduct,
                },
            },
        });
        setupComponent();

        expect(component.prefill()).toEqual({
            barcode: '4600000000000',
            offProduct,
        });
    });

    it('ignores malformed Open Food Facts product state', () => {
        router.currentNavigation.mockReturnValue({
            extras: {
                state: {
                    barcode: '4600000000000',
                    offProduct: {
                        barcode: '4600000000000',
                    },
                },
            },
        });
        setupComponent();

        expect(component.prefill()).toEqual({
            barcode: '4600000000000',
            offProduct: null,
        });
    });

    it('falls back to empty prefill when navigation state is missing', () => {
        router.currentNavigation.mockReturnValue(null);
        setupComponent();

        expect(component.prefill()).toEqual({
            barcode: null,
            offProduct: null,
        });
    });
});

function setupComponent(): void {
    TestBed.configureTestingModule({
        imports: [ProductAddComponent],
        providers: [{ provide: Router, useValue: router }],
    });
    TestBed.overrideComponent(ProductAddComponent, {
        set: { template: '' },
    });

    fixture = TestBed.createComponent(ProductAddComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
}

function createOpenFoodFactsProduct(): OpenFoodFactsProduct {
    return {
        barcode: '4600000000000',
        name: 'Apple',
        brand: 'Garden',
        category: 'Fruit',
        imageUrl: null,
        caloriesPer100G: PRODUCT_CALORIES,
    };
}
