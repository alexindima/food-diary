import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { firstValueFrom, of, Subject, throwError } from 'rxjs';
import { describe, expect, it, type Mock, onTestFinished, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { AuthService } from '../../../../services/auth.service';
import { RecipeLookupService } from '../../../../shared/api/recipe-lookup.service';
import type { Meal } from '../../../../shared/models/meal.data';
import { MeasurementUnit, type Product, ProductVisibility } from '../../../../shared/models/product.data';
import { type Recipe, RecipeVisibility } from '../../../../shared/models/recipe.data';
import type { RecipeLookup } from '../../../../shared/models/recipe-lookup.data';
import { utcInstant } from '../../../../shared/models/semantics/date-value';
import { entityId } from '../../../../shared/models/semantics/entity-id';
import { MealService } from '../../api/meal.service';
import { QuickMealService } from '../../lib/quick/quick-meal.service';
import { QuickMealDrawerComponent } from './quick-meal-drawer';

const INITIAL_SERVINGS = 2;
const EDITED_SERVINGS = 3;
const GRAMS_PER_SERVING = 150;
const RECIPE_SERVINGS = 4;
const RECIPE: Recipe = {
    id: entityId<'recipe'>('prepared-soup'),
    name: 'Prepared soup',
    servings: RECIPE_SERVINGS,
    visibility: RecipeVisibility.Private,
    usageCount: 0,
    createdAt: utcInstant('2026-05-14T08:00:00Z'),
    isOwnedByCurrentUser: true,
    isNutritionAutoCalculated: true,
    steps: [],
};
const PRODUCT_PORTION_ML = 125;
const EDITED_PRODUCT_ML = 175;
const PRODUCT: Product = {
    id: entityId<'product'>('oat-milk'),
    name: 'Oat milk',
    baseUnit: MeasurementUnit.ML,
    baseAmount: 100,
    defaultPortionAmount: PRODUCT_PORTION_ML,
    caloriesPerBase: 40,
    proteinsPerBase: 1,
    fatsPerBase: 2,
    carbsPerBase: 5,
    fiberPerBase: 0,
    alcoholPerBase: 0,
    usageCount: 0,
    visibility: ProductVisibility.Private,
    createdAt: new Date('2026-05-14T08:00:00Z'),
    isOwnedByCurrentUser: true,
    qualityScore: 80,
    qualityGrade: 'green',
};
const CREATED: Meal = {
    id: entityId<'meal'>('saved-quick-meal'),
    date: utcInstant('2026-05-14T08:00:00Z'),
    mealType: null,
    comment: null,
    totalCalories: 450,
    totalProteins: 10,
    totalFats: 5,
    totalCarbs: 35,
    totalFiber: 4,
    totalAlcohol: 0,
    isNutritionAutoCalculated: true,
    items: [],
    aiSessions: [],
};

describe('Quick meal product quantities in a mixed draft', () => {
    it('edits a product in its units while retaining the recipe servings in the same draft', async () => {
        const { fixture, lookupRecipe, createMeal } = await renderDrawerAsync();
        TestBed.inject(QuickMealService).addProduct(PRODUCT);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        const productEdit = requiredElement(
            host.querySelector<HTMLButtonElement>(`button[aria-label="Edit ${PRODUCT.name}"]`),
            'the product edit button',
        );
        const productRow = requiredElement(productEdit.closest('.quick-meal__item'), 'the product draft row');
        expect(productRow.querySelector('.quick-meal__name')?.textContent).toBe(PRODUCT.name);
        expect(productRow.querySelector('img')?.getAttribute('src')).toBe('assets/images/stubs/receipt.png');
        expect(productRow.querySelector('.quick-meal__meta')?.textContent).toContain(`${PRODUCT_PORTION_ML} GENERAL.UNITS.ML`);

        productEdit.click();
        const dialog = await visibleEditorAsync();
        expect(amountInput(dialog).valueAsNumber).toBe(PRODUCT_PORTION_ML);
        setAmount(dialog, EDITED_PRODUCT_ML);
        editorAction(dialog, 'MEAL_MANAGE.MANUAL_ITEM_DIALOG_SAVE').click();
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(host.textContent).toContain(`${EDITED_PRODUCT_ML} GENERAL.UNITS.ML`);
        });

        draftSaveButton(host).click();
        expect(lookupRecipe).not.toHaveBeenCalled();
        expect(createMeal).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({
                items: [
                    { recipeId: RECIPE.id, productId: null, amount: INITIAL_SERVINGS },
                    { productId: PRODUCT.id, recipeId: null, amount: EDITED_PRODUCT_ML },
                ],
            }),
            expect.any(String),
        );
    });
});

describe('Quick meal quantity editing with the real dialog', () => {
    it('waits for recipe mass, edits in grams and only stores servings when saving the meal', async () => {
        const lookup = new Subject<RecipeLookup>();
        const { fixture, lookupRecipe, createMeal } = await renderDrawerAsync();
        lookupRecipe.mockReturnValue(lookup);
        const host = fixture.nativeElement as HTMLElement;

        editButton(host).click();
        fixture.detectChanges();
        expect(lookupRecipe).toHaveBeenCalledWith(RECIPE.id);
        expect(editButton(host).disabled).toBe(true);
        expect(draftSaveButton(host).disabled).toBe(true);
        expect(document.querySelector('fd-meal-manual-item-dialog')).toBeNull();
        expect(createMeal).not.toHaveBeenCalled();

        lookup.next({
            id: RECIPE.id,
            servings: RECIPE_SERVINGS,
            steps: [{ ingredients: [{ amount: GRAMS_PER_SERVING * RECIPE_SERVINGS, productBaseUnit: 'G' }] }],
        });
        const dialog = await visibleEditorAsync();
        expect(amountInput(dialog).valueAsNumber).toBe(INITIAL_SERVINGS * GRAMS_PER_SERVING);
        setAmount(dialog, EDITED_SERVINGS * GRAMS_PER_SERVING);
        editorAction(dialog, 'MEAL_MANAGE.MANUAL_ITEM_DIALOG_SAVE').click();
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(host.querySelector('.quick-meal__meta')?.textContent).toContain(`${EDITED_SERVINGS} servings`);
        });
        expect(createMeal).not.toHaveBeenCalled();

        draftSaveButton(host).click();
        expect(createMeal).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({ items: [{ recipeId: RECIPE.id, productId: null, amount: EDITED_SERVINGS }] }),
            expect.any(String),
        );
        fixture.detectChanges();
        expect(host.querySelector('[role="region"]')).toBeNull();
    });

    it('retains the draft after dismissing an edit and a failed save, then retries the same request', async () => {
        const { fixture, lookupRecipe, createMeal, showError } = await renderDrawerAsync();
        lookupRecipe.mockReturnValue(throwError(() => new Error('Recipe details unavailable')));
        createMeal.mockReturnValueOnce(throwError(() => new Error('Temporary save failure')));
        const host = fixture.nativeElement as HTMLElement;
        editButton(host).click();
        const dialog = await visibleEditorAsync();
        expect(amountInput(dialog).valueAsNumber).toBe(INITIAL_SERVINGS);
        setAmount(dialog, EDITED_SERVINGS);
        editorAction(dialog, 'PRODUCT_MANAGE.CANCEL_BUTTON').click();
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(draftSaveButton(host).disabled).toBe(false);
        });

        draftSaveButton(host).click();
        fixture.detectChanges();
        expect(showError).toHaveBeenCalledWith('QUICK_MEAL.SAVE_ERROR');
        expect(host.querySelector('.quick-meal__meta')?.textContent).toContain(`${INITIAL_SERVINGS} servings`);
        expect(createMeal).toHaveBeenCalledTimes(1);
        const originalCall = createMeal.mock.calls[0];
        expect(originalCall[0].items).toEqual([{ recipeId: RECIPE.id, productId: null, amount: INITIAL_SERVINGS }]);

        draftSaveButton(host).click();
        fixture.detectChanges();
        expect(createMeal).toHaveBeenCalledTimes(2);
        expect(createMeal.mock.calls[1]).toEqual(originalCall);
        expect(host.querySelector('[role="region"]')).toBeNull();
    });
});

describe('Quick meal serving mass after a temporary lookup failure', () => {
    it('reopens a recipe in recovered grams and still saves its original servings', async () => {
        const { fixture, lookupRecipe, createMeal } = await renderDrawerAsync();
        const host = fixture.nativeElement as HTMLElement;
        lookupRecipe.mockReturnValueOnce(throwError(() => new Error('Temporary recipe lookup failure'))).mockReturnValueOnce(
            of({
                id: RECIPE.id,
                servings: RECIPE_SERVINGS,
                steps: [{ ingredients: [{ amount: GRAMS_PER_SERVING * RECIPE_SERVINGS, productBaseUnit: 'G' }] }],
            }),
        );

        editButton(host).click();
        const fallback = await visibleEditorAsync();
        expect(amountInput(fallback).getAttribute('aria-label')).toBe('MEAL_MANAGE.AMOUNT_LABEL_RECIPE_SERVINGS');
        expect(amountInput(fallback).valueAsNumber).toBe(INITIAL_SERVINGS);
        editorAction(fallback, 'PRODUCT_MANAGE.CANCEL_BUTTON').click();
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(editButton(host).disabled).toBe(false);
        });

        editButton(host).click();
        const recovered = await visibleEditorAsync();
        expect(lookupRecipe.mock.calls).toEqual([[RECIPE.id], [RECIPE.id]]);
        expect(amountInput(recovered).valueAsNumber).toBe(INITIAL_SERVINGS * GRAMS_PER_SERVING);
        editorAction(recovered, 'MEAL_MANAGE.MANUAL_ITEM_DIALOG_SAVE').click();
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(draftSaveButton(host).disabled).toBe(false);
        });

        draftSaveButton(host).click();
        expect(createMeal).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({ items: [{ recipeId: RECIPE.id, productId: null, amount: INITIAL_SERVINGS }] }),
            expect.any(String),
        );
    });
});

describe('Quick meal with recipes that have no computable serving mass', () => {
    const editedServings = 2.5;
    const recipes = [
        { kind: 'an instruction-only step', ingredients: [] },
        {
            kind: 'a mix of weight and volume',
            ingredients: [
                { amount: GRAMS_PER_SERVING, productBaseUnit: 'G' },
                { amount: GRAMS_PER_SERVING, productBaseUnit: 'ML' },
            ],
        },
    ];

    it.each(recipes)('corrects and saves decimal servings for a recipe with $kind', async ({ ingredients }) => {
        const { fixture, lookupRecipe, createMeal, showError } = await renderDrawerAsync();
        const host = fixture.nativeElement as HTMLElement;
        lookupRecipe.mockReturnValueOnce(of({ id: RECIPE.id, servings: RECIPE_SERVINGS, steps: [{ ingredients }] }));

        editButton(host).click();
        const dialog = await visibleEditorAsync();
        const amount = amountInput(dialog);
        expect(amount.getAttribute('aria-label')).toBe('MEAL_MANAGE.AMOUNT_LABEL_RECIPE_SERVINGS');
        expect(amount.valueAsNumber).toBe(INITIAL_SERVINGS);

        const confirm = editorAction(dialog, 'MEAL_MANAGE.MANUAL_ITEM_DIALOG_SAVE');
        setAmount(dialog, 0);
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(confirm.disabled).toBe(true);
        });
        confirm.click();
        expect(dialog.isConnected).toBe(true);
        expect(createMeal).not.toHaveBeenCalled();

        setAmount(dialog, editedServings);
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(confirm.disabled).toBe(false);
        });
        confirm.click();
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(editButton(host).disabled).toBe(false);
            expect(host.querySelector('.quick-meal__meta')?.textContent).toContain('2.5 servings');
        });

        editButton(host).click();
        const reopened = await visibleEditorAsync();
        expect(amountInput(reopened).getAttribute('aria-label')).toBe('MEAL_MANAGE.AMOUNT_LABEL_RECIPE_SERVINGS');
        expect(amountInput(reopened).valueAsNumber).toBe(editedServings);
        expect(lookupRecipe).toHaveBeenCalledExactlyOnceWith(RECIPE.id);
        editorAction(reopened, 'PRODUCT_MANAGE.CANCEL_BUTTON').click();
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(draftSaveButton(host).disabled).toBe(false);
        });

        draftSaveButton(host).click();
        expect(createMeal).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({ items: [{ recipeId: RECIPE.id, productId: null, amount: editedServings }] }),
            expect.any(String),
        );
        fixture.detectChanges();
        expect(host.querySelector('[role="region"]')).toBeNull();
        expect(showError).not.toHaveBeenCalled();
    });
});

describe('Quick meal draft actions during a pending save', () => {
    it.each([
        { action: 'clear', selector: 'button[aria-label="QUICK_MEAL.CLEAR"]', accepted: true },
        { action: 'remove', selector: '.quick-meal__item-action--danger button', accepted: false },
    ])('keeps the pending request and submitted draft when the user tries to $action it', async scenario => {
        const { fixture, createMeal, showError } = await renderDrawerAsync();
        const pending = new Subject<Meal>();
        createMeal.mockReturnValueOnce(pending);
        const host = fixture.nativeElement as HTMLElement;
        const action = requiredElement(host.querySelector<HTMLButtonElement>(scenario.selector), `the draft ${scenario.action} button`);

        draftSaveButton(host).click();
        fixture.detectChanges();
        expect(pending.observed).toBe(true);
        expect(createMeal).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({ items: [{ recipeId: RECIPE.id, productId: null, amount: INITIAL_SERVINGS }] }),
            expect.any(String),
        );
        action.click();
        fixture.detectChanges();
        expect(pending.observed).toBe(true);
        expect(host.querySelector('.quick-meal__name')?.textContent).toBe(RECIPE.name);
        expect(action.disabled).toBe(true);
        expect(action.title).toBe('QUICK_MEAL.SAVING');

        if (scenario.accepted) {
            pending.next(CREATED);
            pending.complete();
            fixture.detectChanges();
            expect(host.querySelector('[role="region"]')).toBeNull();
            expect(showError).not.toHaveBeenCalled();
        } else {
            pending.error(new Error('Meal save failed'));
            fixture.detectChanges();
            expect(showError).toHaveBeenCalledExactlyOnceWith('QUICK_MEAL.SAVE_ERROR');
            expect(host.querySelector('.quick-meal__name')?.textContent).toBe(RECIPE.name);
            expect(action.disabled).toBe(false);
            action.click();
            fixture.detectChanges();
            expect(host.querySelector('[role="region"]')).toBeNull();
        }
        expect(createMeal).toHaveBeenCalledOnce();
    });
});

describe('Quick meal detail fields during a pending save', () => {
    it('keeps the submitted comment read-only until the successful write removes the draft', async () => {
        const { fixture, createMeal } = await renderDrawerAsync();
        const pending = new Subject<Meal>();
        createMeal.mockReturnValueOnce(pending);
        const host = fixture.nativeElement as HTMLElement;
        const submittedComment = 'Lunch with a friend';
        openMealDetails(host);
        const comment = quickMealComment(host);
        comment.focus();
        expect(document.activeElement).toBe(comment);
        setQuickMealComment(comment, submittedComment);

        draftSaveButton(host).click();
        TestBed.tick();
        expect(pending.observed).toBe(true);
        expect(createMeal).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ comment: submittedComment }), expect.any(String));

        comment.blur();
        comment.focus();
        if (document.activeElement === comment) {
            setQuickMealComment(comment, 'Correction made while saving');
        }
        const commentWhenSaved = TestBed.inject(QuickMealService).details().comment;

        pending.next(CREATED);
        pending.complete();
        fixture.detectChanges();
        expect(host.querySelector('[role="region"]')).toBeNull();
        expect(commentWhenSaved).toBe(submittedComment);
    });

    it('locks the editable controls during the write and restores them after failure', async () => {
        const { fixture, createMeal, showError } = await renderDrawerAsync();
        const pending = new Subject<Meal>();
        createMeal.mockReturnValueOnce(pending);
        const host = fixture.nativeElement as HTMLElement;
        const originalComment = 'Dinner after work';
        const revisedComment = 'Dinner at home';
        openMealDetails(host);
        const controls = quickMealDetailControls(host);
        const details = requiredElement(host.querySelector('fieldset.quick-meal__details'), 'the meal details fieldset');
        const comment = quickMealComment(host);
        expect(controls.every(control => !control.matches(':disabled'))).toBe(true);
        setQuickMealComment(comment, originalComment);

        draftSaveButton(host).click();
        TestBed.tick();
        expect(controls.filter(control => !control.matches(':disabled'))).toHaveLength(0);
        expect(details.getAttribute('aria-busy')).toBe('true');

        pending.error(new Error('Could not save the draft'));
        TestBed.tick();
        expect(showError).toHaveBeenCalledExactlyOnceWith('QUICK_MEAL.SAVE_ERROR');
        expect(controls.every(control => !control.matches(':disabled'))).toBe(true);
        expect(comment.value).toBe(originalComment);
        expect(details.getAttribute('aria-busy')).toBe('false');

        setQuickMealComment(comment, revisedComment);
        draftSaveButton(host).click();
        TestBed.tick();
        expect(createMeal).toHaveBeenCalledTimes(2);
        expect(createMeal).toHaveBeenLastCalledWith(expect.objectContaining({ comment: revisedComment }), expect.any(String));
        expect(host.querySelector('[role="region"]')).toBeNull();
    });
});

function quickMealComment(drawer: HTMLElement): HTMLTextAreaElement {
    return requiredElement(drawer.querySelector<HTMLTextAreaElement>('fd-meal-details-fields textarea'), 'the meal comment');
}

function quickMealDetailControls(drawer: HTMLElement): Array<HTMLButtonElement | HTMLInputElement | HTMLTextAreaElement> {
    return [
        requiredElement(drawer.querySelector<HTMLButtonElement>('fd-ui-select button[aria-haspopup="listbox"]'), 'the meal type picker'),
        requiredElement(drawer.querySelector<HTMLButtonElement>('fd-ui-date-input button[aria-haspopup="dialog"]'), 'the meal date picker'),
        quickMealTimeControl(drawer),
        quickMealComment(drawer),
        requiredElement(drawer.querySelector<HTMLButtonElement>('button[aria-label^="HUNGER_BEFORE_SCALE.LEVEL_2.TITLE."]'), 'hunger'),
        requiredElement(drawer.querySelector<HTMLButtonElement>('button[aria-label^="SATIETY_AFTER_SCALE.LEVEL_4.TITLE."]'), 'satiety'),
    ];
}

function setQuickMealComment(input: HTMLTextAreaElement, comment: string): void {
    input.value = comment;
    input.dispatchEvent(new Event('input', { bubbles: true }));
    TestBed.tick();
}

describe('Quick meal inline hunger and satiety', () => {
    it('clears a hunger choice without dropping the independent satiety answer when saving', async () => {
        const satiety = 4;
        const { fixture, createMeal } = await renderDrawerAsync();
        const host = fixture.nativeElement as HTMLElement;
        openMealDetails(host);
        const fields = requiredElement(host.querySelector('fd-meal-satiety-fields'), 'the inline meal details controls');
        const hungerOption = requiredElement(
            fields.querySelector<HTMLButtonElement>('button[role="radio"][aria-label^="HUNGER_BEFORE_SCALE.LEVEL_2.TITLE."]'),
            'the hunger option',
        );
        const satietyOption = requiredElement(
            fields.querySelector<HTMLButtonElement>('button[role="radio"][aria-label^="SATIETY_AFTER_SCALE.LEVEL_4.TITLE."]'),
            'the satiety option',
        );
        hungerOption.click();
        satietyOption.click();
        fixture.detectChanges();
        expect(hungerOption.getAttribute('aria-checked')).toBe('true');
        expect(satietyOption.getAttribute('aria-checked')).toBe('true');

        editorAction(host, 'MEAL_DETAILS.CLEAR_HUNGER').click();
        fixture.detectChanges();
        expect(hungerOption.getAttribute('aria-checked')).toBe('false');
        expect(satietyOption.getAttribute('aria-checked')).toBe('true');

        draftSaveButton(host).click();
        expect(createMeal).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({ preMealSatietyLevel: null, postMealSatietyLevel: satiety }),
            expect.any(String),
        );
    });
});

describe('Quick meal local date and time', () => {
    it('submits the selected local date and time near midnight', async () => {
        const localDate = '2026-05-14';
        const localTime = '23:45';
        setDrawerCalendarMonth(localDate);
        const { fixture, createMeal, showError } = await renderDrawerAsync();
        const host = fixture.nativeElement as HTMLElement;
        openMealDetails(host);
        selectQuickMealDate(host, localDate);
        selectQuickMealTime(host, localTime);
        expect(quickMealTimeControl(host).value).toBe(localTime);

        draftSaveButton(host).click();
        expect(createMeal).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({ date: new Date(`${localDate}T${localTime}`) }),
            expect.any(String),
        );
        expect(showError).not.toHaveBeenCalled();
    });

    const missingTimes: Partial<Record<string, { date: string; missing: string; corrected: string; expected: string }>> = {
        'America/Los_Angeles': { date: '2026-03-08', missing: '02:30', corrected: '03:30', expected: '2026-03-08T10:30:00.000Z' },
        'America/St_Johns': { date: '2026-03-08', missing: '02:30', corrected: '03:30', expected: '2026-03-08T06:00:00.000Z' },
        'Asia/Tbilisi': { date: '2005-03-27', missing: '02:30', corrected: '03:30', expected: '2005-03-26T23:30:00.000Z' },
        'Europe/Berlin': { date: '2026-03-29', missing: '02:30', corrected: '03:30', expected: '2026-03-29T01:30:00.000Z' },
        'Australia/Lord_Howe': { date: '2026-10-04', missing: '02:15', corrected: '02:45', expected: '2026-10-03T15:45:00.000Z' },
    };
    const zone = new Intl.DateTimeFormat().resolvedOptions().timeZone;
    const gap = missingTimes[zone];
    if (gap !== undefined) {
        it(`keeps the draft when ${gap.missing} does not exist in ${zone} and saves the corrected time`, async () => {
            setDrawerCalendarMonth(gap.date);
            const { fixture, createMeal, showError } = await renderDrawerAsync();
            const host = fixture.nativeElement as HTMLElement;
            openMealDetails(host);
            selectQuickMealDate(host, gap.date);
            selectQuickMealTime(host, gap.missing);

            draftSaveButton(host).click();
            TestBed.tick();
            expect(createMeal).not.toHaveBeenCalled();
            expect(showError).toHaveBeenCalledExactlyOnceWith('MEAL_MANAGE.NONEXISTENT_TIME');
            expect(host.querySelector('.quick-meal__name')?.textContent).toBe(RECIPE.name);
            expect(quickMealTimeControl(host).value).toBe(gap.missing);

            selectQuickMealTime(host, gap.corrected);
            draftSaveButton(host).click();
            TestBed.tick();
            expect(createMeal).toHaveBeenCalledExactlyOnceWith(
                expect.objectContaining({ date: new Date(gap.expected) }),
                expect.any(String),
            );
            expect(host.querySelector('[role="region"]')).toBeNull();
        });
    }
});

function setDrawerCalendarMonth(date: string): void {
    const [year, month] = date.split('-');
    vi.useFakeTimers({ toFake: ['Date'] });
    vi.setSystemTime(new Date(`${year}-${month}-01T12:00:00`));
    onTestFinished(() => {
        vi.useRealTimers();
    });
}

function openMealDetails(drawer: HTMLElement): void {
    requiredElement(drawer.querySelector<HTMLButtonElement>('button[aria-label="MEAL_DETAILS.ADD"]'), 'the meal details action').click();
    TestBed.tick();
}

function selectQuickMealDate(drawer: HTMLElement, date: string): void {
    requiredElement(drawer.querySelector<HTMLButtonElement>('fd-ui-date-input button[aria-haspopup="dialog"]'), 'the date picker').click();
    TestBed.tick();
    requiredElement(document.querySelector<HTMLButtonElement>(`button[data-date="${date}"]`), `calendar date ${date}`).click();
    TestBed.tick();
}

function selectQuickMealTime(drawer: HTMLElement, time: string): void {
    const [hour, minute] = time.split(':');
    quickMealTimeControl(drawer).click();
    TestBed.tick();
    timeOption('HOURS', hour).click();
    TestBed.tick();
    timeOption('MINUTES', minute).click();
    TestBed.tick();
}

function quickMealTimeControl(drawer: HTMLElement): HTMLInputElement {
    return requiredElement(drawer.querySelector<HTMLInputElement>('fd-ui-time-input input[role="combobox"]'), 'the time picker');
}

function timeOption(part: 'HOURS' | 'MINUTES', value: string): HTMLButtonElement {
    const choice = Array.from(document.querySelectorAll<HTMLButtonElement>(`[aria-label="MEAL_DETAILS.${part}"] button`)).find(
        option => option.textContent.trim() === value,
    );
    return requiredElement(choice ?? null, `time option ${value}`);
}

async function renderDrawerAsync(): Promise<{
    fixture: ComponentFixture<QuickMealDrawerComponent>;
    lookupRecipe: Mock<RecipeLookupService['getById']>;
    createMeal: Mock<MealService['create']>;
    showError: Mock<FdUiToastService['error']>;
}> {
    vi.stubGlobal(
        'matchMedia',
        vi.fn(() => ({ matches: false })),
    );
    const lookupRecipe = vi.fn<RecipeLookupService['getById']>();
    const createMeal = vi.fn<MealService['create']>().mockReturnValue(of(CREATED));
    const showError = vi.fn<FdUiToastService['error']>();
    TestBed.configureTestingModule({
        imports: [QuickMealDrawerComponent],
        providers: [
            provideRouter([]),
            provideTranslateTesting(),
            { provide: AuthService, useValue: { isPremium: signal(false) } },
            { provide: MealService, useValue: { create: createMeal } },
            { provide: RecipeLookupService, useValue: { getById: lookupRecipe } },
            { provide: FdUiToastService, useValue: { success: vi.fn(), error: showError } },
        ],
    });
    const dialogs = TestBed.inject(FdUiDialogService);
    onTestFinished(() => {
        dialogs.closeAll();
        vi.unstubAllGlobals();
    });
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('en', { QUICK_MEAL: { EDIT_ITEM_NAMED: 'Edit {{name}}', SERVINGS_OTHER: 'servings' } });
    await firstValueFrom(translate.use('en'));
    TestBed.inject(QuickMealService).addRecipe(RECIPE, INITIAL_SERVINGS);
    const fixture = TestBed.createComponent(QuickMealDrawerComponent);
    fixture.detectChanges();
    return { fixture, lookupRecipe, createMeal, showError };
}

function editButton(host: HTMLElement): HTMLButtonElement {
    return requiredElement(host.querySelector<HTMLButtonElement>(`button[aria-label="Edit ${RECIPE.name}"]`), 'the recipe edit button');
}

function draftSaveButton(host: HTMLElement): HTMLButtonElement {
    return requiredElement(host.querySelector<HTMLButtonElement>('button[aria-label="QUICK_MEAL.SAVE_ARIA"]'), 'the draft save button');
}

async function visibleEditorAsync(): Promise<HTMLElement> {
    return vi.waitFor(() => {
        TestBed.tick();
        return requiredElement(document.querySelector<HTMLElement>('fd-meal-manual-item-dialog'), 'the quick meal editor');
    });
}

function amountInput(editor: HTMLElement): HTMLInputElement {
    return requiredElement(editor.querySelector<HTMLInputElement>('input[type="number"]'), 'the recipe amount input');
}

function setAmount(editor: HTMLElement, amount: number): void {
    const input = amountInput(editor);
    input.value = String(amount);
    input.dispatchEvent(new Event('input', { bubbles: true }));
    TestBed.tick();
}

function editorAction(editor: HTMLElement, label: string): HTMLButtonElement {
    const button = Array.from(editor.querySelectorAll<HTMLButtonElement>('button')).find(
        action => action.querySelector('.fd-ui-button__label')?.textContent.trim() === label,
    );
    if (button === undefined) {
        throw new Error(`Expected ${label} in the quick meal editor`);
    }
    return button;
}

function requiredElement<ElementType extends Element>(element: ElementType | null, label: string): ElementType {
    if (element === null) {
        throw new Error(`Expected ${label} in the quick meal flow`);
    }
    return element;
}
