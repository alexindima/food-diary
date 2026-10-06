import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { MeasurementUnit, type Product, ProductType, ProductVisibility } from '../../../../shared/models/product.data';
import { ProductListDialogContentComponent } from './product-list-dialog-content';

const PRODUCT_CALORIES = 120;
const PRODUCT_PROTEINS = 12;
const PRODUCT_FATS = 4;
const PRODUCT_CARBS = 8;
const QUALITY_SCORE_GREEN = 80;

describe('ProductListDialogContentComponent', () => {
    it.each([MeasurementUnit.G, MeasurementUnit.ML, MeasurementUnit.PCS])('renders nutrition per %s', baseUnit => {
        const { fixture } = setupComponent([{ product: createProduct({ baseUnit }), imageUrl: undefined }]);

        expect(getText(fixture)).toContain(`PRODUCT_AMOUNT_UNITS_SHORT.${baseUnit}`);
        if (baseUnit !== MeasurementUnit.G) {
            expect(getText(fixture)).not.toContain('PRODUCT_AMOUNT_UNITS_SHORT.G');
        }
    });

    it('renders product rows and emits selected product', () => {
        const { fixture, component } = setupComponent([
            {
                product: createProduct(),
                imageUrl: 'https://example.test/apple.jpg',
            },
        ]);
        const selected: Product[] = [];
        component['productSelected'].subscribe(product => {
            selected.push(product);
        });

        selectionButton(fixture).click();

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

describe('Product selection accessibility', () => {
    it('exposes one named native button per card without nested interactive controls', () => {
        const { fixture } = setupComponent([{ product: createProduct(), imageUrl: undefined }]);
        const host = fixture.nativeElement as HTMLElement;
        const card = host.querySelector('.product-select__item');
        const button = selectionButton(fixture);

        expect(card?.tagName).toBe('DIV');
        expect(card?.getAttribute('role')).toBe('listitem');
        expect(card?.querySelectorAll('button')).toHaveLength(1);
        expect(button.querySelector('button, a[href], [tabindex="0"]')).toBeNull();
        expect(button.type).toBe('button');
        expect(button.getAttribute('aria-label')).toBe('COMMON.ADD: Apple');
        button.focus();
        expect(document.activeElement).toBe(button);
    });

    it('emits selection once when the primary action icon is clicked', () => {
        const { fixture, component } = setupComponent([{ product: createProduct(), imageUrl: undefined }]);
        const selected: Product[] = [];
        component.productSelected.subscribe(product => selected.push(product));
        const icon = selectionButton(fixture).querySelector('fd-ui-icon');
        if (icon === null) {
            throw new Error('Product selection action icon was not rendered');
        }

        icon.dispatchEvent(new MouseEvent('click', { bubbles: true }));

        expect(selected).toEqual([createProduct()]);
    });
});

function selectionButton(fixture: ComponentFixture<ProductListDialogContentComponent>): HTMLButtonElement {
    const button = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('.product-select__action button');
    if (button === null) {
        throw new Error('Product selection action was not rendered');
    }
    return button;
}

function setupComponent(
    items: ReadonlyArray<{ product: Product; imageUrl: string | undefined }>,
    isLoading = false,
): { fixture: ComponentFixture<ProductListDialogContentComponent>; component: ProductListDialogContentComponent } {
    TestBed.configureTestingModule({
        imports: [ProductListDialogContentComponent],
        providers: [provideTranslateTesting()],
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
