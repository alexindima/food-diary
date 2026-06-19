import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { type FieldTree, form, min, required, validate } from '@angular/forms/signals';
import { TranslateService } from '@ngx-translate/core';
import { Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import {
    getProductMaxAmountForUnit,
    PRODUCT_BRAND_MAX_LENGTH,
    PRODUCT_MAX_PIECE_AMOUNT,
    PRODUCT_MAX_WEIGHT_OR_VOLUME_AMOUNT,
    PRODUCT_MIN_AMOUNT,
} from '../../../lib/product-manage.constants';
import { MeasurementUnit, ProductType, ProductVisibility } from '../../../models/product.data';
import { createProductForm } from '../product-manage-lib/product-manage-form.mapper';
import type { ProductFormValues } from '../product-manage-lib/product-manage-form.types';
import type { ProductNameSuggestion } from '../product-manage-lib/product-name-search.types';
import { ProductBasicInfoComponent } from './product-basic-info';

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
                    instant: vi.fn((key: string, params?: { max?: number; min?: number; requiredLength?: number }) => {
                        if (params?.min !== undefined) {
                            return `${key}:${params.min}`;
                        }
                        if (params?.max !== undefined) {
                            return `${key}:${params.max}`;
                        }
                        if (params?.requiredLength !== undefined) {
                            return `${key}:${params.requiredLength}`;
                        }

                        return key;
                    }),
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

        expect(component['unitOptions']().map(option => option.value)).toEqual(Object.values(MeasurementUnit));
        expect(component['productTypeOptions']().map(option => option.value)).toEqual(Object.values(ProductType));
        expect(component['visibilityOptions']().map(option => option.value)).toEqual(Object.values(ProductVisibility));
    });

    it('rebuilds select options when language changes', () => {
        setRequiredInputs();
        fixture.detectChanges();
        const firstOptions = component['unitOptions']();

        languageChanges$.next(null);

        expect(component['unitOptions']()).not.toBe(firstOptions);
    });
});

describe('ProductBasicInfoComponent field errors', () => {
    it('returns required error only after the control becomes touched or dirty', () => {
        const productForm = createProductSignalForm();
        setRequiredInputs(productForm);
        fixture.detectChanges();

        expect(component['fieldErrors']().name).toBeNull();

        productForm.name().markAsTouched();
        fixture.detectChanges();

        expect(component['fieldErrors']().name).toBe('FORM_ERRORS.REQUIRED');
    });

    it('returns min amount error with validator metadata', () => {
        const productForm = createProductSignalForm(createProductForm());
        productForm.defaultPortionAmount().value.set(INVALID_AMOUNT);
        productForm.defaultPortionAmount().markAsTouched();
        setRequiredInputs(productForm);
        fixture.detectChanges();

        expect(component['fieldErrors']().defaultPortionAmount).toBe('FORM_ERRORS.INVALID_MIN_AMOUNT_MUST_BE_MORE_ZERO:0.001');
    });

    it('returns max length and max amount errors with validator metadata', () => {
        const productForm = createProductSignalForm(createProductForm());
        productForm.brand().value.set('x'.repeat(PRODUCT_BRAND_MAX_LENGTH + 1));
        productForm.brand().markAsTouched();
        productForm.defaultPortionAmount().value.set(PRODUCT_MAX_WEIGHT_OR_VOLUME_AMOUNT + 1);
        productForm.defaultPortionAmount().markAsTouched();
        setRequiredInputs(productForm);
        fixture.detectChanges();

        expect(component['fieldErrors']().brand).toBe(`FORM_ERRORS.MAX_LENGTH:${PRODUCT_BRAND_MAX_LENGTH}`);
        expect(component['fieldErrors']().defaultPortionAmount).toBe(
            `FORM_ERRORS.INVALID_MAX_AMOUNT:${PRODUCT_MAX_WEIGHT_OR_VOLUME_AMOUNT}`,
        );
    });

    it('updates maximum amount from selected serving unit', () => {
        const productForm = createProductSignalForm(createProductForm());
        setRequiredInputs(productForm);
        fixture.detectChanges();

        expect(component['maxAmount']()).toBe(PRODUCT_MAX_WEIGHT_OR_VOLUME_AMOUNT);

        productForm.baseUnit().value.set(MeasurementUnit.PCS);
        fixture.detectChanges();

        expect(component['maxAmount']()).toBe(PRODUCT_MAX_PIECE_AMOUNT);
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
        component['nameSuggestionSelected'].subscribe(value => {
            selected.push(value);
        });

        component['onNameOptionSelected']({
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
        component['nameSuggestionSelected'].subscribe(value => {
            selected.push(value);
        });

        component['onNameOptionSelected']({
            id: 'plain',
            value: 'Plain',
            label: 'Plain',
            data: { label: 'Plain' },
        });

        expect(selected).toEqual([]);
    });
});

function setRequiredInputs(productForm = createProductSignalForm()): void {
    fixture.componentRef.setInput('form', productForm);
    fixture.componentRef.setInput('nameOptions', []);
    fixture.componentRef.setInput('isNameSearchLoading', false);
}

function createProductSignalForm(model = createProductForm()): FieldTree<ProductFormValues> {
    const formModel = signal(model);
    return TestBed.runInInjectionContext(() =>
        form(formModel, path => {
            required(path.name);
            validate(path.brand, ({ value }) => {
                const brand = value();
                return brand !== null && brand.length > PRODUCT_BRAND_MAX_LENGTH
                    ? { kind: 'maxLength', maxLength: PRODUCT_BRAND_MAX_LENGTH }
                    : undefined;
            });
            required(path.defaultPortionAmount);
            min(path.defaultPortionAmount, PRODUCT_MIN_AMOUNT);
            validate(path.defaultPortionAmount, ({ value }) => {
                const maximumAmount = getProductMaxAmountForUnit(formModel().baseUnit);
                return value() > maximumAmount ? { kind: 'max', max: maximumAmount } : undefined;
            });
            required(path.productType);
            required(path.baseUnit);
            required(path.visibility);
        }),
    );
}
