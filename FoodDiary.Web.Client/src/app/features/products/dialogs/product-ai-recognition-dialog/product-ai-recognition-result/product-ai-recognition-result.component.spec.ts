import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import type { FoodNutritionResponse } from '../../../../../shared/models/ai.data';
import { MeasurementUnit } from '../../../models/product.data';
import { createProductAiRecognitionForm } from '../product-ai-recognition-lib/product-ai-recognition.helpers';
import { ProductAiRecognitionResultComponent } from './product-ai-recognition-result.component';

const PRODUCT_CALORIES = 150;
const PRODUCT_PROTEINS = 4;
const PRODUCT_FATS = 2;
const PRODUCT_CARBS = 25;
const PRODUCT_FIBER = 3;

let fixture: ComponentFixture<ProductAiRecognitionResultComponent>;
let component: ProductAiRecognitionResultComponent;
let languageChanges$: Subject<unknown>;

beforeEach(() => {
    languageChanges$ = new Subject<unknown>();

    TestBed.configureTestingModule({
        imports: [ProductAiRecognitionResultComponent],
        providers: [
            {
                provide: TranslateService,
                useValue: {
                    instant: vi.fn((key: string) => key),
                    onLangChange: languageChanges$.asObservable(),
                },
            },
        ],
    });
    TestBed.overrideComponent(ProductAiRecognitionResultComponent, {
        set: { template: '' },
    });

    fixture = TestBed.createComponent(ProductAiRecognitionResultComponent);
    component = fixture.componentInstance;
});

describe('ProductAiRecognitionResultComponent', () => {
    it('builds unit options inside the component', () => {
        setRequiredInputs(['Apple']);
        fixture.detectChanges();

        expect(component.unitOptions()).toEqual([
            { value: MeasurementUnit.G, label: 'PRODUCT_AMOUNT_UNITS.G' },
            { value: MeasurementUnit.ML, label: 'PRODUCT_AMOUNT_UNITS.ML' },
            { value: MeasurementUnit.PCS, label: 'PRODUCT_AMOUNT_UNITS.PCS' },
        ]);
    });

    it('rebuilds unit options when language changes', () => {
        setRequiredInputs(['Apple']);
        fixture.detectChanges();
        const firstOptions = component.unitOptions();

        languageChanges$.next(null);

        expect(component.unitOptions()).not.toBe(firstOptions);
    });

    it('detects multiple recognized item names', () => {
        setRequiredInputs(['Apple', 'Milk']);
        fixture.detectChanges();

        expect(component.hasMultipleItems()).toBe(true);
    });

    it('detects single recognized item name', () => {
        setRequiredInputs(['Apple']);
        fixture.detectChanges();

        expect(component.hasMultipleItems()).toBe(false);
    });
});

function setRequiredInputs(itemNames: readonly string[]): void {
    fixture.componentRef.setInput('form', createProductAiRecognitionForm());
    fixture.componentRef.setInput('nutrition', createNutrition());
    fixture.componentRef.setInput('itemNames', itemNames);
}

function createNutrition(): FoodNutritionResponse {
    return {
        calories: PRODUCT_CALORIES,
        protein: PRODUCT_PROTEINS,
        fat: PRODUCT_FATS,
        carbs: PRODUCT_CARBS,
        fiber: PRODUCT_FIBER,
        alcohol: 0,
        items: [],
    };
}
