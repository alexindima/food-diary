import { Location } from '@angular/common';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { FdTourService } from 'fd-tour';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { EMPTY, type Observable, of, Subject } from 'rxjs';
import { beforeEach, describe, expect, it, type Mock, onTestFinished, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import { NavigationService } from '../../../../../services/navigation.service';
import { ProductVisibility } from '../../../../../shared/models/product.data';
import { ProductService } from '../../../api/product.service';
import { ProductManageFormComponent } from '../../../components/manage/product-manage-form/product-manage-form';
import { ProductExternalFoodFacade } from '../../../lib/manage/product-external-food.facade';
import { ProductManageFacade } from '../../../lib/product-manage.facade';
import type { OpenFoodFactsProduct } from '../../../models/open-food-facts.data';
import { ProductAddComponent } from './product-add';

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

        expect(component['prefill']()).toEqual({
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

        expect(component['prefill']()).toEqual({
            barcode: '4600000000000',
            offProduct: null,
        });
    });

    it('falls back to empty prefill when navigation state is missing', () => {
        router.currentNavigation.mockReturnValue(null);
        setupComponent();

        expect(component['prefill']()).toEqual({
            barcode: null,
            offProduct: null,
        });
    });
});

describe('Product Add with restored browser history', () => {
    it('keeps a barcode and known zero nutrition, and requires the user to supply the missing name', async () => {
        const entry = { ...createOpenFoodFactsProduct(), name: '', caloriesPer100G: 0 };
        const { editor, host, searchByBarcode, submitProduct } = renderFromHistory({ offProduct: entry });
        const name = editorInput(host, 'fd-ui-autocomplete input');
        const save = host.querySelector('button[type="submit"]');
        if (!(save instanceof HTMLButtonElement)) {
            throw new Error('Expected the product save button');
        }

        expect(name.value).toBe('');
        expect(editorInput(host, 'input[aria-label="PRODUCT_MANAGE.BARCODE"]').value).toBe(entry.barcode);
        expect(editorInput(host, 'input[aria-label="PRODUCT_MANAGE.BRAND"]').value).toBe(entry.brand);
        save.click();
        await editor.whenStable();
        expect(submitProduct).not.toHaveBeenCalled();

        name.value = 'Sparkling water';
        name.dispatchEvent(new Event('input', { bubbles: true }));
        editor.detectChanges();
        save.click();
        await editor.whenStable();

        expect(searchByBarcode).not.toHaveBeenCalled();
        expect(submitProduct).toHaveBeenCalledOnce();
        const [existing, request] = submitProduct.mock.calls[0];
        expect(existing).toBeNull();
        expect(request).toMatchObject({
            name: 'Sparkling water',
            barcode: entry.barcode,
            brand: entry.brand,
            caloriesPerBase: 0,
            visibility: ProductVisibility.Private,
        });
    });

    it('looks up the preserved barcode when the saved Open Food Facts entry is malformed', async () => {
        const source = createOpenFoodFactsProduct();
        const resolved = new Subject<OpenFoodFactsProduct>();
        const { editor, host, searchByBarcode } = renderFromHistory(
            { barcode: source.barcode, offProduct: { barcode: source.barcode } },
            resolved,
        );

        expect(searchByBarcode).toHaveBeenCalledExactlyOnceWith(source.barcode);
        expect(editorInput(host, 'fd-ui-autocomplete input').value).toBe('');
        resolved.next(source);
        resolved.complete();
        await editor.whenStable();
        editor.detectChanges();

        expect(editorInput(host, 'fd-ui-autocomplete input').value).toBe(source.name);
        expect(editorInput(host, 'input[aria-label="PRODUCT_MANAGE.BARCODE"]').value).toBe(source.barcode);
        expect(editorInput(host, 'input[aria-label="PRODUCT_MANAGE.BRAND"]').value).toBe(source.brand);
    });

    it('allows a pristine restored form to leave and respects the user’s decision for a changed name', async () => {
        const { editor, host, confirmDiscard } = renderFromHistory({ offProduct: createOpenFoodFactsProduct() });

        await expect(editor.componentInstance.canLeaveAsync()).resolves.toBe(true);
        expect(confirmDiscard).not.toHaveBeenCalled();

        const name = editorInput(host, 'fd-ui-autocomplete input');
        name.value = 'Green apple';
        name.dispatchEvent(new Event('input', { bubbles: true }));
        editor.detectChanges();
        confirmDiscard.mockResolvedValueOnce(false).mockResolvedValueOnce(true);

        await expect(editor.componentInstance.canLeaveAsync()).resolves.toBe(false);
        expect(name.value).toBe('Green apple');
        await expect(editor.componentInstance.canLeaveAsync()).resolves.toBe(true);
        expect(confirmDiscard).toHaveBeenCalledTimes(2);
    });
});

type RestoredProductEditor = {
    editor: ComponentFixture<ProductAddComponent>;
    host: HTMLElement;
    searchByBarcode: Mock<ProductExternalFoodFacade['searchByBarcode']>;
    submitProduct: Mock<ProductManageFacade['submitProductAsync']>;
    confirmDiscard: Mock<ProductManageFacade['confirmDiscardChangesAsync']>;
};

function renderFromHistory(
    state: Record<string, unknown>,
    lookupResult: Observable<OpenFoodFactsProduct | null> = of(null),
): RestoredProductEditor {
    const searchByBarcode = vi.fn<ProductExternalFoodFacade['searchByBarcode']>().mockReturnValue(lookupResult);
    const submitProduct = vi.fn<ProductManageFacade['submitProductAsync']>().mockResolvedValue({ product: null, error: null });
    const confirmDiscard = vi.fn<ProductManageFacade['confirmDiscardChangesAsync']>();
    TestBed.configureTestingModule({
        imports: [ProductAddComponent],
        providers: [
            provideRouter([]),
            provideTranslateTesting(),
            { provide: ProductService, useValue: { searchSuggestions: vi.fn().mockReturnValue(of([])) } },
            { provide: ProductExternalFoodFacade, useValue: { searchByBarcode } },
            {
                provide: ProductManageFacade,
                useValue: { committed$: EMPTY, submitProductAsync: submitProduct, confirmDiscardChangesAsync: confirmDiscard },
            },
            { provide: NavigationService, useValue: {} },
            { provide: FdUiDialogService, useValue: {} },
            { provide: FdTourService, useValue: {} },
        ],
    }).overrideComponent(ProductManageFormComponent, { remove: { providers: [ProductManageFacade] } });

    const location = TestBed.inject(Location);
    const path = location.path();
    const previousState = location.getState();
    location.replaceState(path, '', state);
    onTestFinished(() => {
        location.replaceState(path, '', previousState);
    });
    const editor = TestBed.createComponent(ProductAddComponent);
    editor.detectChanges();
    return { editor, host: editor.nativeElement as HTMLElement, searchByBarcode, submitProduct, confirmDiscard };
}

function editorInput(host: HTMLElement, selector: string): HTMLInputElement {
    const input = host.querySelector(selector);
    if (!(input instanceof HTMLInputElement)) {
        throw new Error(`Expected a product input matching ${selector}`);
    }
    return input;
}

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
