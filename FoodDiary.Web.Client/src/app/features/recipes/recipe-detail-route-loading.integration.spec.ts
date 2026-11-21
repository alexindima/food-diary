import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { type Observable, of, Subject } from 'rxjs';
import { expect, it, type MockInstance, onTestFinished, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../testing/async-testing';
import { provideTranslateTesting } from '../../../testing/translate-testing.module';
import { AuthService } from '../../services/auth.service';
import { type Recipe, type RecipeOverview, RecipeVisibility } from '../../shared/models/recipe.data';
import { utcInstant } from '../../shared/models/semantics/date-value';
import { entityId } from '../../shared/models/semantics/entity-id';
import { ViewportService } from '../../shared/platform/viewport.service';
import { RecipeService } from './api/recipe.service';
import { RecipeListFacade } from './lib/recipe-list.facade';
import routes from './recipe.routes';

const RECIPE: Recipe = {
    id: entityId<'recipe'>('avocado-soup'),
    name: 'Avocado soup',
    servings: 2,
    visibility: RecipeVisibility.Private,
    usageCount: 0,
    createdAt: utcInstant('2026-05-17T09:00:00.000Z'),
    isOwnedByCurrentUser: true,
    isNutritionAutoCalculated: true,
    steps: [],
};

const OTHER_RECIPE: Recipe = {
    ...RECIPE,
    id: entityId<'recipe'>('spinach-salad'),
    name: 'Spinach salad',
};

it('drops a recipe detail after the read resolves and the list view is destroyed', async () => {
    const reply = new Subject<Recipe>();
    const { dialogs, read, harness } = await openCatalogueAsync(reply);
    const lookup = trackDetailReads(harness);
    const show = vi.spyOn(dialogs, 'open');
    openRecipe(harness);
    const completion = startedDetailRead(lookup);
    expect(read).toHaveBeenCalledExactlyOnceWith(RECIPE.id, true);
    expect(reply.observed).toBe(true);
    reply.next(RECIPE);
    reply.complete();
    // The page awaited this promise before the test. Its continuation starts the actual import first.
    expect(await completion.value).toBe(RECIPE);
    harness.fixture.destroy();
    await vi.dynamicImportSettled();
    await waitForAsyncTasksAsync();
    expect(show).not.toHaveBeenCalled();
    expect(document.querySelector('fd-recipe-detail')).toBeNull();
});

it('opens the real lazy detail for a catalogue that stays active', async () => {
    const { harness } = await openCatalogueAsync(of(RECIPE));
    openRecipe(harness);
    await waitForDetailAsync(RECIPE);
});

it.each(['succeeds', 'fails'] as const)('keeps the most recently selected recipe open when the earlier read %s', async outcome => {
    const earlier = new Subject<Recipe>();
    const latest = new Subject<Recipe>();
    const { harness, read } = await openCatalogueAsync(earlier, [RECIPE, OTHER_RECIPE]);
    read.mockImplementation(id => (id === RECIPE.id ? earlier : latest));

    openRecipe(harness, RECIPE);
    openRecipe(harness, OTHER_RECIPE);
    expect(read.mock.calls).toEqual([
        [RECIPE.id, true],
        [OTHER_RECIPE.id, true],
    ]);
    expect(latest.observed).toBe(true);
    const earlierReadCancelled = !earlier.observed;

    latest.next(OTHER_RECIPE);
    latest.complete();
    await waitForDetailAsync(OTHER_RECIPE);

    if (outcome === 'fails') {
        earlier.error(new Error('The previous recipe read failed'));
    } else {
        earlier.next(RECIPE);
        earlier.complete();
    }
    await vi.dynamicImportSettled();
    await waitForAsyncTasksAsync();

    const details = Array.from(document.querySelectorAll('fd-recipe-detail'));
    expect(details).toHaveLength(1);
    expect(details[0]?.textContent).toContain(OTHER_RECIPE.name);
    expect(TestBed.inject(FdUiToastService).toasts()).toEqual([]);
    expect(earlierReadCancelled).toBe(true);
});

it('discards a detail already loading its dialog when a different recipe is selected', async () => {
    const earlier = new Subject<Recipe>();
    const latest = new Subject<Recipe>();
    const { harness, read } = await openCatalogueAsync(earlier, [RECIPE, OTHER_RECIPE]);
    read.mockImplementation(id => (id === RECIPE.id ? earlier : latest));
    const lookup = trackDetailReads(harness);
    openRecipe(harness, RECIPE);
    const completion = startedDetailRead(lookup);
    earlier.next(RECIPE);
    earlier.complete();
    // The page has started the native import before its lookup promise resumes this test.
    expect(await completion.value).toBe(RECIPE);
    openRecipe(harness, OTHER_RECIPE);
    await vi.dynamicImportSettled();

    expect(latest.observed).toBe(true);
    expect(document.querySelector('fd-recipe-detail')).toBeNull();
    latest.next(OTHER_RECIPE);
    latest.complete();
    await waitForDetailAsync(OTHER_RECIPE);
});

it('notifies about a failed current selection and opens its details on retry', async () => {
    const failed = new Subject<Recipe>();
    const { harness, read } = await openCatalogueAsync(failed);
    openRecipe(harness);
    expect(failed.observed).toBe(true);
    failed.error(new Error('The current recipe read failed'));

    await vi.waitFor(() => {
        expect(
            TestBed.inject(FdUiToastService)
                .toasts()
                .map(toast => toast.message),
        ).toEqual(['ERRORS.LOAD_FAILED_MESSAGE']);
    });
    expect(document.querySelector('fd-recipe-detail')).toBeNull();

    read.mockReturnValue(of(RECIPE));
    openRecipe(harness);
    await waitForDetailAsync(RECIPE);
});

type CatalogueFixture = {
    dialogs: FdUiDialogService;
    read: ReturnType<typeof vi.fn<RecipeService['getById']>>;
    harness: RouterTestingHarness;
};

async function openCatalogueAsync(response: Observable<Recipe | null>, recipes = [RECIPE]): Promise<CatalogueFixture> {
    const read = vi.fn<RecipeService['getById']>().mockReturnValue(response);
    const overview: RecipeOverview = {
        allRecipes: { data: recipes, page: 1, limit: 10, totalItems: recipes.length, totalPages: 1 },
        recentItems: [],
        favoriteItems: [],
        favoriteTotalCount: 0,
    };
    TestBed.configureTestingModule({
        providers: [
            provideRouter([{ path: 'recipes', children: routes }], withComponentInputBinding()),
            provideHttpClient(),
            provideHttpClientTesting(),
            provideTranslateTesting({ lang: 'en', fallbackLang: 'en' }),
            {
                provide: AuthService,
                useValue: { getUserId: (): string => 'recipe-owner', isAuthenticated: signal(true), isPremium: signal(false) },
            },
            { provide: ViewportService, useValue: { isMobile: signal(true) } },
            {
                provide: RecipeService,
                useValue: { getById: read, queryOverview: (): ReturnType<RecipeService['queryOverview']> => of(overview) },
            },
        ],
    });
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
    const dialogs = TestBed.inject(FdUiDialogService);
    onTestFinished(() => {
        dialogs.closeAll();
        vi.unstubAllGlobals();
    });
    const harness = await RouterTestingHarness.create('/recipes');
    return { dialogs, read, harness };
}

function openRecipe(harness: RouterTestingHarness, recipe = RECIPE): void {
    const button = harness.routeNativeElement?.querySelector(`button[aria-label="${recipe.name}"]`);
    if (!(button instanceof HTMLButtonElement)) {
        throw new Error('Expected the rendered recipe action');
    }
    button.click();
}

function trackDetailReads(harness: RouterTestingHarness): MockInstance<RecipeListFacade['getRecipeDetailAsync']> {
    const list = harness.routeDebugElement?.queryAll(By.css('fd-recipe-list')).at(0);
    if (list === undefined) {
        throw new Error('Expected the rendered recipe list');
    }
    return vi.spyOn(list.injector.get(RecipeListFacade), 'getRecipeDetailAsync');
}

function startedDetailRead(lookup: MockInstance<RecipeListFacade['getRecipeDetailAsync']>): { value: Promise<Recipe | null> } {
    const completion = lookup.mock.results.at(0);
    if (completion?.type !== 'return') {
        throw new Error('Expected the real recipe read to start');
    }
    return completion;
}

async function waitForDetailAsync(recipe: Recipe): Promise<void> {
    await vi.waitFor(() => {
        expect(document.querySelector('fd-recipe-detail')?.textContent).toContain(recipe.name);
    });
}
