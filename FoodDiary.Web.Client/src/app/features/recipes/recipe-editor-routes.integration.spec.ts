import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { of, throwError } from 'rxjs';
import { afterEach, beforeEach, expect, it, vi } from 'vitest';

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

beforeEach(() => {
    readRecipe.mockReset().mockReturnValue(of(RECIPE));
    loadRecipes.mockReset().mockReturnValue(of(OVERVIEW));
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
            { provide: RecipeService, useValue: { getById: readRecipe, queryOverview: loadRecipes } },
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
