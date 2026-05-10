import { TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TranslateModule } from '@ngx-translate/core';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { describe, expect, it, vi } from 'vitest';

import { ProductType } from '../../models/product.data';
import { ProductListFiltersDialogComponent, type ProductListFiltersDialogData } from './product-list-filters-dialog.component';

describe('ProductListFiltersDialogComponent', () => {
    let component: ProductListFiltersDialogComponent;
    let dialogRefSpy: { close: ReturnType<typeof vi.fn> };

    const defaultData: ProductListFiltersDialogData = {
        onlyMine: false,
        productTypes: [ProductType.Meat, ProductType.Fruit],
    };

    function createComponent(data: ProductListFiltersDialogData = defaultData): void {
        dialogRefSpy = { close: vi.fn() };

        TestBed.configureTestingModule({
            imports: [ProductListFiltersDialogComponent, TranslateModule.forRoot()],
            providers: [
                provideNoopAnimations(),
                { provide: FdUiDialogRef, useValue: dialogRefSpy as Partial<FdUiDialogRef<ProductListFiltersDialogComponent>> },
                { provide: FD_UI_DIALOG_DATA, useValue: data },
            ],
        });

        const fixture = TestBed.createComponent(ProductListFiltersDialogComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    }

    it('should create', () => {
        createComponent();
        expect(component).toBeTruthy();
    });

    it('should initialize with provided filter data', () => {
        createComponent({ onlyMine: true, productTypes: [ProductType.Dairy] });

        expect(component.visibilityValue).toBe('mine');
        expect(component.isTypeSelected(ProductType.Dairy)).toBe(true);
        expect(component.isTypeSelected(ProductType.Meat)).toBe(false);
    });

    it('should toggle product type selection', () => {
        createComponent();

        expect(component.isTypeSelected(ProductType.Meat)).toBe(true);

        component.toggleType(ProductType.Meat);
        expect(component.isTypeSelected(ProductType.Meat)).toBe(false);

        component.toggleType(ProductType.Grain);
        expect(component.isTypeSelected(ProductType.Grain)).toBe(true);
    });

    it('should apply filters on submit', () => {
        createComponent({ onlyMine: false, productTypes: [] });

        component.onVisibilityChange('mine');
        component.toggleType(ProductType.Seafood);
        component.onApply();

        const result = dialogRefSpy.close.mock.calls[0]?.[0] as ProductListFiltersDialogData | undefined;

        expect(result?.onlyMine).toBe(true);
        expect(result?.productTypes).toContain(ProductType.Seafood);
    });

    it('should cancel without applying', () => {
        createComponent();

        component.onCancel();

        expect(dialogRefSpy.close).toHaveBeenCalledWith(null);
    });
});
