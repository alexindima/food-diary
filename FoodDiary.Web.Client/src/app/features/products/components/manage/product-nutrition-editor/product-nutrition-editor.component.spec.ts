import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { DEFAULT_NUTRITION_BASE_AMOUNT } from '../../../../../shared/lib/nutrition.constants';
import { MeasurementUnit } from '../../../models/product.data';
import { createProductForm } from '../product-manage-lib/product-manage-form.mapper';
import { ProductNutritionEditorComponent } from './product-nutrition-editor.component';

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
        const form = createProductForm();
        setRequiredInputs(form);
        fixture.detectChanges();

        expect(component.nutritionModeOptions()).toEqual([
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
        const form = createProductForm();
        setRequiredInputs(form);
        fixture.detectChanges();

        form.controls.baseUnit.setValue(MeasurementUnit.PCS);

        expect(component.nutritionModeOptions()[0]).toEqual({
            value: 'base',
            label: `PRODUCT_MANAGE.NUTRITION_MODE.BASE:${PIECE_BASE_AMOUNT}:GENERAL.UNITS.PCS`,
        });
    });
});

describe('ProductNutritionEditorComponent nutrition signals', () => {
    it('calculates macro distribution and calorie mismatch warning from form values', () => {
        const form = createProductForm();
        form.patchValue({
            caloriesPerBase: PRODUCT_CALORIES,
            proteinsPerBase: PRODUCT_PROTEINS,
            fatsPerBase: PRODUCT_FATS,
            carbsPerBase: PRODUCT_CARBS,
        });
        setRequiredInputs(form);
        fixture.detectChanges();

        expect(component.macroBarState().isEmpty).toBe(false);
        expect(component.macroBarState().segments.map(segment => segment.key)).toEqual(['proteins', 'fats', 'carbs']);
        expect(component.nutritionWarning()).toEqual({
            expectedCalories: 290,
            actualCalories: PRODUCT_CALORIES,
        });
    });

    it('updates macro distribution when nutrition values change', () => {
        const form = createProductForm();
        setRequiredInputs(form);
        fixture.detectChanges();

        expect(component.macroBarState().isEmpty).toBe(true);

        form.controls.proteinsPerBase.setValue(PRODUCT_PROTEINS);

        expect(component.macroBarState()).toEqual({
            isEmpty: false,
            segments: [{ key: 'proteins', percent: 100 }],
        });
    });
});

describe('ProductNutritionEditorComponent validation messages', () => {
    it('returns calories required error when calories control is invalid and touched', () => {
        const form = createProductForm();
        setRequiredInputs(form);
        fixture.detectChanges();

        form.controls.caloriesPerBase.markAsTouched();

        expect(component.caloriesError()).toBe('PRODUCT_MANAGE.NUTRITION_ERRORS.CALORIES_REQUIRED');
    });

    it('returns macros required error when macro controls are empty and touched', () => {
        const form = createProductForm();
        setRequiredInputs(form);
        fixture.detectChanges();

        form.controls.proteinsPerBase.markAsTouched();
        form.controls.fatsPerBase.markAsTouched();
        form.controls.carbsPerBase.markAsTouched();
        form.controls.alcoholPerBase.markAsTouched();

        expect(component.macrosError()).toBe('PRODUCT_MANAGE.NUTRITION_ERRORS.MACROS_REQUIRED');
    });
});

function setRequiredInputs(form = createProductForm()): void {
    fixture.componentRef.setInput('formGroup', form);
    fixture.componentRef.setInput('nutritionMode', 'base');
}
