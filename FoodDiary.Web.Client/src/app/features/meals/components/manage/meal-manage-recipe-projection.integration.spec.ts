import { Dialog } from '@angular/cdk/dialog';
import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideLocationMocks } from '@angular/common/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { firstValueFrom, of, Subject, throwError } from 'rxjs';
import { describe, expect, it, onTestFinished, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { AuthService } from '../../../../services/auth.service';
import { RecipeLookupService } from '../../../../shared/api/recipe-lookup.service';
import { createEmptyRecipeSnapshot, type Meal, MealSourceType } from '../../../../shared/models/meal.data';
import type { Recipe } from '../../../../shared/models/recipe.data';
import { recipeIngredientFromStored } from '../../../../shared/models/recipe-ingredient';
import type { RecipeLookup } from '../../../../shared/models/recipe-lookup.data';
import { utcInstant } from '../../../../shared/models/semantics/date-value';
import { entityId } from '../../../../shared/models/semantics/entity-id';
import { recipeServingsFromStored } from '../../../../shared/models/semantics/meal-quantity';
import { PRIVACY_NOTICE_VERSION } from '../../../../shared/privacy/privacy-notice-version';
import { MEAL_DEVELOPMENT_TOOLS, type MealDevelopmentTools } from '../../contracts/meal-development-tools';
import { QuickMealService } from '../../lib/quick/quick-meal.service';
import { QuickMealDrawerComponent } from '../quick-meal-drawer/quick-meal-drawer';
import { MealManageFormComponent } from './meal-manage-form';

const KNOWN_MASS = 300;
const RECIPE_SERVINGS = 2;
const DISPLAY_GRAMS = 150;
const CHANGED_DISPLAY_GRAMS = 300;
const STORED_SERVINGS = 1;
const CALORIES_PER_SERVING = 200;
const SOUP = { ...recipeSnapshot('prepared-soup', 'Prepared soup'), totalCalories: CALORIES_PER_SERVING * RECIPE_SERVINGS };
const TEA = recipeSnapshot('instruction-only-tea', 'Steeped tea');
const MEASURED_SOUP: RecipeLookup = {
    id: SOUP.id,
    servings: RECIPE_SERVINGS,
    steps: [{ ingredients: [{ amount: KNOWN_MASS, productBaseUnit: 'G' }] }],
};
const UPDATED_SOUP: Recipe = {
    ...SOUP,
    steps: [
        {
            id: 'updated-step',
            stepNumber: 1,
            instruction: '',
            ingredients: [
                recipeIngredientFromStored({
                    id: 'updated-ingredient',
                    amount: CHANGED_DISPLAY_GRAMS * RECIPE_SERVINGS,
                    productBaseUnit: 'G',
                }),
            ],
        },
    ],
};

describe('Saved meal recipe quantities in the editor and its registered development screen', () => {
    it('reports grams for a measured recipe and servings for an instruction-only recipe', async () => {
        const measured = new Subject<RecipeLookup>();
        const instructionOnly = new Subject<RecipeLookup>();
        const lookup = vi.fn<RecipeLookupService['getById']>().mockReturnValueOnce(measured).mockReturnValueOnce(instructionOnly);
        const detach = vi.fn<() => void>();
        const attach = vi.fn<MealDevelopmentTools['attach']>().mockReturnValue(detach);
        await setupEditorAsync(lookup, { attach });
        const fixture = TestBed.createComponent(MealManageFormComponent);
        const host = fixture.nativeElement as HTMLElement;
        fixture.componentRef.setInput('meal', savedMeal([SOUP, TEA]));
        fixture.detectChanges();

        expect(lookup.mock.calls).toEqual([[SOUP.id], [TEA.id]]);
        expect(host.querySelector('form')?.style.display).toBe('none');
        measured.next({ id: SOUP.id, servings: RECIPE_SERVINGS, steps: [{ ingredients: [{ amount: KNOWN_MASS, productBaseUnit: 'G' }] }] });
        measured.complete();
        fixture.detectChanges();
        expect(host.querySelector('form')?.style.display).toBe('none');
        instructionOnly.next({ id: TEA.id, servings: RECIPE_SERVINGS, steps: [{ ingredients: [] }] });
        instructionOnly.complete();
        await fixture.whenStable();
        fixture.detectChanges();

        expect(host.querySelector('form')?.style.display).toBe('');
        const amounts = host.querySelectorAll('.meal-manage__manual-summary .meal-manage__ai-summary-amount');
        expect(Array.from(amounts, amount => amount.textContent.trim())).toEqual(['150 g', '1 serving']);
        expect(attach).toHaveBeenCalledOnce();
        const [[screen]] = attach.mock.calls;
        expect(screen.read()).toMatchObject({
            kind: 'edit-meal-editor',
            dirty: false,
            items: [
                { recipeId: SOUP.id, amount: DISPLAY_GRAMS, servings: STORED_SERVINGS, unit: 'grams' },
                { recipeId: TEA.id, amount: STORED_SERVINGS, servings: STORED_SERVINGS, unit: 'servings' },
            ],
        });
        fixture.destroy();
        expect(detach).toHaveBeenCalledOnce();
    });
});

describe('Independent recipe quantity drafts', () => {
    it.each([
        {
            action: 'recovers a previously unavailable mass',
            firstLookup: throwError(() => new Error('Lookup unavailable')),
            savedLabel: '1 serving',
            nextRecipe: SOUP,
            nextAmount: DISPLAY_GRAMS,
            needsSecondLookup: true,
        },
        {
            action: 'opens a newer gram recipe snapshot',
            firstLookup: of(MEASURED_SOUP),
            savedLabel: '150 g',
            nextRecipe: UPDATED_SOUP,
            nextAmount: CHANGED_DISPLAY_GRAMS,
            needsSecondLookup: false,
        },
    ])(
        'retains the open editor quantity when Quick Meal $action',
        async ({ firstLookup, savedLabel, nextRecipe, nextAmount, needsSecondLookup }) => {
            const lookup = vi.fn<RecipeLookupService['getById']>().mockReturnValueOnce(firstLookup);
            lookup.mockReturnValueOnce(of(MEASURED_SOUP));
            await setupEditorAsync(lookup, { attach: (): (() => void) => (): void => undefined });
            const editor = TestBed.createComponent(MealManageFormComponent);
            const editorHost = editor.nativeElement as HTMLElement;
            const stored = savedMeal([SOUP]);
            editor.componentRef.setInput('meal', stored);
            editor.detectChanges();
            expect(editorHost.querySelector('.meal-manage__manual-summary .meal-manage__ai-summary-amount')?.textContent.trim()).toBe(
                savedLabel,
            );

            TestBed.inject(QuickMealService).addRecipe(nextRecipe);
            const drawer = TestBed.createComponent(QuickMealDrawerComponent);
            const drawerHost = drawer.nativeElement as HTMLElement;
            drawer.detectChanges();
            const opened = firstValueFrom(TestBed.inject(Dialog).afterOpened);
            requiredElement(drawerHost.querySelector<HTMLButtonElement>('button[aria-label="QUICK_MEAL.EDIT_ITEM_NAMED"]')).click();
            const dialog = await opened;
            await drawer.whenStable();
            drawer.detectChanges();
            const modal = requiredElement(dialog.overlayRef.overlayElement.querySelector<HTMLElement>('fd-meal-manual-item-dialog'));
            const amount = requiredElement(modal.querySelector<HTMLInputElement>('input[type="number"]'));
            expect(amount.valueAsNumber).toBe(nextAmount);
            expect(lookup.mock.calls).toEqual(needsSecondLookup ? [[SOUP.id], [SOUP.id]] : [[SOUP.id]]);
            const cancel = Array.from(modal.querySelectorAll('button')).find(
                button => button.textContent.trim() === 'PRODUCT_MANAGE.CANCEL_BUTTON',
            );
            requiredElement(cancel ?? null).click();

            const note = requiredElement(editorHost.querySelector('textarea'));
            note.value = 'Keep the saved serving count';
            note.dispatchEvent(new Event('input', { bubbles: true }));
            editor.detectChanges();
            expect(editorHost.querySelector('.meal-manage__manual-summary .meal-manage__ai-summary-amount')?.textContent.trim()).toBe(
                savedLabel,
            );
            expect(
                editorHost
                    .querySelector('.meal-manage__manual-summary .meal-manage__ai-summary-calories span:last-child')
                    ?.textContent.trim(),
            ).toBe(`${CALORIES_PER_SERVING} kcal`);
            requiredElement(editorHost.querySelector('form')).requestSubmit();

            const http = TestBed.inject(HttpTestingController);
            const privacy = await vi.waitFor(() =>
                http.expectOne(request => request.method === 'GET' && request.url.endsWith('/api/v1/users/privacy')),
            );
            privacy.flush({ noticeVersion: PRIVACY_NOTICE_VERSION, healthAcceptedAtUtc: '2026-11-21T09:00:00Z' });
            const update = await vi.waitFor(() => http.expectOne(request => request.method === 'PATCH' && request.url.endsWith(stored.id)));
            expect(update.request.body).toMatchObject({ items: [{ recipeId: SOUP.id, productId: null, amount: STORED_SERVINGS }] });
            update.flush(null, { status: HttpStatusCode.ServiceUnavailable, statusText: 'Service Unavailable' });
            await editor.whenStable();
        },
    );
});

function requiredElement<T extends Element>(element: T | null): T {
    if (element === null) {
        throw new Error('Expected the rendered meal editor control');
    }
    return element;
}

async function setupEditorAsync(getById: RecipeLookupService['getById'], tools: MealDevelopmentTools): Promise<void> {
    vi.stubGlobal(
        'matchMedia',
        vi.fn(() => ({ matches: false })),
    );
    TestBed.configureTestingModule({
        imports: [MealManageFormComponent, QuickMealDrawerComponent],
        providers: [
            provideHttpClient(),
            provideHttpClientTesting(),
            provideRouter([]),
            provideLocationMocks(),
            provideTranslateTesting(),
            {
                provide: AuthService,
                useValue: { isAuthenticated: signal(true), isPremium: signal(false), getUserId: (): string => 'meal-editor-user' },
            },
            { provide: RecipeLookupService, useValue: { getById } },
            { provide: MEAL_DEVELOPMENT_TOOLS, useValue: tools },
        ],
    });
    const http = TestBed.inject(HttpTestingController);
    const dialogs = TestBed.inject(FdUiDialogService);
    onTestFinished(() => {
        dialogs.closeAll();
        http.verify();
        vi.unstubAllGlobals();
    });
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('en', { GENERAL: { UNITS: { G: 'g', KCAL: 'kcal' } }, MEAL_MANAGE: { AMOUNT_UNIT_SERVING: 'serving' } });
    await firstValueFrom(translate.use('en'));
}

function recipeSnapshot(id: string, name: string): Recipe {
    return { ...createEmptyRecipeSnapshot(), id: entityId<'recipe'>(id), name, servings: RECIPE_SERVINGS };
}

function savedMeal(recipes: readonly Recipe[]): Meal {
    const id = entityId<'meal'>('recipe-portion-projection');
    return {
        id,
        date: utcInstant('2026-11-20T12:00:00Z'),
        mealType: 'LUNCH',
        isNutritionAutoCalculated: true,
        totalCalories: 0,
        totalProteins: 0,
        totalFats: 0,
        totalCarbs: 0,
        totalFiber: 0,
        totalAlcohol: 0,
        items: recipes.map(recipe => ({
            id: entityId<'meal-item'>(`item-${recipe.id}`),
            mealId: id,
            sourceType: MealSourceType.Recipe,
            amount: recipeServingsFromStored(STORED_SERVINGS),
            recipe,
            product: null,
        })),
    };
}
