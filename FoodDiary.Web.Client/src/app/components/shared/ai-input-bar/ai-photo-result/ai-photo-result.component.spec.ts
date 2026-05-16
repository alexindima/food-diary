import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import type { FoodNutritionResponse, FoodVisionItem } from '../../../../shared/models/ai.data';
import { AiPhotoResultComponent } from './ai-photo-result.component';
import type { AiPhotoEditApplied } from './ai-photo-result-lib/ai-photo-result.types';

const HALF_AMOUNT = 50;
const HALF_CALORIES = 77.5;
const HALF_PROTEIN = 6.1725;
const ORIGINAL_AMOUNT = 100;
const INVALID_SATIETY_LEVEL = 99;
const VISION_ITEMS: FoodVisionItem[] = [
    {
        nameEn: 'egg',
        nameLocal: ' яйцо ',
        amount: ORIGINAL_AMOUNT,
        unit: 'g',
        confidence: 0.9,
    },
];

const NUTRITION: FoodNutritionResponse = {
    calories: 155,
    protein: 12.345,
    fat: 10,
    carbs: 1,
    fiber: 0,
    alcohol: 0,
    items: [
        {
            name: 'egg',
            amount: ORIGINAL_AMOUNT,
            unit: 'g',
            calories: 155,
            protein: 12.345,
            fat: 10,
            carbs: 1,
            fiber: 0,
            alcohol: 0,
        },
    ],
};

type AiPhotoResultTestContext = {
    component: AiPhotoResultComponent;
    fixture: ComponentFixture<AiPhotoResultComponent>;
    translateService: TranslateService;
};

async function setupAiPhotoResultAsync(): Promise<AiPhotoResultTestContext> {
    await TestBed.configureTestingModule({
        imports: [AiPhotoResultComponent, TranslateModule.forRoot()],
    }).compileComponents();

    const fixture = TestBed.createComponent(AiPhotoResultComponent);
    const component = fixture.componentInstance;
    const translateService = TestBed.inject(TranslateService);
    fixture.componentRef.setInput('submitLabelKey', 'ADD');
    fixture.componentRef.setInput('showDetails', true);
    fixture.componentRef.setInput('results', VISION_ITEMS);
    fixture.componentRef.setInput('isAnalyzing', false);
    fixture.componentRef.setInput('isNutritionLoading', false);
    fixture.componentRef.setInput('nutrition', NUTRITION);
    fixture.componentRef.setInput('errorKey', null);
    fixture.componentRef.setInput('nutritionErrorKey', null);
    fixture.componentRef.setInput('isProcessing', false);

    return { component, fixture, translateService };
}

describe('AiPhotoResultComponent view models', () => {
    it('builds localized result rows and nutrition summary', async () => {
        const { component, fixture, translateService } = await setupAiPhotoResultAsync();
        vi.spyOn(translateService, 'instant').mockImplementation((key: string) => {
            if (key === 'GENERAL.UNITS.G') {
                return 'g';
            }

            return key;
        });
        vi.spyOn(translateService, 'getCurrentLang').mockReturnValue('en');
        fixture.detectChanges();

        expect(component.resultRows()).toEqual([{ key: 'egg', displayName: 'Яйцо', amountLabel: '100 g' }]);
        expect(component.nutritionSummary()[1]).toEqual({
            labelKey: 'GENERAL.NUTRIENTS.PROTEIN',
            value: '12.3 g',
        });
    });

    it('switches edit action and details toggle views from component state', async () => {
        const { component, fixture } = await setupAiPhotoResultAsync();
        fixture.detectChanges();

        expect(component.editActionView().labelKey).toBe('CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.EDIT_BUTTON');
        expect(component.detailsToggleView().icon).toBe('expand_more');

        component.startEditing();
        component.toggleDetails();

        expect(component.editActionView().labelKey).toBe('CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.SAVE');
        expect(component.detailsToggleView().icon).toBe('expand_less');
    });
});

describe('AiPhotoResultComponent editing', () => {
    it('recalculates nutrition locally for amount-only edit', async () => {
        const { component, fixture } = await setupAiPhotoResultAsync();
        const editSpy = vi.fn<(applied: AiPhotoEditApplied) => void>();
        component.editApplied.subscribe(applied => {
            editSpy(applied);
        });
        fixture.detectChanges();

        component.startEditing();
        component.updateEditItem(0, 'amount', String(HALF_AMOUNT));
        component.applyEditing();

        expect(editSpy).toHaveBeenCalledWith({
            items: [{ nameEn: 'egg', nameLocal: 'яйцо', amount: HALF_AMOUNT, unit: 'g', confidence: 1 }],
            nutrition: {
                calories: HALF_CALORIES,
                protein: HALF_PROTEIN,
                fat: 5,
                carbs: 0.5,
                fiber: 0,
                alcohol: 0,
                items: [
                    {
                        name: 'яйцо',
                        amount: HALF_AMOUNT,
                        unit: 'g',
                        calories: HALF_CALORIES,
                        protein: HALF_PROTEIN,
                        fat: 5,
                        carbs: 0.5,
                        fiber: 0,
                        alcohol: 0,
                    },
                ],
                notes: null,
            },
        });
    });

    it('requests AI recalculation when an item name changes', async () => {
        const { component, fixture } = await setupAiPhotoResultAsync();
        const editSpy = vi.fn<(applied: AiPhotoEditApplied) => void>();
        component.editApplied.subscribe(applied => {
            editSpy(applied);
        });
        fixture.detectChanges();

        component.startEditing();
        component.updateEditItem(0, 'name', 'omelette');
        component.applyEditing();

        expect(editSpy).toHaveBeenCalledWith({
            items: [{ nameEn: 'omelette', nameLocal: 'omelette', amount: ORIGINAL_AMOUNT, unit: 'g', confidence: 1 }],
            nutrition: null,
        });
    });
});

describe('AiPhotoResultComponent meal details', () => {
    it('emits normalized meal details', async () => {
        const { component, fixture } = await setupAiPhotoResultAsync();
        const addSpy = vi.fn();
        component.addToMeal.subscribe(addSpy);
        fixture.detectChanges();

        component.updateDetailsDate('2026-05-17');
        component.updateDetailsTime('08:30');
        component.updateDetailsComment('  Breakfast  ');
        component.preMealSatietyLevel.set(INVALID_SATIETY_LEVEL);
        component.postMealSatietyLevel.set(null);
        component.emitAddToMeal();

        expect(addSpy).toHaveBeenCalledWith({
            date: '2026-05-17',
            time: '08:30',
            comment: 'Breakfast',
            preMealSatietyLevel: 5,
            postMealSatietyLevel: 3,
        });
    });
});
