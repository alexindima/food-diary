import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { type FieldTree, form, min, required } from '@angular/forms/signals';
import { TranslateService } from '@ngx-translate/core';
import { Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { DEFAULT_NUTRITION_BASE_AMOUNT } from '../../../../../shared/lib/nutrition.constants';
import { MeasurementUnit } from '../../../models/product.data';
import { createProductForm } from '../product-manage-lib/product-manage-form.mapper';
import type { ProductFormValues } from '../product-manage-lib/product-manage-form.types';
import { ProductNutritionEditorComponent } from './product-nutrition-editor';

const PIECE_BASE_AMOUNT = 1;
const PRODUCT_CALORIES = 10;
const PRODUCT_PROTEINS = 20;
const PRODUCT_FATS = 10;
const PRODUCT_CARBS = 30;

let fixture: ComponentFixture<ProductNutritionEditorComponent>;
let component: ProductNutritionEditorComponent;
let languageChanges$: Subject<unknown>;

beforeEach(() => {
    languageChanges$ = new Subject<unknown>();

    TestBed.configureTestingModule({
        imports: [ProductNutritionEditorComponent],
        providers: [
            {
                provide: TranslateService,
                useValue: {
                    instant: vi.fn((key: string, params?: { amount?: number; unit?: string }) =>
                        params === undefined ? key : `${key}:${params.amount}:${params.unit}`,
                    ),
                    onLangChange: languageChanges$.asObservable(),
                },
            },
        ],
    });
    TestBed.overrideComponent(ProductNutritionEditorComponent, {
        set: { template: '' },
    });

    fixture = TestBed.createComponent(ProductNutritionEditorComponent);
    component = fixture.componentInstance;
});

describe('ProductNutritionEditorComponent options', () => {
    it('builds base and portion mode options from current base unit', () => {
        const productForm = createProductSignalForm();
        setRequiredInputs(productForm);
        fixture.detectChanges();

        expect(component['nutritionModeOptions']()).toEqual([
            {
                value: 'base',
                label: `PRODUCT_MANAGE.NUTRITION_MODE.BASE:${DEFAULT_NUTRITION_BASE_AMOUNT}:GENERAL.UNITS.G`,
            },
            {
                value: 'portion',
                label: 'PRODUCT_MANAGE.NUTRITION_MODE.PORTION',
            },
        ]);
    });

    it('updates base amount label when base unit changes to pieces', () => {
        const productForm = createProductSignalForm();
        setRequiredInputs(productForm);
        fixture.detectChanges();

        productForm.baseUnit().value.set(MeasurementUnit.PCS);
        fixture.detectChanges();

        expect(component['nutritionModeOptions']()[0]).toEqual({
            value: 'base',
            label: `PRODUCT_MANAGE.NUTRITION_MODE.BASE:${PIECE_BASE_AMOUNT}:GENERAL.UNITS.PCS`,
        });
    });
});

describe('ProductNutritionEditorComponent nutrition signals', () => {
    it('calculates macro distribution and calorie mismatch warning from form values', () => {
        const productForm = createProductSignalForm({
            ...createProductForm(),
            caloriesPerBase: PRODUCT_CALORIES,
            proteinsPerBase: PRODUCT_PROTEINS,
            fatsPerBase: PRODUCT_FATS,
            carbsPerBase: PRODUCT_CARBS,
        });
        setRequiredInputs(productForm);
        fixture.detectChanges();

        expect(component['macroBarState']().isEmpty).toBe(false);
        expect(component['macroBarState']().segments.map(segment => segment.key)).toEqual(['proteins', 'fats', 'carbs']);
        expect(component['nutritionWarning']()).toEqual({
            expectedCalories: 290,
            actualCalories: PRODUCT_CALORIES,
        });
    });

    it('updates macro distribution when nutrition values change', () => {
        const productForm = createProductSignalForm();
        setRequiredInputs(productForm);
        fixture.detectChanges();

        expect(component['macroBarState']().isEmpty).toBe(true);

        productForm.proteinsPerBase().value.set(PRODUCT_PROTEINS);
        fixture.detectChanges();

        expect(component['macroBarState']()).toEqual({
            isEmpty: false,
            segments: [{ key: 'proteins', percent: 100 }],
        });
    });
});

describe('ProductNutritionEditorComponent validation messages', () => {
    it('returns calories required error when calories control is invalid and touched', () => {
        const productForm = createProductSignalForm();
        setRequiredInputs(productForm);
        fixture.detectChanges();

        productForm.caloriesPerBase().markAsTouched();

        expect(component['caloriesError']()).toBe('PRODUCT_MANAGE.NUTRITION_ERRORS.CALORIES_REQUIRED');
    });

    it('returns macros required error when macro controls are empty and touched', () => {
        const productForm = createProductSignalForm();
        setRequiredInputs(productForm);
        fixture.detectChanges();

        productForm.proteinsPerBase().markAsTouched();
        productForm.fatsPerBase().markAsTouched();
        productForm.carbsPerBase().markAsTouched();
        productForm.alcoholPerBase().markAsTouched();

        expect(component['macrosError']()).toBe('PRODUCT_MANAGE.NUTRITION_ERRORS.MACROS_REQUIRED');
    });
});

function setRequiredInputs(productForm = createProductSignalForm()): void {
    fixture.componentRef.setInput('form', productForm);
    fixture.componentRef.setInput('nutritionMode', 'base');
}

function createProductSignalForm(model = createProductForm()): FieldTree<ProductFormValues> {
    const formModel = signal(model);
    return TestBed.runInInjectionContext(() =>
        form(formModel, path => {
            required(path.caloriesPerBase);
            min(path.caloriesPerBase, 0);
            min(path.proteinsPerBase, 0);
            min(path.fatsPerBase, 0);
            min(path.carbsPerBase, 0);
            min(path.alcoholPerBase, 0);
        }),
    );
}
