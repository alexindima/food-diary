import { TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { describe, expect, it, vi } from 'vitest';

import { ProductType } from '../../../models/product.data';
import { ProductListFiltersDialogComponent } from './product-list-filters-dialog.component';
import type { ProductListFiltersDialogData } from './product-list-filters-dialog.types';

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
        expect(component.selectedTypeValues()).toEqual([ProductType.Dairy]);
    });

    it('should update product type selection', () => {
        createComponent();

        component.onSelectedTypesChange([ProductType.Fruit, ProductType.Grain]);

        expect(component.selectedTypeValues()).toEqual([ProductType.Fruit, ProductType.Grain]);
    });

    it('should apply filters on submit', () => {
        createComponent({ onlyMine: false, productTypes: [] });

        component.onVisibilityChange('mine');
        component.onSelectedTypesChange([ProductType.Seafood]);
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
