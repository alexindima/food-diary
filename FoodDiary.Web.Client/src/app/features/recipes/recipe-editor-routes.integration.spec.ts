import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdTourService } from 'fd-tour';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { firstValueFrom, of, throwError } from 'rxjs';
import { afterEach, beforeEach, expect, it, onTestFinished, vi } from 'vitest';

import { provideTranslateTesting } from '../../../testing/translate-testing.module';
import { AuthService } from '../../services/auth.service';
import { type Recipe, type RecipeOverview, RecipeVisibility } from '../../shared/models/recipe.data';
import { utcInstant } from '../../shared/models/semantics/date-value';
import { entityId } from '../../shared/models/semantics/entity-id';
import { ViewportService } from '../../shared/platform/viewport.service';
import { RecipeService } from './api/recipe.service';
import routes from './recipe.routes';

const RECIPES = '/recipes';
const ADD_RECIPE = '/recipes/add';
const EDIT_RECIPE = '/recipes/recipe-editor-record/edit';
const PAGE_SIZE = 10;
const UPDATED_SERVINGS = 3;
const LANGUAGE_DIALOG = 'fd-recipe-language-dialog';
const INITIAL_INSTRUCTIONS =
    'Heat the oil in a pan and add the chopped tomatoes with salt. Stir and cook until the sauce thickens, then pour in the water and let it simmer for ten minutes.';
const REVISED_INSTRUCTIONS =
    'Place the sliced onions in the oven and bake until the edges turn brown. Remove the tray, mix the roasted vegetables with the herbs, then serve it with warm bread.';
const RECIPE: Recipe = {
    id: entityId<'recipe'>('recipe-editor-record'),
    name: 'Tomato soup',
    servings: 2,
    visibility: RecipeVisibility.Private,
    usageCount: 0,
    createdAt: utcInstant('2026-05-17T09:00:00.000Z'),
    isOwnedByCurrentUser: true,
    isNutritionAutoCalculated: true,
    steps: [],
    category: 'soups',
};
const OVERVIEW: RecipeOverview = {
    allRecipes: { data: [RECIPE], page: 1, limit: PAGE_SIZE, totalItems: 1, totalPages: 1 },
    recentItems: [],
    favoriteItems: [],
    favoriteTotalCount: 0,
};

const readRecipe = vi.fn<RecipeService['getById']>();
const loadRecipes = vi.fn<RecipeService['queryOverview']>();
const updateRecipe = vi.fn<RecipeService['update']>();

beforeEach(() => {
    readRecipe.mockReset().mockReturnValue(of(RECIPE));
    loadRecipes.mockReset().mockReturnValue(of(OVERVIEW));
    updateRecipe.mockReset();
    vi.stubGlobal('matchMedia', (media: string) => ({
        matches: false,
        media,
        onchange: null,
        addListener: vi.fn(),
        removeListener: vi.fn(),
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
        dispatchEvent: vi.fn(() => true),
    }));
    TestBed.configureTestingModule({
        providers: [
            provideRouter([{ path: 'recipes', children: routes }], withComponentInputBinding()),
            provideHttpClient(),
            provideHttpClientTesting(),
            provideTranslateTesting({ lang: 'en', fallbackLang: 'en' }),
            {
                provide: AuthService,
                useValue: { getUserId: (): string => 'recipe-editor-owner', isAuthenticated: signal(true), isPremium: signal(false) },
            },
            { provide: ViewportService, useValue: { isMobile: signal(true) } },
            { provide: RecipeService, useValue: { getById: readRecipe, queryOverview: loadRecipes, update: updateRecipe } },
        ],
    });
});

afterEach(() => {
    vi.unstubAllGlobals();
});

it('opens the lazy add route from its visible action and enables entering a recipe', async () => {
    const harness = await RouterTestingHarness.create(RECIPES);
    const router = TestBed.inject(Router);
    await harness.fixture.whenStable();
    harness.detectChanges();
    expect(loadRecipes).toHaveBeenCalled();
    const add = getButton(harness.routeNativeElement, 'button[aria-label="RECIPE_LIST.ADD_RECIPE_BUTTON"]');
    expect(add.disabled).toBe(false);
    add.click();
    await vi.waitFor(() => {
        harness.detectChanges();
        expect(router.url).toBe(ADD_RECIPE);
        expect(harness.routeNativeElement?.querySelector('fd-recipe-basic-info input')).not.toBeNull();
    });
    const name = recipeName(harness);
    name.value = 'A new soup';
    name.dispatchEvent(new Event('input', { bubbles: true }));
    harness.detectChanges();

    expect(recipeName(harness).value).toBe('A new soup');
});

it('binds the resolved owned recipe to the lazy edit form', async () => {
    const harness = await RouterTestingHarness.create(EDIT_RECIPE);

    expect(TestBed.inject(Router).url).toBe(EDIT_RECIPE);
    expect(readRecipe).toHaveBeenCalledExactlyOnceWith(RECIPE.id, false);
    expect(recipeName(harness).value).toBe(RECIPE.name);
});

it.each([
    { title: 'a missing recipe', response: of(null) },
    { title: 'a recipe in use', response: of({ ...RECIPE, usageCount: 1 }) },
    { title: 'another owner’s recipe', response: of({ ...RECIPE, isOwnedByCurrentUser: false }) },
    { title: 'a temporary read error', response: throwError(() => new Error('Connection interrupted')) },
])('returns to the lazy list when the editor receives $title', async ({ response }) => {
    readRecipe.mockReturnValueOnce(response);
    const harness = await RouterTestingHarness.create(EDIT_RECIPE);
    const router = TestBed.inject(Router);

    await vi.waitFor(() => {
        harness.detectChanges();
        expect(router.url).toBe(RECIPES);
        expect(harness.routeNativeElement?.querySelector('fd-recipe-basic-info')).toBeNull();
        expect(harness.routeNativeElement?.querySelector('button[aria-label="RECIPE_LIST.ADD_RECIPE_BUTTON"]')).not.toBeNull();
    });
    expect(readRecipe).toHaveBeenCalledExactlyOnceWith(RECIPE.id, false);
});

it('keeps the saved language choice for a servings edit after changing the editor language', async () => {
    const stored = confirmedRecipe();
    readRecipe.mockReturnValueOnce(of(stored));
    updateRecipe.mockReturnValueOnce(of({ ...stored, servings: UPDATED_SERVINGS }));
    const harness = await RouterTestingHarness.create(EDIT_RECIPE);
    const startTour = vi.spyOn(TestBed.inject(FdTourService), 'start').mockReturnValue(true);
    await firstValueFrom(TestBed.inject(TranslateService).use('ru'));
    harness.detectChanges();
    getButton(harness.routeNativeElement, 'button[aria-label="RECIPE_MANAGE.TOUR.START_TOUR"]').click();
    expect(startTour).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ id: 'recipe-manage' }), { force: true });

    const label = [...(harness.routeNativeElement?.querySelectorAll('label') ?? [])].find(element =>
        element.textContent.includes('RECIPE_MANAGE.SERVINGS'),
    );
    const servings = label?.control;
    if (!(servings instanceof HTMLInputElement)) {
        throw new Error('Expected the servings input on the recipe editor');
    }
    typeText(servings, String(UPDATED_SERVINGS));
    harness.detectChanges();
    getButton(harness.routeNativeElement, 'button[type="submit"]').click();

    await vi.waitFor(() => {
        expect(updateRecipe).toHaveBeenCalledExactlyOnceWith(
            stored.id,
            expect.objectContaining({ servings: UPDATED_SERVINGS, language: 'ru', languageConfirmed: true }),
        );
    });
    expect(document.querySelector(LANGUAGE_DIALOG)).toBeNull();
    await vi.waitFor(() => {
        expect(TestBed.inject(Router).url).toBe(RECIPES);
    });
});

it('rechecks the language after rewriting a step and preserves the draft when the user goes back', async () => {
    const stored = confirmedRecipe();
    readRecipe.mockReturnValueOnce(of(stored));
    updateRecipe.mockReturnValueOnce(of({ ...stored, language: 'en', languageConfirmed: false }));
    const harness = await RouterTestingHarness.create(EDIT_RECIPE);
    const dialogs = TestBed.inject(FdUiDialogService);
    onTestFinished(() => {
        dialogs.closeAll();
    });
    const input = harness.routeNativeElement?.querySelector('fd-recipe-step-card textarea');
    if (!(input instanceof HTMLTextAreaElement)) {
        throw new Error('Expected the native recipe step description');
    }
    const fields = input.closest('fieldset');
    if (fields === null) {
        throw new Error('Expected the editable recipe fieldset');
    }
    typeText(input, REVISED_INSTRUCTIONS);
    harness.detectChanges();
    const submit = getButton(harness.routeNativeElement, 'button[type="submit"]');
    submit.click();

    const back = await recipeLanguageActionAsync(harness, 'RECIPE_LANGUAGE.BACK');
    expect(fields.disabled).toBe(true);
    back.click();
    await vi.waitFor(() => {
        harness.detectChanges();
        expect(document.querySelector(LANGUAGE_DIALOG)).toBeNull();
        expect(fields.disabled).toBe(false);
    });
    expect(input.value).toBe(REVISED_INSTRUCTIONS);
    expect(updateRecipe).not.toHaveBeenCalled();

    getButton(harness.routeNativeElement, 'button[type="submit"]').click();
    const change = await recipeLanguageActionAsync(harness, 'RECIPE_LANGUAGE.CHANGE');
    change.click();
    await vi.waitFor(() => {
        expect(updateRecipe).toHaveBeenCalledExactlyOnceWith(
            stored.id,
            expect.objectContaining({ language: 'en', languageConfirmed: false }),
        );
    });
    expect(updateRecipe.mock.lastCall?.[1].steps[0]?.description).toBe(REVISED_INSTRUCTIONS);
});

function confirmedRecipe(): Recipe {
    return {
        ...RECIPE,
        language: 'ru',
        languageConfirmed: true,
        steps: [{ id: 'recipe-editor-step', stepNumber: 1, instruction: INITIAL_INSTRUCTIONS, ingredients: [] }],
    };
}

function typeText(input: HTMLInputElement | HTMLTextAreaElement, value: string): void {
    input.value = value;
    input.dispatchEvent(new Event('input', { bubbles: true }));
}

async function recipeLanguageActionAsync(harness: RouterTestingHarness, label: string): Promise<HTMLButtonElement> {
    return vi.waitFor(() => {
        TestBed.tick();
        harness.detectChanges();
        const dialog = document.querySelector(LANGUAGE_DIALOG);
        if (!(dialog instanceof HTMLElement)) {
            throw new Error(`Expected the recipe language confirmation for ${label}`);
        }
        const button = [...dialog.querySelectorAll('button')].find(candidate => candidate.textContent.includes(label));
        if (button === undefined) {
            throw new Error(`Expected the recipe language action ${label}`);
        }
        return button;
    });
}

function getButton(host: ParentNode | null, selector: string): HTMLButtonElement {
    const button = host?.querySelector(selector);
    if (!(button instanceof HTMLButtonElement)) {
        throw new Error('Expected the rendered recipe action');
    }
    return button;
}

function recipeName(harness: RouterTestingHarness): HTMLInputElement {
    const name = harness.routeNativeElement?.querySelector('fd-recipe-basic-info input');
    if (!(name instanceof HTMLInputElement)) {
        throw new Error('Expected the rendered recipe name input');
    }
    return name;
}
