import { TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import { ProductType } from '../../../models/product.data';
import { ProductListFiltersDialogComponent } from './product-list-filters-dialog';
import type { ProductListFiltersDialogData } from './product-list-filters-dialog.types';

const FILTER_MIN = 50;
const FILTER_MAX = 100;
const REVERSED_MIN = 200;
const FRACTIONAL_BOUND = 50.5;
type CalorieValue = number | string | null;
const INVALID_BOUNDS: Array<[CalorieValue, CalorieValue, string]> = [
    [-1, null, 'FILTER_CALORIES_INVALID'],
    ['invalid', null, 'FILTER_CALORIES_INVALID'],
    [Infinity, null, 'FILTER_CALORIES_INVALID'],
    [REVERSED_MIN, FILTER_MAX, 'FILTER_CALORIES_ORDER'],
];
const VALID_BOUNDS: Array<[CalorieValue, CalorieValue]> = [
    [0, 0],
    [null, FILTER_MAX],
    [FILTER_MAX, null],
    ['', ' '],
    [FRACTIONAL_BOUND, FRACTIONAL_BOUND],
];

describe('ProductListFiltersDialogComponent', () => {
    let component: ProductListFiltersDialogComponent;
    let dialogRefSpy: { close: ReturnType<typeof vi.fn> };

    const defaultData: ProductListFiltersDialogData = {
        onlyMine: false,
        productTypes: [ProductType.Meat, ProductType.Fruit],
        caloriesFrom: null,
        caloriesTo: null,
        hasImage: null,
    };

    function createComponent(data: ProductListFiltersDialogData = defaultData): void {
        dialogRefSpy = { close: vi.fn() };

        TestBed.configureTestingModule({
            imports: [ProductListFiltersDialogComponent],
            providers: [
                provideTranslateTesting(),
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
        createComponent({ ...defaultData, onlyMine: true, productTypes: [ProductType.Dairy] });

        expect(component['visibilityValue']).toBe('mine');
        expect(component['selectedTypeValues']()).toEqual([ProductType.Dairy]);
    });

    it('should update product type selection', () => {
        createComponent();

        component['onSelectedTypesChange']([ProductType.Fruit, ProductType.Grain]);

        expect(component['selectedTypeValues']()).toEqual([ProductType.Fruit, ProductType.Grain]);
    });

    it('should apply filters on submit', () => {
        createComponent({ ...defaultData, onlyMine: false, productTypes: [] });

        component['onVisibilityChange']('mine');
        component['onSelectedTypesChange']([ProductType.Seafood]);
        component['onApply']();

        const result = dialogRefSpy.close.mock.calls[0]?.[0] as ProductListFiltersDialogData | undefined;

        expect(result?.onlyMine).toBe(true);
        expect(result?.productTypes).toContain(ProductType.Seafood);
    });

    it.each(INVALID_BOUNDS)('retains an invalid calories draft %s/%s and permits correction', (from, to, error) => {
        createComponent();
        component['caloriesFromValue'] = from;
        component['caloriesToValue'] = to;
        component['onApply']();
        expect(dialogRefSpy.close).not.toHaveBeenCalled();
        expect(component['caloriesError']()).toBe(`PRODUCT_LIST.${error}`);
        expect(component['caloriesFromValue']).toBe(from);
        component['caloriesFromValue'] = FILTER_MIN;
        component['caloriesToValue'] = FILTER_MAX;
        component['onApply']();
        expect(dialogRefSpy.close).toHaveBeenCalledWith(expect.objectContaining({ caloriesFrom: FILTER_MIN, caloriesTo: FILTER_MAX }));
    });

    it.each(VALID_BOUNDS)('accepts valid inclusive or open bounds %s/%s', (from, to) => {
        createComponent();
        component['caloriesFromValue'] = from;
        component['caloriesToValue'] = to;
        component['onApply']();
        expect(dialogRefSpy.close).toHaveBeenCalledOnce();
        expect(component['caloriesError']()).toBe('');
    });

    it('should cancel without applying', () => {
        createComponent();

        component['onCancel']();

        expect(dialogRefSpy.close).toHaveBeenCalledWith(null);
    });
});
