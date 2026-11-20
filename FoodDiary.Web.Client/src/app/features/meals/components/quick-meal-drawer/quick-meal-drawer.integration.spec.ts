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
