import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { MeasurementUnit, ProductType, ProductVisibility } from '../../../models/product.data';
import { createProductForm } from '../product-manage-lib/product-manage-form.mapper';
import type { ProductNameSuggestion } from '../product-manage-lib/product-name-search.types';
import { ProductBasicInfoComponent } from './product-basic-info.component';

const INVALID_AMOUNT = 0;

let fixture: ComponentFixture<ProductBasicInfoComponent>;
let component: ProductBasicInfoComponent;
let languageChanges$: Subject<unknown>;

beforeEach(() => {
    languageChanges$ = new Subject<unknown>();

    TestBed.configureTestingModule({
        imports: [ProductBasicInfoComponent],
        providers: [
            {
                provide: TranslateService,
                useValue: {
                    instant: vi.fn((key: string, params?: { min?: number }) => (params?.min === undefined ? key : `${key}:${params.min}`)),
                    onLangChange: languageChanges$.asObservable(),
                },
            },
        ],
    });
    TestBed.overrideComponent(ProductBasicInfoComponent, {
        set: { template: '' },
    });

    fixture = TestBed.createComponent(ProductBasicInfoComponent);
    component = fixture.componentInstance;
});

describe('ProductBasicInfoComponent options', () => {
    it('builds select options inside the component', () => {
        setRequiredInputs();
        fixture.detectChanges();

        expect(component.unitOptions().map(option => option.value)).toEqual(Object.values(MeasurementUnit));
        expect(component.productTypeOptions().map(option => option.value)).toEqual(Object.values(ProductType));
        expect(component.visibilityOptions().map(option => option.value)).toEqual(Object.values(ProductVisibility));
    });

    it('rebuilds select options when language changes', () => {
        setRequiredInputs();
        fixture.detectChanges();
        const firstOptions = component.unitOptions();

        languageChanges$.next(null);

        expect(component.unitOptions()).not.toBe(firstOptions);
    });
});

describe('ProductBasicInfoComponent field errors', () => {
    it('returns required error only after the control becomes touched or dirty', () => {
        const form = createProductForm();
        setRequiredInputs(form);
        fixture.detectChanges();

        expect(component.fieldErrors().name).toBeNull();

        form.controls.name.markAsTouched();
        form.controls.name.updateValueAndValidity();

        expect(component.fieldErrors().name).toBe('FORM_ERRORS.REQUIRED');
    });

    it('returns min amount error with validator metadata', () => {
        const form = createProductForm();
        form.controls.defaultPortionAmount.setValue(INVALID_AMOUNT);
        form.controls.defaultPortionAmount.markAsTouched();
        setRequiredInputs(form);
        fixture.detectChanges();

        expect(component.fieldErrors().defaultPortionAmount).toBe('FORM_ERRORS.INVALID_MIN_AMOUNT_MUST_BE_MORE_ZERO:0.001');
    });
});

describe('ProductBasicInfoComponent name suggestions', () => {
    it('emits selected product name suggestion when option data has a name', () => {
        const suggestion: ProductNameSuggestion = {
            source: 'openFoodFacts',
            name: 'Apple',
            barcode: '4600000000000',
        };
        const selected: ProductNameSuggestion[] = [];
        setRequiredInputs();
        fixture.detectChanges();
        component.nameSuggestionSelected.subscribe(value => {
            selected.push(value);
        });

        component.onNameOptionSelected({
            id: 'apple',
            value: 'Apple',
            label: 'Apple',
            data: suggestion,
        });

        expect(selected).toEqual([suggestion]);
    });

    it('ignores autocomplete options without product suggestion data', () => {
        const selected: ProductNameSuggestion[] = [];
        setRequiredInputs();
        fixture.detectChanges();
        component.nameSuggestionSelected.subscribe(value => {
            selected.push(value);
        });

        component.onNameOptionSelected({
            id: 'plain',
            value: 'Plain',
            label: 'Plain',
            data: { label: 'Plain' },
        });

        expect(selected).toEqual([]);
    });
});

function setRequiredInputs(form = createProductForm()): void {
    fixture.componentRef.setInput('formGroup', form);
    fixture.componentRef.setInput('nameOptions', []);
    fixture.componentRef.setInput('isNameSearchLoading', false);
}
