import { CdkDrag, CdkDropList } from '@angular/cdk/drag-drop';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { TranslateService } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import type { FoodNutritionResponse, FoodVisionItem } from '../../../../shared/models/ai.data';
import { AiPhotoResultComponent } from './ai-photo-result';
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
        imports: [AiPhotoResultComponent],
        providers: [provideTranslateTesting()],
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

describe('AI edit invalid rows', () => {
    it.each(['0', '-1', '', 'invalid', 'Infinity'])('keeps the last row for correction after amount %s', async amount => {
        const { component, fixture } = await setupAiPhotoResultAsync();
        const applied = vi.fn();
        component.editApplied.subscribe(applied);
        component['startEditing']();
        component['updateEditItem'](0, 'amount', amount);
        component['applyEditing']();
        fixture.detectChanges();
        expect(component['isEditing']()).toBe(true);
        expect(component['editItems']()).toHaveLength(1);
        expect(applied).not.toHaveBeenCalled();
        expect((fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')?.textContent).toContain(
            'PHOTO_AI_DIALOG.ROW_INVALID',
        );
        component['updateEditItem'](0, 'amount', String(ORIGINAL_AMOUNT));
        component['applyEditing']();
        expect(applied).toHaveBeenCalledOnce();
        expect(component['isEditing']()).toBe(false);
    });

    it('keeps an unnamed row and allows cancel to restore the original result', async () => {
        const { component } = await setupAiPhotoResultAsync();
        const applied = vi.fn();
        component.editApplied.subscribe(applied);
        component['startEditing']();
        component['updateEditItem'](0, 'name', ' ');
        component['applyEditing']();
        expect(component['isEditing']()).toBe(true);
        component['cancelEditing']();
        expect(applied).not.toHaveBeenCalled();
        expect(component['resolvedResults']()).toEqual(VISION_ITEMS);
    });
});

describe('AiPhotoResultComponent localization', () => {
    it('formats total and item calories with the active language and updates them when it changes', async () => {
        const { fixture, translateService } = await setupAiPhotoResultAsync();
        fixture.componentRef.setInput('nutrition', {
            ...NUTRITION,
            calories: 2052.4,
            items: [{ ...NUTRITION.items[0], calories: 1056.4 }],
        });
        const element = fixture.nativeElement as HTMLElement;
        for (const [language, total, item] of [
            ['ru', '2 052', '1 056'],
            ['en', '2,052', '1,056'],
            ['ru', '2 052', '1 056'],
        ]) {
            translateService.use(language);
            fixture.detectChanges();
            expect(element.querySelector('.ai-photo-result__products-calories')?.textContent.replaceAll('\u00A0', ' ')).toContain(total);
            expect(element.querySelector('.ai-photo-result__item-calories')?.textContent.replaceAll('\u00A0', ' ')).toContain(item);
        }
    });

    it('uses the available food name and preserves an unfamiliar or omitted amount unit', async () => {
        const { fixture, translateService } = await setupAiPhotoResultAsync();
        translateService.use('en');
        fixture.componentRef.setInput('nutrition', null);
        fixture.componentRef.setInput('results', [
            { ...VISION_ITEMS[0], nameEn: 'salt', nameLocal: '   ', amount: 1, unit: 'pinch' },
            { ...VISION_ITEMS[0], nameEn: 'pear', nameLocal: null, amount: 1, unit: '' },
        ]);
        fixture.detectChanges();

        const host = fixture.nativeElement as HTMLElement;
        const rows = Array.from(host.querySelectorAll('.ai-photo-result__table-row'), row => ({
            name: row.querySelector('.ai-photo-result__item-name')?.textContent.trim(),
            amount: row.querySelector('.ai-photo-result__item-amount')?.textContent.trim(),
        }));
        expect(rows).toEqual([
            { name: 'Salt', amount: '1 pinch' },
            { name: 'Pear', amount: '1' },
        ]);
    });
});

describe('AiPhotoResultComponent view models', () => {
    it('keeps the products panel visible while an image is being analyzed', async () => {
        const { fixture } = await setupAiPhotoResultAsync();
        fixture.componentRef.setInput('imageUrl', 'https://example.com/meal.png');
        fixture.componentRef.setInput('results', []);
        fixture.componentRef.setInput('nutrition', null);
        fixture.componentRef.setInput('isAnalyzing', true);
        fixture.detectChanges();

        const element = fixture.nativeElement as HTMLElement;
        expect(element.querySelector('.ai-photo-result__workspace--with-products')).not.toBeNull();
        expect(element.querySelector('.ai-photo-result__products')).not.toBeNull();
        expect(element.querySelector('.ai-photo-result__products-count')?.getAttribute('role')).toBe('status');
        expect(element.querySelector('.ai-photo-result__details-toggle')).not.toBeNull();
        expect(element.querySelector('.ai-photo-result__submit-action')).not.toBeNull();
    });

    it('builds localized result rows with calories', async () => {
        const { component, fixture, translateService } = await setupAiPhotoResultAsync();
        vi.spyOn(translateService, 'instant').mockImplementation((key: string) => {
            if (key === 'GENERAL.UNITS.G') {
                return 'g';
            }

            return key;
        });
        vi.spyOn(translateService, 'getCurrentLang').mockReturnValue('en');
        fixture.detectChanges();

        expect(component['resultRows']()).toEqual([
            { key: 'egg', annotationId: null, displayName: 'Яйцо', amountLabel: '100 g', calories: 155 },
        ]);
    });

    it('uses the nutrition order without moving calories or image markers to another food', async () => {
        const { component, fixture } = await setupAiPhotoResultAsync();
        const egg = { ...VISION_ITEMS[0], centerX: 0.25, centerY: 0.3, locationConfidence: 0.9 };
        const toast = { ...egg, nameEn: 'toast', nameLocal: null, amount: 45, centerX: 0.75 };
        const tea = { ...egg, nameEn: 'tea', nameLocal: null, amount: 250, unit: 'ml' };
        fixture.componentRef.setInput('results', [toast, tea, egg]);
        fixture.componentRef.setInput('nutrition', {
            ...NUTRITION,
            items: [NUTRITION.items[0], { ...NUTRITION.items[0], name: ' Toast ', amount: 45, calories: 100 }],
        });
        fixture.detectChanges();

        expect(component['resultRows']().map(({ key, calories, annotationId }) => ({ key, calories, annotationId }))).toEqual([
            { key: 'egg', calories: 155, annotationId: 'egg-2' },
            { key: 'toast', calories: 100, annotationId: 'toast-0' },
            { key: 'tea', calories: null, annotationId: null },
        ]);
        expect(component['annotations']()).toEqual([
            expect.objectContaining({ id: 'egg-2', centerX: 25, calories: 155 }),
            expect.objectContaining({ id: 'toast-0', centerX: 75, calories: 100 }),
        ]);
    });

    it('switches edit action and details toggle views from component state', async () => {
        const { component, fixture } = await setupAiPhotoResultAsync();
        fixture.detectChanges();

        expect(component['editActionView']().labelKey).toBe('MEAL_MANAGE.PHOTO_AI_DIALOG.EDIT_BUTTON');
        expect(component['detailsToggleView']().icon).toBe('expand_more');

        component['startEditing']();
        component['toggleDetails']();

        expect(component['editActionView']().labelKey).toBe('MEAL_MANAGE.PHOTO_AI_DIALOG.SAVE');
        expect(component['detailsToggleView']().icon).toBe('expand_less');
    });

    it('allows the modal workspace to shrink when details are expanded', async () => {
        const { component, fixture } = await setupAiPhotoResultAsync();
        fixture.componentRef.setInput('imageUrl', 'https://example.com/meal.png');
        fixture.detectChanges();

        component['toggleDetails']();
        fixture.detectChanges();

        expect((fixture.nativeElement as HTMLElement).querySelector('.ai-photo-result--details-expanded')).not.toBeNull();
    });
});

describe('AiPhotoResultComponent editing', () => {
    it('recalculates nutrition locally for amount-only edit', async () => {
        const { component, fixture } = await setupAiPhotoResultAsync();
        const editSpy = vi.fn<(applied: AiPhotoEditApplied) => void>();
        component['editApplied'].subscribe(applied => {
            editSpy(applied);
        });
        fixture.detectChanges();

        component['startEditing']();
        component['updateEditItem'](0, 'amount', String(HALF_AMOUNT));
        component['applyEditing']();

        expect(editSpy).toHaveBeenCalledWith({
            items: [{ nameEn: 'egg', nameLocal: 'яйцо', amount: HALF_AMOUNT, unit: 'g', confidence: 0.9 }],
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
        component['editApplied'].subscribe(applied => {
            editSpy(applied);
        });
        fixture.detectChanges();

        component['startEditing']();
        component['updateEditItem'](0, 'name', 'omelette');
        component['applyEditing']();

        expect(editSpy).toHaveBeenCalledWith({
            items: [{ nameEn: 'omelette', nameLocal: 'omelette', amount: ORIGINAL_AMOUNT, unit: 'g', confidence: 0.9 }],
            nutrition: null,
        });
    });

    it('emits empty items without stale nutrition when all edited items are removed', async () => {
        const { component, fixture } = await setupAiPhotoResultAsync();
        const editSpy = vi.fn<(applied: AiPhotoEditApplied) => void>();
        component['editApplied'].subscribe(applied => {
            editSpy(applied);
        });
        fixture.detectChanges();

        component['startEditing']();
        component['removeEditItem'](0);
        component['applyEditing']();

        expect(editSpy).toHaveBeenCalledWith({
            items: [],
            nutrition: null,
        });
    });
});

describe('AiPhotoResultComponent meal details', () => {
    it('emits normalized meal details', async () => {
        const { component, fixture } = await setupAiPhotoResultAsync();
        const addSpy = vi.fn();
        component['addToMeal'].subscribe(addSpy);
        fixture.detectChanges();

        component['updateDetailsDate']('2026-05-17');
        component['updateDetailsTime']('08:30');
        component['updateDetailsComment']('  Breakfast  ');
        component['preMealSatietyLevel'].set(INVALID_SATIETY_LEVEL);
        component['postMealSatietyLevel'].set(null);
        component['emitAddToMeal']();

        expect(addSpy).toHaveBeenCalledWith({
            date: '2026-05-17',
            time: '08:30',
            comment: 'Breakfast',
            preMealSatietyLevel: 5,
            postMealSatietyLevel: null,
        });
    });
});

describe('Rendered photo correction', () => {
    it('validates a new row and publishes only the corrected food after removal and apply', async () => {
        const { component, fixture } = await setupAiPhotoResultAsync();
        const applied = vi.fn();
        component.editApplied.subscribe(applied);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        const action = '.ai-photo-result__items-header > fd-ui-button:nth-child(2) button';
        photoButton(host, action).click();
        fixture.detectChanges();
        photoButton(host, '.ai-photo-result__edit-add button').click();
        fixture.detectChanges();
        expect(host.querySelectorAll('.ai-photo-result__edit-row')).toHaveLength(2);

        photoButton(host, action).click();
        fixture.detectChanges();
        expect(applied).not.toHaveBeenCalled();
        expect(host.querySelector('[role="alert"]')?.textContent).toContain('PHOTO_AI_DIALOG.ROW_INVALID');
        changePhotoField(host, '.ai-photo-result__edit-row:nth-child(2) input[type="text"]', '  pear  ');
        changePhotoField(host, '.ai-photo-result__edit-row:nth-child(2) input[type="number"]', '100');
        photoButton(host, '.ai-photo-result__edit-row:first-child .ai-photo-result__edit-remove button').click();
        fixture.detectChanges();
        expect(host.querySelectorAll('.ai-photo-result__edit-row')).toHaveLength(1);
        expect(host.querySelector('[role="alert"]')).toBeNull();

        photoButton(host, action).click();
        fixture.detectChanges();
        expect(applied).toHaveBeenCalledExactlyOnceWith({
            items: [expect.objectContaining({ nameEn: 'pear', amount: ORIGINAL_AMOUNT, unit: 'g' })],
            nutrition: null,
        });
        expect(host.querySelector('fd-ai-photo-edit-list')).toBeNull();
    });

    it('keeps the nutrition of another same-named portion when one amount is halved', async () => {
        const { component, fixture } = await setupAiPhotoResultAsync();
        const applied = vi.fn<(applied: AiPhotoEditApplied) => void>();
        component.editApplied.subscribe(applied);
        const twoServings = 2;
        const original = NUTRITION.items[0];
        const largerPortion = {
            ...original,
            amount: ORIGINAL_AMOUNT * twoServings,
            calories: original.calories * twoServings,
            protein: original.protein * twoServings,
            fat: original.fat * twoServings,
            carbs: original.carbs * twoServings,
        };
        fixture.componentRef.setInput('results', [VISION_ITEMS[0], { ...VISION_ITEMS[0], amount: largerPortion.amount }]);
        fixture.componentRef.setInput('nutrition', {
            ...NUTRITION,
            calories: original.calories + largerPortion.calories,
            protein: original.protein + largerPortion.protein,
            fat: original.fat + largerPortion.fat,
            carbs: original.carbs + largerPortion.carbs,
            items: [largerPortion, original],
        });
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        const action = '.ai-photo-result__items-header > fd-ui-button:nth-child(2) button';

        photoButton(host, action).click();
        fixture.detectChanges();
        changePhotoField(host, '.ai-photo-result__edit-row:first-child input[type="number"]', String(HALF_AMOUNT));
        fixture.detectChanges();
        photoButton(host, action).click();

        expect(applied).toHaveBeenCalledOnce();
        expect(applied.mock.calls[0][0].nutrition).toMatchObject({
            calories: HALF_CALORIES + largerPortion.calories,
            protein: HALF_PROTEIN + largerPortion.protein,
            items: [
                { amount: HALF_AMOUNT, calories: HALF_CALORIES, protein: HALF_PROTEIN },
                { amount: largerPortion.amount, calories: largerPortion.calories, protein: largerPortion.protein },
            ],
        });
    });
});

describe('Rendered recognition draft order', () => {
    it('preserves an unchanged drop and delivers reordered amounts with their original nutrition on save', async () => {
        const { component, fixture } = await setupAiPhotoResultAsync();
        const applied = vi.fn<(result: AiPhotoEditApplied) => void>();
        component.editApplied.subscribe(applied);
        const egg = { ...VISION_ITEMS[0], nameLocal: null };
        const pear = { ...egg, nameEn: 'pear', amount: HALF_AMOUNT };
        const eggNutrition = NUTRITION.items[0];
        const pearNutrition = { ...eggNutrition, name: 'pear', amount: HALF_AMOUNT, calories: HALF_CALORIES };
        fixture.componentRef.setInput('results', [egg, pear]);
        fixture.componentRef.setInput('nutrition', {
            ...NUTRITION,
            calories: eggNutrition.calories + pearNutrition.calories,
            items: [eggNutrition, pearNutrition],
        });
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        const action = '.ai-photo-result__items-header > fd-ui-button:nth-child(2) button';
        photoButton(host, action).click();
        fixture.detectChanges();

        const container = fixture.debugElement.query(By.directive(CdkDropList)).injector.get<CdkDropList<unknown>>(CdkDropList);
        const item = fixture.debugElement.queryAll(By.directive(CdkDrag))[1].injector.get<CdkDrag<unknown>>(CdkDrag);
        const drop = {
            previousIndex: 1,
            container,
            previousContainer: container,
            item,
            isPointerOverContainer: true,
            distance: { x: 0, y: 0 },
            dropPoint: { x: 0, y: 0 },
            event: new MouseEvent('mouseup'),
        };
        container.dropped.emit({ ...drop, currentIndex: 1 });
        fixture.detectChanges();
        expect(Array.from(host.querySelectorAll<HTMLInputElement>('.ai-photo-result__edit-input--name'), input => input.value)).toEqual([
            'egg',
            'pear',
        ]);

        container.dropped.emit({ ...drop, currentIndex: 0 });
        fixture.detectChanges();
        expect(Array.from(host.querySelectorAll<HTMLInputElement>('.ai-photo-result__edit-input--name'), input => input.value)).toEqual([
            'pear',
            'egg',
        ]);
        expect(applied).not.toHaveBeenCalled();
        photoButton(host, action).click();

        expect(applied).toHaveBeenCalledOnce();
        const result = applied.mock.calls[0][0];
        expect(result.items).toEqual([pear, egg]);
        expect(result.nutrition?.items).toEqual([pearNutrition, eggNutrition]);
    });
});

describe('Rendered photo result details', () => {
    it('retains a comment when details close and submits only nonblank content', async () => {
        const { component, fixture } = await setupAiPhotoResultAsync();
        const submitted = vi.fn();
        component.addToMeal.subscribe(submitted);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        const details = host.querySelector('.ai-photo-result__details');
        const showDetails = photoButton(host, '.ai-photo-result__details-toggle button');
        expect(details?.hasAttribute('inert')).toBe(true);
        showDetails.click();
        fixture.detectChanges();
        expect(details?.hasAttribute('inert')).toBe(false);

        changePhotoField(host, 'textarea', '  Breakfast with pears  ');
        showDetails.click();
        fixture.detectChanges();
        expect(details?.hasAttribute('inert')).toBe(true);
        photoButton(host, '.ai-photo-result__submit-action button').click();
        expect(submitted).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ comment: 'Breakfast with pears' }));

        showDetails.click();
        fixture.detectChanges();
        expect(host.querySelector('textarea')?.value).toBe('  Breakfast with pears  ');
        changePhotoField(host, 'textarea', '  ');
        photoButton(host, '.ai-photo-result__submit-action button').click();
        expect(submitted).toHaveBeenLastCalledWith(expect.objectContaining({ comment: null }));
    });
});

describe('Rendered photo annotations', () => {
    it('keeps the chosen food selected when the user toggles the overlay off and on', async () => {
        const { fixture } = await setupAiPhotoResultAsync();
        fixture.componentRef.setInput('imageUrl', '/meal.png');
        fixture.componentRef.setInput('results', [
            { ...VISION_ITEMS[0], centerX: 0.2, centerY: 0.2 },
            { ...VISION_ITEMS[0], nameEn: 'pear', nameLocal: 'Pear', centerX: 0.8, centerY: 0.8 },
        ]);
        fixture.componentRef.setInput('nutrition', { ...NUTRITION, items: [...NUTRITION.items, { ...NUTRITION.items[0], name: 'pear' }] });
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        const toggle = photoButton(host, '.ai-photo-result__annotation-toggle');
        photoButton(host, '.ai-photo-result__table-row:nth-child(2)').click();
        fixture.detectChanges();
        expect(host.querySelector('.ai-photo-result__table-row--active')?.textContent).toContain('Pear');
        expect(host.querySelector('.ai-photo-result__annotations')).not.toBeNull();

        toggle.click();
        fixture.detectChanges();
        expect(host.querySelector('.ai-photo-result__annotations')).toBeNull();
        expect(toggle.textContent).toContain('PHOTO_AI_DIALOG.SHOW_ANNOTATIONS');
        toggle.click();
        fixture.detectChanges();
        expect(host.querySelector('.ai-photo-result__annotations')).not.toBeNull();
        expect(host.querySelector('.ai-photo-result__table-row--active')?.textContent).toContain('Pear');
    });
});

function photoButton(root: HTMLElement, selector: string): HTMLButtonElement {
    const button = root.querySelector(selector);
    if (!(button instanceof HTMLButtonElement)) {
        throw new Error(`Expected the photo result control ${selector}`);
    }
    return button;
}

function changePhotoField(root: HTMLElement, selector: string, value: string): void {
    const field = root.querySelector(selector);
    if (!(field instanceof HTMLInputElement) && !(field instanceof HTMLTextAreaElement)) {
        throw new Error(`Expected the photo result field ${selector}`);
    }
    field.value = value;
    field.dispatchEvent(new Event('input', { bubbles: true }));
}
