import { Location, PlatformLocation } from '@angular/common';
import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting, type TestRequest } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { NavigationEnd, provideRouter, Router, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdTourService } from 'fd-tour';
import { provideFdUiDialogNavigation } from 'fd-ui-kit';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { WebmcpScreenRegistry } from 'fd-webmcp';
import { filter, firstValueFrom, NEVER, of, Subject, throwError } from 'rxjs';
import { afterEach, beforeEach, expect, it, onTestFinished, vi } from 'vitest';

import { environment } from '../../../environments/environment';
import { waitForAsyncTasksAsync } from '../../../testing/async-testing';
import { stubScrollIntoView } from '../../../testing/dom-testing';
import { provideTranslateTesting } from '../../../testing/translate-testing.module';
import { AuthService } from '../../services/auth.service';
import { NavigationService } from '../../services/navigation.service';
import type { FavoriteRecipeHttpResponse } from '../../shared/api/sdk/generated/model/favorite-recipe-http-response';
import type { ProductHttpResponse } from '../../shared/api/sdk/generated/model/product-http-response';
import type { RecipeHttpResponse } from '../../shared/api/sdk/generated/model/recipe-http-response';
import { recipeFromSdk } from '../../shared/api/sdk/recipe-sdk.mapper';
import { DEVELOPMENT_SCREENS } from '../../shared/development/development-tools';
import { UserFacade } from '../../shared/lib/user.facade';
import { type Recipe, type RecipeOverview, RecipeVisibility } from '../../shared/models/recipe.data';
import { recipeIngredientFromStored } from '../../shared/models/recipe-ingredient';
import { utcInstant } from '../../shared/models/semantics/date-value';
import { entityId } from '../../shared/models/semantics/entity-id';
import { BrowserClipboardService } from '../../shared/platform/browser-clipboard.service';
import { ViewportService } from '../../shared/platform/viewport.service';
import { ProductPublicationService } from '../products/contracts/product-publication';
import { RecipeService } from './api/recipe.service';
import routes from './recipe.routes';

const RECIPES = '/recipes';
const ADD_RECIPE = '/recipes/add';
const EDIT_RECIPE = '/recipes/recipe-editor-record/edit';
const PAGE_SIZE = 10;
const UPDATED_SERVINGS = 3;
const LANGUAGE_DIALOG = 'fd-recipe-language-dialog';
const PUBLICATION_DIALOG = 'fd-recipe-publication-dialog';
const INITIAL_INSTRUCTIONS =
    'Heat the oil in a pan and add the chopped tomatoes with salt. Stir and cook until the sauce thickens, then pour in the water and let it simmer for ten minutes.';
const REVISED_INSTRUCTIONS =
    'Place the sliced onions in the oven and bake until the edges turn brown. Remove the tray, mix the roasted vegetables with the herbs, then serve it with warm bread.';
const RECIPE = {
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
} satisfies Recipe;
const OVERVIEW: RecipeOverview = {
    allRecipes: { data: [RECIPE], page: 1, limit: PAGE_SIZE, totalItems: 1, totalPages: 1 },
    recentItems: [],
    favoriteItems: [],
    favoriteTotalCount: 0,
};

const readRecipe = vi.fn<RecipeService['getById']>();
const loadRecipes = vi.fn<RecipeService['queryOverview']>();
const createRecipe = vi.fn<RecipeService['create']>();
const deleteRecipe = vi.fn<RecipeService['deleteById']>();
const updateRecipe = vi.fn<RecipeService['update']>();

beforeEach(() => {
    readRecipe.mockReset().mockReturnValue(of(RECIPE));
    loadRecipes.mockReset().mockReturnValue(of(OVERVIEW));
    createRecipe.mockReset().mockReturnValue(of(RECIPE));
    deleteRecipe.mockReset();
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
            provideFdUiDialogNavigation(),
            provideRouter([{ path: 'recipes', children: routes }], withComponentInputBinding()),
            provideHttpClient(),
            provideHttpClientTesting(),
            provideTranslateTesting({ lang: 'en', fallbackLang: 'en' }),
            {
                provide: AuthService,
                useValue: { getUserId: (): string => 'recipe-editor-owner', isAuthenticated: signal(true), isPremium: signal(false) },
            },
            { provide: ViewportService, useValue: { isMobile: signal(true) } },
            {
                provide: RecipeService,
                useValue: {
                    getById: readRecipe,
                    queryOverview: loadRecipes,
                    create: createRecipe,
                    deleteById: deleteRecipe,
                    update: updateRecipe,
                },
            },
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
    const opened = firstValueFrom(
        router.events.pipe(filter(event => event instanceof NavigationEnd && event.urlAfterRedirects === ADD_RECIPE)),
    );
    add.click();
    await opened;
    harness.detectChanges();
    expect(router.url).toBe(ADD_RECIPE);
    const name = recipeName(harness);
    name.value = 'A new soup';
    name.dispatchEvent(new Event('input', { bubbles: true }));
    harness.detectChanges();

    expect(recipeName(harness).value).toBe('A new soup');
});

it('keeps a manually replaced recipe source when an older clipboard paste finishes', async () => {
    const manuallyEntered = 'https://example.test/new-source';
    const explicitPaste = 'https://example.test/next-paste';
    const olderClipboardRead = new Subject<string>();
    const pending = firstValueFrom(olderClipboardRead);
    const clipboard = vi.spyOn(TestBed.inject(BrowserClipboardService), 'readTextAsync');
    clipboard.mockReturnValueOnce(pending).mockResolvedValueOnce(` ${explicitPaste} `);
    const harness = await RouterTestingHarness.create(ADD_RECIPE);
    const source = openRecipeImportSource(harness);
    typeText(source, 'https://example.test/original-source');
    harness.detectChanges();
    const paste = recipeTextButton(harness.routeNativeElement, 'RECIPE_MANAGE.IMPORT.PASTE_LINK');
    paste.click();
    expect(clipboard).toHaveBeenCalledOnce();

    typeText(source, manuallyEntered);
    olderClipboardRead.next('https://example.test/obsolete-paste');
    olderClipboardRead.complete();
    await pending;
    await waitForAsyncTasksAsync();
    await harness.fixture.whenStable();
    harness.detectChanges();

    expect(source.value).toBe(manuallyEntered);
    paste.click();
    await vi.waitFor(() => {
        harness.detectChanges();
        expect(source.value).toBe(explicitPaste);
    });
    expect(clipboard).toHaveBeenCalledTimes(2);
});

it('keeps the latest recipe clipboard paste when it matches the current address', async () => {
    const selectedUrl = 'https://example.test/current-source';
    const olderClipboardRead = new Subject<string>();
    const pending = firstValueFrom(olderClipboardRead);
    const clipboard = vi.spyOn(TestBed.inject(BrowserClipboardService), 'readTextAsync');
    clipboard.mockReturnValueOnce(pending).mockResolvedValueOnce(selectedUrl);
    const harness = await RouterTestingHarness.create(ADD_RECIPE);
    const source = openRecipeImportSource(harness);
    typeText(source, selectedUrl);
    harness.detectChanges();
    const paste = recipeTextButton(harness.routeNativeElement, 'RECIPE_MANAGE.IMPORT.PASTE_LINK');

    paste.click();
    paste.click();
    expect(clipboard).toHaveBeenCalledTimes(2);
    await waitForAsyncTasksAsync();
    olderClipboardRead.next('https://example.test/obsolete-paste');
    olderClipboardRead.complete();
    await pending;
    await waitForAsyncTasksAsync();
    await harness.fixture.whenStable();
    harness.detectChanges();

    expect(source.value).toBe(selectedUrl);
});

it('keeps the submitted recipe source locked when an earlier clipboard paste completes', async () => {
    const submittedUrl = 'https://example.test/tomato-soup';
    const obsoleteUrl = 'https://example.test/obsolete-recipe';
    const nextUrl = 'https://example.test/next-recipe';
    const clipboardResult = new Subject<string>();
    const pasting = firstValueFrom(clipboardResult);
    TestBed.overrideProvider(AuthService, {
        useValue: { getUserId: (): string => 'recipe-editor-owner', isAuthenticated: signal(true), isPremium: signal(true) },
    });
    TestBed.overrideProvider(UserFacade, { useValue: { user: signal({ aiConsentAcceptedAt: 'accepted' }) } });
    const clipboard = vi.spyOn(TestBed.inject(BrowserClipboardService), 'readTextAsync');
    clipboard.mockReturnValueOnce(pasting).mockResolvedValueOnce(nextUrl);
    const http = TestBed.inject(HttpTestingController);
    onTestFinished(() => {
        http.verify();
    });
    const harness = await RouterTestingHarness.create(ADD_RECIPE);
    const source = openRecipeImportSource(harness);
    typeText(source, submittedUrl);
    harness.detectChanges();

    const paste = recipeTextButton(harness.routeNativeElement, 'RECIPE_MANAGE.IMPORT.PASTE_LINK');
    paste.click();
    expect(clipboard).toHaveBeenCalledOnce();
    const recognize = recipeTextButton(harness.routeNativeElement, 'RECIPE_MANAGE.IMPORT.RECOGNIZE');
    expect(recognize.disabled).toBe(false);
    recognize.click();
    await waitForAsyncTasksAsync();
    harness.detectChanges();
    const request = http.expectOne(req => req.method === 'POST' && req.url.endsWith('/ai/food/recipe-import'));
    expect(request.request.body).toEqual({ sourceUrl: submittedUrl, text: null });
    expect(source.closest('fieldset')?.disabled).toBe(true);

    clipboardResult.next(obsoleteUrl);
    clipboardResult.complete();
    await pasting;
    await waitForAsyncTasksAsync();
    harness.detectChanges();
    const sourceWhileRecognizing = source.value;

    request.flush({ name: 'Recognized tomato soup', sourceUrl: submittedUrl, ingredients: [], steps: [INITIAL_INSTRUCTIONS] });
    await vi.waitFor(() => {
        harness.detectChanges();
        expect(recipeName(harness).value).toBe('Recognized tomato soup');
        expect(source.closest('fieldset')?.disabled).toBe(false);
    });
    const sourceWhenRecognized = source.value;
    paste.click();
    await vi.waitFor(() => {
        harness.detectChanges();
        expect(source.value).toBe(nextUrl);
    });
    expect(clipboard).toHaveBeenCalledTimes(2);
    expect([sourceWhileRecognizing, sourceWhenRecognized]).toEqual([submittedUrl, submittedUrl]);
});

it('keeps a pending recipe paste when recognition rejects the empty source', async () => {
    const clipboardUrl = 'https://example.test/recipe-from-clipboard';
    const clipboardResult = new Subject<string>();
    const pasting = firstValueFrom(clipboardResult);
    const clipboard = vi.spyOn(TestBed.inject(BrowserClipboardService), 'readTextAsync').mockReturnValueOnce(pasting);
    const harness = await RouterTestingHarness.create(ADD_RECIPE);
    const source = openRecipeImportSource(harness);
    recipeTextButton(harness.routeNativeElement, 'RECIPE_MANAGE.IMPORT.PASTE_LINK').click();
    expect(clipboard).toHaveBeenCalledOnce();
    recipeTextButton(harness.routeNativeElement, 'RECIPE_MANAGE.IMPORT.RECOGNIZE').click();
    await harness.fixture.whenStable();
    harness.detectChanges();

    expect(harness.routeNativeElement?.querySelector('[role="alert"]')?.textContent).toContain('RECIPE_MANAGE.IMPORT.EMPTY_ERROR');
    expect(source.closest('fieldset')?.disabled).toBe(false);
    TestBed.inject(HttpTestingController).expectNone(req => req.method === 'POST' && req.url.endsWith('/ai/food/recipe-import'));

    clipboardResult.next(clipboardUrl);
    clipboardResult.complete();
    await pasting;
    await vi.waitFor(() => {
        harness.detectChanges();
        expect(source.value).toBe(clipboardUrl);
    });
});

it.each([
    { target: 'the first step', addSecondStep: false },
    { target: 'the second step', addSecondStep: true },
])('waits for a photo upload for $target before replacing the recipe from pasted text', async ({ addSecondStep }) => {
    const harness = await RouterTestingHarness.create(ADD_RECIPE);
    const host = harness.routeNativeElement;
    if (host === null) {
        throw new Error('Expected the routed new recipe editor');
    }
    openRecipeImportSource(harness);
    const importText = recipeTextarea(host, 'fd-ui-textarea.recipe-manage__import-text textarea');

    const originalName = 'Tomato soup draft';
    const importedName = 'Roasted onion soup';
    typeText(recipeName(harness), originalName);
    typeText(importText, `${importedName}\nInstructions:\n${REVISED_INSTRUCTIONS}`);
    harness.detectChanges();
    if (addSecondStep) {
        recipeTextButton(host, 'RECIPE_MANAGE.ADD_STEP').click();
        harness.detectChanges();
    }

    const targetStepIndex = addSecondStep ? 1 : 0;
    const gallery = [...host.querySelectorAll('fd-recipe-step-card fd-image-gallery-editor')].at(targetStepIndex);
    if (gallery === undefined) {
        throw new Error('Expected the selected step photo gallery');
    }
    const input = gallery.querySelector('input[type="file"]');
    if (!(input instanceof HTMLInputElement)) {
        throw new Error('Expected the selected step’s native photo chooser');
    }
    const form = host.querySelector('form.recipe-manage__form');
    const apply = recipeTextButton(host, 'RECIPE_MANAGE.IMPORT.APPLY_BUTTON');
    const originalStepCount = targetStepIndex + 1;
    expect(host.querySelectorAll('fd-recipe-step-card')).toHaveLength(originalStepCount);
    expect(apply.disabled).toBe(false);

    const pendingPhoto = holdRecipePhotoConfirmationAsync(input);
    expect(apply.isConnected).toBe(true);
    expect(apply.disabled).toBe(false);
    apply.click();
    harness.detectChanges();
    expect(recipeName(harness).value).toBe(originalName);
    expect(host.querySelectorAll('fd-recipe-step-card')).toHaveLength(originalStepCount);

    const { confirmation, fileUrl, assetId } = await pendingPhoto;
    await vi.waitFor(() => {
        harness.detectChanges();
        expect(form?.getAttribute('aria-busy')).toBe('true');
    });

    expect(apply.disabled).toBe(true);
    apply.click();
    harness.detectChanges();
    const nameWhileUploading = recipeName(harness).value;
    const stepsWhileUploading = host.querySelectorAll('fd-recipe-step-card').length;
    const cancelled = confirmation.cancelled;
    if (!cancelled) {
        confirmation.flush({ assetId, fileUrl });
    }
    await waitForAsyncTasksAsync();
    harness.detectChanges();

    expect({
        nameWhileUploading,
        stepsWhileUploading,
        cancelled,
        busyAfterUpload: form?.getAttribute('aria-busy'),
        attachmentOnOriginalStep: gallery.querySelector('img')?.getAttribute('src'),
    }).toEqual({
        nameWhileUploading: originalName,
        stepsWhileUploading: originalStepCount,
        cancelled: false,
        busyAfterUpload: 'false',
        attachmentOnOriginalStep: fileUrl,
    });
    expect(apply.disabled).toBe(false);
    apply.click();
    harness.detectChanges();
    expect(recipeName(harness).value).toBe(importedName);
    expect(host.querySelectorAll('fd-recipe-step-card')).toHaveLength(1);
    expect(recipeTextarea(host, 'fd-recipe-step-card textarea').value).toBe(REVISED_INSTRUCTIONS);
    expect(host.querySelector('fd-recipe-step-card img')).toBeNull();
});

it('waits for a step photo before recognizing a replacement recipe from a link', async () => {
    TestBed.overrideProvider(AuthService, {
        useValue: { getUserId: (): string => 'recipe-editor-owner', isAuthenticated: signal(true), isPremium: signal(true) },
    });
    TestBed.overrideProvider(UserFacade, { useValue: { user: signal({ aiConsentAcceptedAt: 'accepted' }) } });
    const harness = await RouterTestingHarness.create(ADD_RECIPE);
    const host = harness.routeNativeElement;
    if (host === null) {
        throw new Error('Expected the routed new recipe editor');
    }
    const sourceUrl = 'https://example.test/roasted-onion-soup';
    const originalName = 'Tomato soup draft';
    const importedName = 'Roasted onion soup';
    typeText(openRecipeImportSource(harness), sourceUrl);
    typeText(recipeName(harness), originalName);
    typeText(recipeTextarea(host, 'fd-recipe-step-card textarea'), INITIAL_INSTRUCTIONS);
    harness.detectChanges();

    const recognize = recipeTextButton(host, 'RECIPE_MANAGE.IMPORT.RECOGNIZE');
    const pendingPhoto = holdRecipePhotoConfirmationAsync(recipeGalleryFileInput(host, 'fd-recipe-step-card'));
    // Selection starts the batch synchronously while the rendered button still has its previous state.
    expect(recognize.isConnected).toBe(true);
    expect(recognize.disabled).toBe(false);
    recognize.click();

    const { confirmation, assetId, fileUrl } = await pendingPhoto;
    harness.detectChanges();
    const form = host.querySelector('form.recipe-manage__form');
    expect(form?.getAttribute('aria-busy')).toBe('true');
    const disabledWhileUploading = recognize.disabled;
    recognize.click();
    await waitForAsyncTasksAsync();
    const http = TestBed.inject(HttpTestingController);
    const isRecipeImport = (request: { method: string; url: string }): boolean =>
        request.method === 'POST' && request.url.endsWith('/ai/food/recipe-import');
    const draft = { name: importedName, sourceUrl, ingredients: [], steps: [REVISED_INSTRUCTIONS] };
    const prematureRequests = http.match(isRecipeImport);
    for (const request of prematureRequests) {
        request.flush(draft);
    }
    await waitForAsyncTasksAsync();
    harness.detectChanges();

    const wasCanceled = confirmation.cancelled;
    if (!wasCanceled) {
        confirmation.flush({ assetId, fileUrl });
    }
    await waitForAsyncTasksAsync();
    harness.detectChanges();
    const attachedPhoto = host.querySelector('fd-recipe-step-card fd-image-gallery-editor img')?.getAttribute('src');
    expect({
        disabledWhileUploading,
        prematureRequestCount: prematureRequests.length,
        wasCanceled,
        activeRecipe: recipeName(harness).value,
        attachedPhoto,
    }).toEqual({
        disabledWhileUploading: true,
        prematureRequestCount: 0,
        wasCanceled: false,
        activeRecipe: originalName,
        attachedPhoto: fileUrl,
    });

    expect(recognize.disabled).toBe(false);
    recognize.click();
    await waitForAsyncTasksAsync();
    const request = http.expectOne(isRecipeImport);
    expect(request.request.body).toEqual({ sourceUrl, text: null });
    harness.detectChanges();
    expect(recognize.closest('fieldset')?.disabled).toBe(true);
    expect(recognize.closest('fieldset')?.hasAttribute('inert')).toBe(true);
    request.flush(draft);
    await vi.waitFor(() => {
        harness.detectChanges();
        expect(recipeName(harness).value).toBe(importedName);
        expect(recipeTextarea(host, 'fd-recipe-step-card textarea').value).toBe(REVISED_INSTRUCTIONS);
        expect(host.querySelector('fd-recipe-step-card fd-image-gallery-editor img')).toBeNull();
    });
});

it('keeps a new recipe draft when departure is cancelled and leaves only after discard', async () => {
    const harness = await RouterTestingHarness.create(ADD_RECIPE);
    const router = TestBed.inject(Router);
    const draftName = 'Unsaved soup';
    typeText(recipeName(harness), draftName);
    harness.detectChanges();

    const stay = router.navigateByUrl(RECIPES);
    await chooseRecipeConfirmationAsync(harness, 'UNSAVED_CHANGES.STAY');
    await expect(stay).resolves.toBe(false);
    expect(router.url).toBe(ADD_RECIPE);
    expect(recipeName(harness).value).toBe(draftName);
    expect(createRecipe).not.toHaveBeenCalled();

    const discard = router.navigateByUrl(RECIPES);
    await chooseRecipeConfirmationAsync(harness, 'UNSAVED_CHANGES.DISCARD');
    await expect(discard).resolves.toBe(true);
    harness.detectChanges();
    expect(router.url).toBe(RECIPES);
    expect(harness.routeNativeElement?.querySelector('fd-recipe-basic-info')).toBeNull();
    expect(createRecipe).not.toHaveBeenCalled();
});

it('keeps a newer typed search usable when the previous catalogue request fails during debounce', async () => {
    const previous = new Subject<RecipeOverview>();
    const search = 'Carrot';
    const found = { ...RECIPE, name: 'Carrot soup' };
    TestBed.overrideProvider(ViewportService, { useValue: { isMobile: signal(false) } });
    loadRecipes.mockReturnValueOnce(previous).mockReturnValueOnce(
        of({
            ...OVERVIEW,
            allRecipes: { ...OVERVIEW.allRecipes, data: [found] },
        }),
    );
    const harness = await RouterTestingHarness.create(RECIPES);
    const host = harness.routeNativeElement;
    const input = host?.querySelector('.recipe-list__desktop-search input');
    if (!(input instanceof HTMLInputElement)) {
        throw new Error('Expected the rendered recipe search field');
    }
    expect(previous.observed).toBe(true);

    typeText(input, search);
    harness.detectChanges();
    expect(previous.observed).toBe(true);
    previous.error(new Error('The previous search failed'));
    harness.detectChanges();
    expect(host?.querySelector('fd-error-state')).toBeNull();

    await vi.waitFor(() => {
        harness.detectChanges();
        expect(TestBed.inject(Router).url).toContain('search=Carrot');
        expect(host?.textContent).toContain(found.name);
    });
    expect(loadRecipes).toHaveBeenCalledTimes(2);
    expect(loadRecipes.mock.lastCall?.[0].filters).toMatchObject({ search });
    expect(host?.querySelector('fd-error-state')).toBeNull();
});

it('keeps both rapid filter removals in the URL and renders the final catalogue result', async () => {
    const result: Recipe = { ...RECIPE, id: entityId<'recipe'>('carrot-soup'), name: 'Carrot soup', category: 'other' };
    loadRecipes.mockImplementation(query => {
        const filtersRemoved = query.filters?.category === undefined && query.filters?.hasImage === undefined;
        return of({ ...OVERVIEW, allRecipes: { ...OVERVIEW.allRecipes, data: [filtersRemoved ? result : RECIPE] } });
    });
    TestBed.inject(TranslateService).setTranslation('en', { COMMON: { REMOVE_FILTER: 'Remove {{filter}}' } }, true);
    const harness = await RouterTestingHarness.create('/recipes?category=soups&hasImage=true&search=soup&campaign=bookmark');
    const router = TestBed.inject(Router);
    const host = harness.routeNativeElement;
    expect(host?.textContent).toContain(RECIPE.name);
    expect(host?.textContent).not.toContain(result.name);
    const removeCategory = getButton(host, 'button[aria-label="Remove RECIPE_CATEGORIES.soups"]');
    const removeMedia = getButton(host, 'button[aria-label="Remove RECIPE_LIST.FILTER_IMAGE_WITH"]');

    removeCategory.click();
    removeMedia.click();

    await vi.waitFor(() => {
        harness.detectChanges();
        expect(router.parseUrl(router.url).queryParams).toEqual({ search: 'soup', campaign: 'bookmark' });
        expect(host?.textContent).toContain(result.name);
        expect(host?.textContent).not.toContain(RECIPE.name);
    });
    const remainingChips = [...(host?.querySelectorAll('fd-active-filters .active-filters__chip button') ?? [])];
    expect(remainingChips.map(chip => chip.getAttribute('aria-label'))).toEqual(['Remove RECIPE_LIST.FILTER_MY_RECIPES']);
    expect(loadRecipes.mock.lastCall?.[0]).toMatchObject({
        page: 1,
        includePublic: false,
        filters: { search: 'soup', category: undefined, hasImage: undefined },
    });
});

it('retries the chosen catalogue filter after the browser rejects its History write', async () => {
    const result: Recipe = { ...RECIPE, id: entityId<'recipe'>('carrot-after-retry'), name: 'Carrot soup', category: 'other' };
    loadRecipes.mockImplementation(query =>
        of({ ...OVERVIEW, allRecipes: { ...OVERVIEW.allRecipes, data: [query.filters?.category === 'soups' ? RECIPE : result] } }),
    );
    TestBed.inject(TranslateService).setTranslation('en', { COMMON: { REMOVE_FILTER: 'Remove {{filter}}' } }, true);
    const harness = await RouterTestingHarness.create('/recipes?category=soups&search=soup&campaign=bookmark');
    const router = TestBed.inject(Router);
    const originalUrl = router.url;
    const host = harness.routeNativeElement;
    expect(host?.textContent).toContain(RECIPE.name);
    const initialReads = loadRecipes.mock.calls.length;
    const finalQuery = { search: 'soup', campaign: 'bookmark' };

    const platform = TestBed.inject(PlatformLocation);
    const pushState = platform.pushState.bind(platform);
    const writeHistory = vi.spyOn(platform, 'pushState').mockImplementationOnce((state: unknown, title, url) => {
        const query = router.parseUrl(url).queryParams;
        if (query['category'] === undefined && query['search'] === finalQuery.search && query['campaign'] === finalQuery.campaign) {
            throw new DOMException('History write temporarily rejected', 'SecurityError');
        }
        pushState(state, title, url);
    });
    onTestFinished(() => {
        writeHistory.mockRestore();
    });

    getButton(host, 'button[aria-label="Remove RECIPE_CATEGORIES.soups"]').click();
    await vi.waitFor(() => {
        harness.detectChanges();
        expect(host?.querySelector('fd-error-state [role="alert"]')).not.toBeNull();
    });
    expect(writeHistory).toHaveBeenCalledOnce();
    expect(router.parseUrl(writeHistory.mock.calls[0][2]).queryParams).toEqual(finalQuery);
    expect(router.url).toBe(originalUrl);
    expect(loadRecipes).toHaveBeenCalledTimes(initialReads);

    writeHistory.mockRestore();
    const retry = getButton(host, 'fd-error-state button');
    expect(retry.disabled).toBe(false);
    retry.click();

    await vi.waitFor(() => {
        harness.detectChanges();
        expect(router.parseUrl(router.url).queryParams).toEqual(finalQuery);
        expect(host?.querySelector('fd-error-state')).toBeNull();
        expect(host?.textContent).toContain(result.name);
        expect(host?.textContent).not.toContain(RECIPE.name);
    });
    expect(loadRecipes).toHaveBeenCalledTimes(initialReads + 1);
    expect(loadRecipes.mock.lastCall?.[0]).toMatchObject({ filters: { search: 'soup', category: undefined } });
});

it('refreshes the filtered catalogue once after favoriting and closing a recipe detail', async () => {
    const harness = await RouterTestingHarness.create('/recipes?search=soup&category=soups&campaign=bookmark');
    const router = TestBed.inject(Router);
    const initialUrl = router.url;
    const dialogs = TestBed.inject(FdUiDialogService);
    const open = vi.spyOn(dialogs, 'open');
    const http = TestBed.inject(HttpTestingController);
    const navigation = vi.fn();
    const routeChanges = router.events.pipe(filter(event => event instanceof NavigationEnd)).subscribe(navigation);
    onTestFinished(() => {
        routeChanges.unsubscribe();
        dialogs.closeAll();
        http.verify();
    });
    await harness.fixture.whenStable();
    harness.detectChanges();
    const addFavorite = 'button[aria-label="RECIPE_DETAIL.ADD_FAVORITE"]';
    const removeFavorite = 'button[aria-label="RECIPE_DETAIL.REMOVE_FAVORITE"]';
    expect(getButton(harness.routeNativeElement, `fd-recipe-list-results ${addFavorite}`).disabled).toBe(false);
    loadRecipes.mockClear();

    getButton(harness.routeNativeElement, `button[aria-label="${RECIPE.name}"]`).click();
    const url = environment.apiUrls.favoriteRecipes;
    const check = await vi.waitFor(() => {
        harness.detectChanges();
        return http.expectOne({ method: 'GET', url: `${url}/check/${RECIPE.id}` });
    });
    check.flush(false);
    harness.detectChanges();
    const detail = document.querySelector('fd-recipe-detail');
    getButton(detail, addFavorite).click();
    const favoriteId = entityId<'favorite-recipe'>('added-favorite');
    const add = http.expectOne({ method: 'POST', url });
    expect(add.request.body).toMatchObject({ recipeId: RECIPE.id });
    add.flush({
        id: favoriteId,
        recipeId: RECIPE.id,
        recipeName: RECIPE.name,
        createdAtUtc: RECIPE.createdAt,
        servings: RECIPE.servings,
        ingredientCount: 0,
    } satisfies FavoriteRecipeHttpResponse);
    harness.detectChanges();
    expect(getButton(detail, removeFavorite).disabled).toBe(false);
    expect(loadRecipes).not.toHaveBeenCalled();

    loadRecipes.mockReturnValueOnce(
        of({ ...OVERVIEW, allRecipes: { ...OVERVIEW.allRecipes, data: [{ ...RECIPE, isFavorite: true, favoriteRecipeId: favoriteId }] } }),
    );
    getButton(detail, 'button[aria-label="COMMON.CLOSE"]').click();
    await vi.waitFor(() => {
        harness.detectChanges();
        expect(document.querySelector('fd-recipe-detail')).toBeNull();
        expect(getButton(harness.routeNativeElement, `fd-recipe-list-results ${removeFavorite}`).disabled).toBe(false);
    });

    expect(router.url).toBe(initialUrl);
    expect(navigation).not.toHaveBeenCalled();
    expect(open).toHaveBeenCalledOnce();
    expect(loadRecipes).toHaveBeenCalledOnce();
    expect(createRecipe).not.toHaveBeenCalled();
    expect(updateRecipe).not.toHaveBeenCalled();
    expect(deleteRecipe).not.toHaveBeenCalled();
});

it('keeps the active catalogue filters after deleting a recipe through its detail dialog', async () => {
    const remaining: Recipe = { ...RECIPE, id: entityId<'recipe'>('remaining-soup'), name: 'Carrot soup' };
    const outsideFilters: Recipe = { ...RECIPE, id: entityId<'recipe'>('other-soup'), name: 'Cold soup', category: 'other' };
    const filters = { search: 'soup', category: 'soups', hasImage: true, caloriesFrom: 100 };
    let deleted = false;
    const response = new Subject<void>();
    deleteRecipe.mockReturnValueOnce(response);
    loadRecipes.mockImplementation(query => {
        const matchesFilters =
            query.filters?.category === filters.category &&
            query.filters.search === filters.search &&
            query.filters.hasImage === filters.hasImage &&
            query.filters.caloriesFrom === filters.caloriesFrom;
        const data = matchesFilters ? (deleted ? [remaining] : [RECIPE, remaining]) : [outsideFilters];
        return of({ ...OVERVIEW, allRecipes: { ...OVERVIEW.allRecipes, data, totalItems: data.length } });
    });
    const harness = await RouterTestingHarness.create(
        '/recipes?search=soup&category=soups&hasImage=true&caloriesFrom=100&campaign=bookmark',
    );
    const router = TestBed.inject(Router);
    const host = harness.routeNativeElement;
    const dialogs = TestBed.inject(FdUiDialogService);
    onTestFinished(() => {
        dialogs.closeAll();
    });

    const item = getButton(host, `button[aria-label="${RECIPE.name}"]`);
    item.click();
    await vi.waitFor(() => {
        harness.detectChanges();
        recipeTextButton(document.querySelector('fd-recipe-detail'), 'RECIPE_DETAIL.DELETE_BUTTON').click();
    });
    await chooseRecipeConfirmationAsync(harness, 'CONFIRM_DELETE.CONFIRM');
    expect(deleteRecipe).toHaveBeenCalledExactlyOnceWith(RECIPE.id);

    deleted = true;
    response.next(undefined);
    response.complete();
    await harness.fixture.whenStable();
    harness.detectChanges();

    expect(router.parseUrl(router.url).queryParams).toEqual({ ...filters, hasImage: 'true', caloriesFrom: '100', campaign: 'bookmark' });
    expect(loadRecipes.mock.lastCall?.[0]).toMatchObject({ page: 1, includePublic: false, filters });
    expect(host?.textContent).toContain(remaining.name);
    expect(host?.textContent).not.toContain(RECIPE.name);
    expect(host?.textContent).not.toContain(outsideFilters.name);
});

it.each(['returns a recipe', 'fails'] as const)('ignores a recipe detail lookup that %s after leaving the list', async outcome => {
    const reply = new Subject<Recipe | null>();
    readRecipe.mockReturnValueOnce(reply);
    const harness = await RouterTestingHarness.create(RECIPES);
    const dialogs = TestBed.inject(FdUiDialogService);
    const open = vi.spyOn(dialogs, 'open');
    const toastError = vi.spyOn(TestBed.inject(FdUiToastService), 'error');
    onTestFinished(() => {
        dialogs.closeAll();
    });
    if (outcome === 'returns a recipe') {
        await import('./components/detail/recipe-detail/recipe-detail');
    }

    getButton(harness.routeNativeElement, `button[aria-label="${RECIPE.name}"]`).click();
    expect(readRecipe).toHaveBeenCalledExactlyOnceWith(RECIPE.id, true);
    expect(reply.observed).toBe(true);
    await harness.navigateByUrl(ADD_RECIPE);
    expect(recipeName(harness).value).toBe('');
    const observedAfterLeaving = reply.observed;

    if (outcome === 'fails') {
        reply.error(new Error('The previous recipe read failed'));
    } else {
        reply.next(RECIPE);
        reply.complete();
    }
    await vi.waitFor(() => {
        const published = open.mock.calls.length > 0 || toastError.mock.calls.length > 0;
        expect(!observedAfterLeaving || published).toBe(true);
    });
    await waitForAsyncTasksAsync();
    harness.detectChanges();

    expect(TestBed.inject(Router).url).toBe(ADD_RECIPE);
    expect(open).not.toHaveBeenCalled();
    expect(toastError).not.toHaveBeenCalled();
    expect(observedAfterLeaving).toBe(false);
});

it('binds the resolved owned recipe to the lazy edit form', async () => {
    const harness = await RouterTestingHarness.create(EDIT_RECIPE);

    expect(TestBed.inject(Router).url).toBe(EDIT_RECIPE);
    expect(readRecipe).toHaveBeenCalledExactlyOnceWith(RECIPE.id, false);
    expect(recipeName(harness).value).toBe(RECIPE.name);
});

it('shows Other for a legacy recipe with no category and saves the selected category', async () => {
    stubScrollIntoView();
    const stored: Recipe = { ...confirmedRecipe(), language: 'en', category: null };
    readRecipe.mockReturnValueOnce(of(stored));
    updateRecipe.mockReturnValueOnce(of({ ...stored, category: 'desserts' }));
    const harness = await RouterTestingHarness.create(EDIT_RECIPE);
    const host = harness.routeNativeElement;
    const category = getButton(host, 'fd-recipe-basic-info .recipe-basic-info__hero-field fd-ui-select button[aria-haspopup="listbox"]');

    expect(category.textContent.trim()).toBe('RECIPE_CATEGORIES.other');
    category.click();
    harness.detectChanges();
    const options = document.querySelector('[role="listbox"][aria-label="RECIPE_MANAGE.CATEGORY"]');
    recipeTextButton(options, 'RECIPE_CATEGORIES.desserts').click();
    harness.detectChanges();

    expect(category.textContent.trim()).toBe('RECIPE_CATEGORIES.desserts');
    getButton(host, 'button[type="submit"]').click();
    await vi.waitFor(() => {
        expect(updateRecipe).toHaveBeenCalledExactlyOnceWith(stored.id, expect.objectContaining({ category: 'desserts' }));
    });
});

it('repairs a legacy recipe without steps and saves only after the required first step is filled', async () => {
    updateRecipe.mockReturnValueOnce(of(confirmedRecipe()));
    const harness = await RouterTestingHarness.create(EDIT_RECIPE);
    const host = harness.routeNativeElement;
    if (host === null) {
        throw new Error('Expected the routed legacy recipe editor');
    }
    const actionsSelector = 'button[aria-label="RECIPE_MANAGE.STEP_ACTIONS"]';
    const removeStepLabel = 'RECIPE_MANAGE.REMOVE_STEP';
    const initialCards = host.querySelectorAll('fd-recipe-step-card');
    expect(initialCards).toHaveLength(1);
    const firstCard = initialCards.item(0);
    const description = firstCard.querySelector('textarea');
    if (description === null) {
        throw new Error('Expected a usable first step for the legacy recipe');
    }
    expect(description.value).toBe('');

    getButton(firstCard, actionsSelector).click();
    harness.detectChanges();
    const firstMenu = document.querySelector('[role="menu"]');
    expect(recipeTextButton(firstMenu, removeStepLabel).disabled).toBe(true);
    getButton(firstMenu, 'button[role="menuitem"]:not([disabled])').dispatchEvent(
        new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }),
    );
    harness.detectChanges();

    recipeTextButton(host, 'RECIPE_MANAGE.ADD_STEP').click();
    harness.detectChanges();
    const cards = host.querySelectorAll('fd-recipe-step-card');
    expect(cards).toHaveLength(2);
    getButton(cards.item(1), actionsSelector).click();
    harness.detectChanges();
    const removeSecond = recipeTextButton(document.querySelector('[role="menu"]'), removeStepLabel);
    expect(removeSecond.disabled).toBe(false);
    removeSecond.click();
    harness.detectChanges();
    expect(host.querySelectorAll('fd-recipe-step-card')).toHaveLength(1);
    expect(host.querySelector('fd-recipe-step-card textarea')).toBe(description);

    getButton(host, 'button[type="submit"]').click();
    harness.detectChanges();
    expect(recipeFieldError(description)).toBe('RECIPE_MANAGE.STEP_DESCRIPTION_REQUIRED');
    expect(updateRecipe).not.toHaveBeenCalled();

    typeText(description, INITIAL_INSTRUCTIONS);
    harness.detectChanges();
    expect(recipeFieldError(description)).toBeNull();
    getButton(host, 'button[type="submit"]').click();
    await vi.waitFor(() => {
        expect(updateRecipe).toHaveBeenCalledExactlyOnceWith(
            RECIPE.id,
            expect.objectContaining({ steps: [expect.objectContaining({ description: INITIAL_INSTRUCTIONS })] }),
        );
    });
});

it('keeps the selected language when saving a photo-led recipe with symbolic instructions', async () => {
    const source = confirmedRecipe();
    const instruction = '🔥 180° · ⏲️ 20′';
    const imageUrl = '/recipes/symbolic-instructions.jpg';
    const stored = {
        ...source,
        name: '🍲',
        languageConfirmed: false,
        steps: [{ ...source.steps[0], instruction, imageUrl }],
    };
    const replacementName = '🍜';
    readRecipe.mockReturnValueOnce(of(stored));
    updateRecipe.mockReturnValueOnce(of({ ...stored, name: replacementName }));
    const harness = await RouterTestingHarness.create(EDIT_RECIPE);
    typeText(recipeName(harness), replacementName);
    harness.detectChanges();
    getButton(harness.routeNativeElement, 'button[type="submit"]').click();

    await vi.waitFor(() => {
        expect(updateRecipe).toHaveBeenCalledExactlyOnceWith(
            stored.id,
            expect.objectContaining({ name: replacementName, language: 'ru', languageConfirmed: false }),
        );
        expect(TestBed.inject(Router).url).toBe(RECIPES);
    });
    expect(updateRecipe.mock.lastCall?.[1].steps[0]).toMatchObject({ description: instruction, imageUrl });
    expect(document.querySelector(LANGUAGE_DIALOG)).toBeNull();
});

it('refuses a registered clear command for the saved recipe and preserves its edited draft', async () => {
    const screens = registerRecipeEditorScreen();
    const stored = confirmedRecipe();
    readRecipe.mockReturnValueOnce(of(stored));
    const harness = await RouterTestingHarness.create(EDIT_RECIPE);
    const draftName = 'Edited tomato soup';
    typeText(recipeName(harness), draftName);
    harness.detectChanges();

    const screen = screens.read();
    if (screen === null) {
        throw new Error('Expected a registered recipe editor screen');
    }
    expect(screen).toMatchObject({
        kind: 'recipe-editor',
        dirty: true,
        actions: { clearDraft: true },
        data: { id: stored.id, isNew: false, name: draftName },
    });

    expect(() => {
        screens.clear(screen.revision);
    }).toThrow('A ready NEW recipe editor is required.');
    harness.detectChanges();
    expect(screens.read()).toEqual(screen);
    expect(recipeName(harness).value).toBe(draftName);
    expect(TestBed.inject(Router).url).toBe(EDIT_RECIPE);
    expect(createRecipe).not.toHaveBeenCalled();
    expect(updateRecipe).not.toHaveBeenCalled();
});

it('reports native recipe validation errors through the registered editor until corrected', async () => {
    const screens = registerRecipeEditorScreen();
    readRecipe.mockReturnValueOnce(of(confirmedRecipe()));
    const harness = await RouterTestingHarness.create(EDIT_RECIPE);
    const name = recipeName(harness);
    const cookTime = recipeBasicInput(harness, 'RECIPE_MANAGE.COOK_TIME');

    typeText(name, '');
    typeText(cookTime, '2.5');
    harness.detectChanges();

    expect(recipeFieldError(name)).toBe('FORM_ERRORS.REQUIRED');
    expect(recipeFieldError(cookTime)).toBe('FORM_ERRORS.INVALID_NUMBER');
    const invalidState = screens.read()?.data;
    expect(invalidState).toMatchObject({
        valid: false,
        validation: { stepsValid: true, cookTime: ['integer'] },
    });
    expect(invalidState).toHaveProperty('validation.name', expect.arrayContaining(['required']));
    getButton(harness.routeNativeElement, 'button[type="submit"]').click();
    expect(updateRecipe).not.toHaveBeenCalled();

    typeText(name, RECIPE.name);
    typeText(cookTime, '3');
    harness.detectChanges();

    expect(recipeFieldError(name)).toBeNull();
    expect(recipeFieldError(cookTime)).toBeNull();
    expect(screens.read()?.data).toMatchObject({ valid: true, validation: { stepsValid: true, name: [], cookTime: [] } });
});

it('preserves the draft on publication cancel and converts only the private ingredient on retry', async () => {
    stubScrollIntoView();
    const { stored, privateFood, publicFood } = publicationRecipeFixture();
    const seasoning = { textName: 'Sea salt', amountText: 'to taste', amount: 0 };
    const recipeWithSeasoning: Recipe = {
        ...stored,
        steps: stored.steps.map(step => ({
            ...step,
            ingredients: [...step.ingredients, recipeIngredientFromStored({ id: 'seasoning', ...seasoning })],
        })),
    };
    const draftName = 'Tomato and yogurt soup';
    readRecipe.mockReturnValueOnce(of(recipeWithSeasoning));
    updateRecipe.mockReturnValueOnce(of({ ...recipeWithSeasoning, name: draftName, visibility: RecipeVisibility.Public }));
    const http = TestBed.inject(HttpTestingController);
    const harness = await RouterTestingHarness.create(EDIT_RECIPE);
    http.expectOne(request => request.url.startsWith('./assets/i18n/en/core.json?')).flush({});
    http.expectOne(request => request.url.startsWith('./assets/i18n/en/app.json?')).flush({ PRODUCT_AMOUNT_UNITS_SHORT: { G: 'g' } });
    const dialogs = TestBed.inject(FdUiDialogService);
    onTestFinished(() => {
        dialogs.closeAll();
    });

    typeText(recipeName(harness), draftName);
    selectPublicRecipeVisibility(harness);
    const submit = getButton(harness.routeNativeElement, 'button[type="submit"]');
    submit.click();
    replyWithRecipePublicationFoods(http, [privateFood, publicFood]);
    const cancel = await recipePublicationActionAsync(harness, 'COMMON.CANCEL');

    const publication = document.querySelector(PUBLICATION_DIALOG);
    expect([...(publication?.querySelectorAll('li') ?? [])].map(row => row.textContent.trim())).toEqual([privateFood.name]);
    expect(publication?.textContent).toContain('RECIPE_PUBLICATION.OWNER_ONLY');
    expect(recipeTextButton(publication, 'RECIPE_PUBLICATION.PUBLISH').disabled).toBe(true);
    cancel.click();
    await vi.waitFor(() => {
        harness.detectChanges();
        expect(document.querySelector(PUBLICATION_DIALOG)).toBeNull();
        expect(submit.disabled).toBe(false);
    });

    expect(TestBed.inject(Router).url).toBe(EDIT_RECIPE);
    expect(recipeName(harness).value).toBe(draftName);
    expect(updateRecipe).not.toHaveBeenCalled();
    http.verify();

    submit.click();
    replyWithRecipePublicationFoods(http, [privateFood, publicFood]);
    const useAsText = await recipePublicationActionAsync(harness, 'RECIPE_PUBLICATION.TEXT');
    useAsText.click();

    await vi.waitFor(() => {
        expect(updateRecipe).toHaveBeenCalledExactlyOnceWith(
            stored.id,
            expect.objectContaining({ name: draftName, visibility: RecipeVisibility.Public }),
        );
        expect(TestBed.inject(Router).url).toBe(RECIPES);
    });
    expect(updateRecipe.mock.lastCall?.[1].steps[0]?.ingredients).toEqual([
        { textName: privateFood.name, amountText: '150.5 g', amount: 0 },
        expect.objectContaining({ productId: publicFood.id, amount: 40, publicName: publicFood.name, publicUnit: 'G' }),
        seasoning,
    ]);
    http.verify();
});

it.each([
    {
        choiceName: 'text conversion',
        choice: 'RECIPE_PUBLICATION.TEXT',
        servings: RECIPE.servings,
    },
    {
        choiceName: 'product publishing',
        choice: 'RECIPE_PUBLICATION.PUBLISH',
        servings: UPDATED_SERVINGS,
    },
])('rejects stale recipe publication after Back ($choiceName)', async ({ choice, servings }) => {
    stubScrollIntoView();
    const { stored, privateFood, publicFood } = publicationRecipeFixture();
    const current: Recipe = { ...stored, visibility: RecipeVisibility.Public };
    const earlier: Recipe = {
        ...current,
        id: entityId<'recipe'>('6f8f8d4f-d8f3-4f22-8a86-251b38dcf2a1'),
        servings,
        steps: current.steps.map(step => ({
            ...step,
            id: `earlier-${step.id}`,
            ingredients: step.ingredients.map(ingredient => ({
                ...ingredient,
                id: entityId<'recipe-ingredient'>(`earlier-${ingredient.id}`),
            })),
        })),
    };
    const earlierUrl = `/recipes/${earlier.id}/edit`;
    const pendingBack = new Subject<Recipe>();
    readRecipe.mockImplementation(id => of(id === earlier.id ? earlier : current));
    const transport = TestBed.runInInjectionContext(() => new RecipeService());
    updateRecipe.mockImplementation((id, data) => transport.update(id, data));
    const http = TestBed.inject(HttpTestingController);
    const router = TestBed.inject(Router);
    router.setUpLocationChangeListener();
    const dialogs = TestBed.inject(FdUiDialogService);
    onTestFinished(() => {
        dialogs.closeAll();
    });
    const harness = await RouterTestingHarness.create(earlierUrl);
    await harness.navigateByUrl(EDIT_RECIPE);
    const editor = harness.routeNativeElement?.querySelector('fd-recipe-manage');
    const returned = firstValueFrom(
        router.events.pipe(filter(event => event instanceof NavigationEnd && event.urlAfterRedirects === earlierUrl)),
    );
    readRecipe.mockClear().mockImplementation(id => (id === earlier.id ? pendingBack : of(current)));
    TestBed.inject(Location).back();
    await vi.waitFor(() => {
        expect(readRecipe).toHaveBeenCalledWith(earlier.id, false);
        expect(pendingBack.observed).toBe(true);
    });

    expect(router.url).toBe(EDIT_RECIPE);
    getButton(harness.routeNativeElement, 'button[type="submit"]').click();
    replyWithRecipePublicationFoods(http, [{ ...privateFood, isOwnedByCurrentUser: true }, publicFood]);
    for (const bundle of http.match(request => request.method === 'GET' && request.url.startsWith('./assets/i18n/en/'))) {
        bundle.flush({ PRODUCT_AMOUNT_UNITS_SHORT: { G: 'g' } });
    }
    const action = await recipePublicationActionAsync(harness, choice);

    pendingBack.next(earlier);
    pendingBack.complete();
    await returned;
    harness.detectChanges();
    expect(router.url).toBe(earlierUrl);
    expect(harness.routeNativeElement?.querySelector('fd-recipe-manage')).toBe(editor);
    const fields = Array.from(harness.routeNativeElement?.querySelectorAll('fd-recipe-basic-info fd-ui-input') ?? []);
    const displayedServings = fields.find(field => field.textContent.includes('RECIPE_MANAGE.SERVINGS'))?.querySelector('input');
    expect(displayedServings?.value).toBe(String(servings));
    expect(action.isConnected).toBe(true);
    expect(action.disabled).toBe(false);
    expect(updateRecipe).not.toHaveBeenCalled();

    action.click();
    await waitForAsyncTasksAsync();
    await harness.fixture.whenStable();
    const productWrites = http.match(request => request.method === 'PATCH' && request.url.includes('/api/v1/products/'));
    for (const request of productWrites) {
        request.flush({ ...privateFood, isOwnedByCurrentUser: true, visibility: 'Public' });
    }
    await waitForAsyncTasksAsync();
    await harness.fixture.whenStable();
    harness.detectChanges();
    expect(document.querySelector(PUBLICATION_DIALOG)).toBeNull();
    const recipeWrites = http.match(request => request.method === 'PATCH' && request.url.includes('/api/v1/recipes/'));
    expect(recipeWrites.map(({ request }) => ({ url: request.url, body: request.body as unknown }))).toEqual([]);
    expect(productWrites.map(({ request }) => ({ url: request.url, body: request.body as unknown }))).toEqual([]);
    http.verify();
});

it('keeps a public recipe draft when ingredient visibility cannot be checked and saves after retry', async () => {
    const { stored, publicFood } = publicationRecipeFixture();
    const published: Recipe = {
        ...stored,
        visibility: RecipeVisibility.Public,
        steps: stored.steps.map(step => ({
            ...step,
            ingredients: step.ingredients.filter(ingredient => ingredient.productId === publicFood.id),
        })),
    };
    const draftName = 'Roasted tomato soup';
    readRecipe.mockReturnValueOnce(of(published));
    updateRecipe.mockReturnValueOnce(of({ ...published, name: draftName }));
    const http = TestBed.inject(HttpTestingController);
    const harness = await RouterTestingHarness.create(EDIT_RECIPE);
    for (const bundle of http.match(request => request.method === 'GET' && request.url.startsWith('./assets/i18n/en/'))) {
        bundle.flush({});
    }

    typeText(recipeName(harness), draftName);
    harness.detectChanges();
    const submit = getButton(harness.routeNativeElement, 'button[type="submit"]');
    submit.click();
    const failedLookup = http.expectOne(request => request.method === 'GET' && request.url.endsWith(`/api/v1/products/${publicFood.id}`));
    failedLookup.flush(null, { status: HttpStatusCode.ServiceUnavailable, statusText: 'Service Unavailable' });

    await vi.waitFor(() => {
        harness.detectChanges();
        expect(harness.routeNativeElement?.querySelector('[role="alert"]')?.textContent).toContain('RECIPE_PUBLICATION.ERROR');
        expect(submit.disabled).toBe(false);
    });
    expect(TestBed.inject(Router).url).toBe(EDIT_RECIPE);
    expect(recipeName(harness).value).toBe(draftName);
    expect(updateRecipe).not.toHaveBeenCalled();

    submit.click();
    replyWithRecipePublicationFoods(http, [publicFood]);
    await vi.waitFor(() => {
        expect(TestBed.inject(Router).url).toBe(RECIPES);
    });
    expect(updateRecipe).toHaveBeenCalledExactlyOnceWith(
        published.id,
        expect.objectContaining({ name: draftName, visibility: RecipeVisibility.Public }),
    );
    expect(updateRecipe.mock.lastCall?.[1].steps[0]?.ingredients).toEqual([
        expect.objectContaining({ productId: publicFood.id, amount: 40 }),
    ]);
    http.verify();
});

it.each(['Public', 'Private'] as const)('ignores a late %s ingredient after going back from a published recipe edit', async visibility => {
    const { stored, publicFood } = publicationRecipeFixture();
    const published: Recipe = {
        ...stored,
        visibility: RecipeVisibility.Public,
        steps: stored.steps.map(step => ({
            ...step,
            ingredients: step.ingredients.filter(ingredient => ingredient.productId === publicFood.id),
        })),
    };
    readRecipe.mockReturnValueOnce(of(published));
    updateRecipe.mockReturnValueOnce(of(published));
    const dialogs = TestBed.inject(FdUiDialogService);
    const open = vi.spyOn(dialogs, 'open');
    const visibilityRead = vi.spyOn(TestBed.inject(ProductPublicationService), 'loadAsync');
    const http = TestBed.inject(HttpTestingController);
    const router = TestBed.inject(Router);
    router.setUpLocationChangeListener();
    const harness = await RouterTestingHarness.create(ADD_RECIPE);
    onTestFinished(() => {
        dialogs.closeAll();
    });
    await harness.navigateByUrl(EDIT_RECIPE);
    expect(recipeName(harness).value).toBe(published.name);
    getButton(harness.routeNativeElement, 'button[type="submit"]').click();
    const lookup = http.expectOne(request => request.method === 'GET' && request.url.endsWith(`/api/v1/products/${publicFood.id}`));
    for (const bundle of http.match(request => request.method === 'GET' && request.url.startsWith('./assets/i18n/en/'))) {
        bundle.flush({});
    }

    await goBackToRecipeAddAsync(harness, router);
    expect(recipeName(harness).value).toBe('');

    lookup.flush({ ...publicFood, visibility });
    await expect(visibilityRead.mock.results[0]?.value).resolves.toEqual([expect.objectContaining({ id: publicFood.id, visibility })]);
    await waitForAsyncTasksAsync();
    await harness.fixture.whenStable();
    harness.detectChanges();

    expect(document.querySelector(PUBLICATION_DIALOG)).toBeNull();
    expect(open).not.toHaveBeenCalled();
    expect(updateRecipe).not.toHaveBeenCalled();
    expect(router.url).toBe(ADD_RECIPE);
    expect(recipeName(harness).value).toBe('');
    http.verify();
});

it('keeps the reused recipe editor free of a publication error from before Back', async () => {
    const { stored, publicFood } = publicationRecipeFixture();
    const published: Recipe = {
        ...stored,
        visibility: RecipeVisibility.Public,
        steps: stored.steps.map(step => ({
            ...step,
            ingredients: step.ingredients.filter(ingredient => ingredient.productId === publicFood.id),
        })),
    };
    const earlier = { ...confirmedRecipe(), id: entityId<'recipe'>('recipe-before-publication'), name: 'Vegetable soup' };
    const earlierUrl = `/recipes/${earlier.id}/edit`;
    readRecipe.mockImplementation(id => of(id === earlier.id ? earlier : published));
    const visibilityRead = vi.spyOn(TestBed.inject(ProductPublicationService), 'loadAsync');
    const http = TestBed.inject(HttpTestingController);
    const router = TestBed.inject(Router);
    router.setUpLocationChangeListener();
    const harness = await RouterTestingHarness.create(earlierUrl);
    await harness.navigateByUrl(EDIT_RECIPE);
    const editor = harness.routeNativeElement?.querySelector('fd-recipe-manage');
    expect(recipeName(harness).value).toBe(published.name);
    getButton(harness.routeNativeElement, 'button[type="submit"]').click();
    const lookup = http.expectOne(request => request.method === 'GET' && request.url.endsWith(`/api/v1/products/${publicFood.id}`));
    for (const bundle of http.match(request => request.method === 'GET' && request.url.startsWith('./assets/i18n/en/'))) {
        bundle.flush({});
    }

    const returned = firstValueFrom(
        router.events.pipe(filter(event => event instanceof NavigationEnd && event.urlAfterRedirects === earlierUrl)),
    );
    TestBed.inject(Location).back();
    await returned;
    harness.detectChanges();
    expect(harness.routeNativeElement?.querySelector('fd-recipe-manage')).toBe(editor);
    expect(recipeName(harness).value).toBe(earlier.name);

    lookup.flush(null, { status: HttpStatusCode.ServiceUnavailable, statusText: 'Service Unavailable' });
    await expect(visibilityRead.mock.results[0]?.value).rejects.toThrow('Cannot verify ingredient visibility');
    await waitForAsyncTasksAsync();
    await harness.fixture.whenStable();
    harness.detectChanges();

    expect(router.url).toBe(earlierUrl);
    expect(recipeName(harness).value).toBe(earlier.name);
    expect(harness.routeNativeElement?.querySelector('[role="alert"]')?.textContent ?? '').not.toContain('RECIPE_PUBLICATION.ERROR');
    expect(getButton(harness.routeNativeElement, 'button[type="submit"]').disabled).toBe(false);
    expect(document.querySelector(PUBLICATION_DIALOG)).toBeNull();
    expect(updateRecipe).not.toHaveBeenCalled();
    http.verify();
});

it('keeps the destination open when an already submitted recipe edit finishes after Back', async () => {
    const stored = confirmedRecipe();
    const response = new Subject<Recipe>();
    readRecipe.mockReturnValueOnce(of(stored));
    updateRecipe.mockReturnValueOnce(response);
    const router = TestBed.inject(Router);
    router.setUpLocationChangeListener();
    const listNavigation = vi.spyOn(TestBed.inject(NavigationService), 'navigateToRecipeListAsync');
    const harness = await RouterTestingHarness.create(ADD_RECIPE);
    await harness.navigateByUrl(EDIT_RECIPE);
    expect(recipeName(harness).value).toBe(stored.name);
    getButton(harness.routeNativeElement, 'button[type="submit"]').click();
    await vi.waitFor(() => {
        expect(updateRecipe).toHaveBeenCalledExactlyOnceWith(stored.id, expect.objectContaining({ name: stored.name }));
    });
    expect(response.observed).toBe(true);

    await goBackToRecipeAddAsync(harness, router);
    expect(recipeName(harness).value).toBe('');
    expect(response.observed).toBe(true);
    response.next(stored);
    response.complete();
    await waitForAsyncTasksAsync();
    await harness.fixture.whenStable();
    harness.detectChanges();

    expect(router.url).toBe(ADD_RECIPE);
    expect(listNavigation).not.toHaveBeenCalled();
    expect(recipeName(harness).value).toBe('');
    expect(response.observed).toBe(false);
});

it('closes the current ingredient picker when a pending browser Back opens another recipe', async () => {
    const source = confirmedRecipe();
    const earlierId = entityId<'recipe'>('earlier-recipe');
    const earlierUrl = `/recipes/${earlierId}/edit`;
    const current = {
        ...source,
        steps: [
            {
                ...source.steps[0],
                ingredients: [recipeIngredientFromStored({ id: 'current-item', productId: 'rice', productName: 'Rice', amount: 100 })],
            },
        ],
    };
    const earlier = {
        ...source,
        id: earlierId,
        name: 'Earlier recipe',
        steps: [
            {
                ...source.steps[0],
                ingredients: [recipeIngredientFromStored({ id: 'earlier-item', productId: 'basil', productName: 'Basil', amount: 100 })],
            },
        ],
    };
    const pendingBack = new Subject<Recipe>();
    readRecipe.mockReturnValueOnce(of(earlier)).mockReturnValueOnce(of(current)).mockReturnValueOnce(pendingBack);
    const router = TestBed.inject(Router);
    router.setUpLocationChangeListener();
    const dialogService = TestBed.inject(FdUiDialogService);
    onTestFinished(() => {
        dialogService.closeAll();
    });
    const harness = await RouterTestingHarness.create(earlierUrl);
    await harness.navigateByUrl(EDIT_RECIPE);
    const currentEditor = harness.routeNativeElement?.querySelector('fd-recipe-manage');
    const returned = firstValueFrom(
        router.events.pipe(filter(event => event instanceof NavigationEnd && event.urlAfterRedirects === earlierUrl)),
    );
    TestBed.inject(Location).back();
    await vi.waitFor(() => {
        expect(readRecipe).toHaveBeenLastCalledWith(earlierId, false);
        expect(pendingBack.observed).toBe(true);
    });
    expect(recipeName(harness).value).toBe(current.name);
    expect(recipeIngredientInput(harness).value).toBe('Rice');
    const picker = openRecipeIngredientPicker(harness);

    pendingBack.next(earlier);
    pendingBack.complete();
    await returned;
    harness.detectChanges();
    expect(harness.routeNativeElement?.querySelector('fd-recipe-manage')).toBe(currentEditor);
    expect(recipeName(harness).value).toBe(earlier.name);
    expect({
        pickerOpen: picker.isConnected,
        ingredient: recipeIngredientInput(harness).value,
    }).toEqual({ pickerOpen: false, ingredient: 'Basil' });

    const replacementPicker = openRecipeIngredientPicker(harness);
    const search = replacementPicker.querySelector('fd-product-list-dialog input');
    if (!(search instanceof HTMLInputElement)) {
        throw new Error('Expected the replacement recipe ingredient search');
    }
    typeText(search, 'Mint');
    harness.detectChanges();
    recipeTextButton(replacementPicker, 'RECIPE_MANAGE.USE_AS_TEXT').click();
    await vi.waitFor(() => {
        harness.detectChanges();
        expect(document.querySelector('fd-item-select-dialog')).toBeNull();
        expect(recipeIngredientInput(harness).value).toBe('Mint');
    });
    expect(updateRecipe).not.toHaveBeenCalled();
});

it('keeps cover-only recipe and step photos after saving and reopening an unrelated edit', async () => {
    const imageUrl = '/recipes/legacy-cover.jpg';
    const stepImageUrl = '/recipes/legacy-step-cover.jpg';
    const galleryImage = 'fd-recipe-basic-info fd-image-gallery-editor img';
    const stepGalleryImage = 'fd-recipe-step-card fd-image-gallery-editor img';
    const source = confirmedRecipe();
    let stored: Recipe = {
        ...source,
        imageUrl,
        imageAssetId: null,
        images: [],
        steps: [{ ...source.steps[0], imageUrl: stepImageUrl, imageAssetId: null, images: [] }],
    };
    readRecipe.mockImplementation(() => of(stored));
    updateRecipe.mockImplementationOnce((_id, data) => {
        stored = {
            ...stored,
            servings: data.servings,
            imageUrl: data.imageUrl,
            steps: [{ ...stored.steps[0], imageUrl: data.steps[0].imageUrl, imageAssetId: data.steps[0].imageAssetId }],
        };
        return of(stored);
    });
    const harness = await RouterTestingHarness.create(EDIT_RECIPE);

    expectRecipeGalleryImage(harness, galleryImage, imageUrl);
    expectRecipeGalleryImage(harness, stepGalleryImage, stepImageUrl);
    const label = [...(harness.routeNativeElement?.querySelectorAll('label') ?? [])].find(element =>
        element.textContent.includes('RECIPE_MANAGE.SERVINGS'),
    );
    const servings = label?.control;
    if (!(servings instanceof HTMLInputElement)) {
        throw new Error('Expected the servings input on the recipe editor');
    }
    typeText(servings, String(UPDATED_SERVINGS));
    harness.detectChanges();
    const router = TestBed.inject(Router);
    const returned = firstValueFrom(
        router.events.pipe(filter(event => event instanceof NavigationEnd && event.urlAfterRedirects === RECIPES)),
    );
    getButton(harness.routeNativeElement, 'button[type="submit"]').click();
    await returned;

    expect(updateRecipe).toHaveBeenCalledExactlyOnceWith(
        stored.id,
        expect.objectContaining({ servings: UPDATED_SERVINGS, imageUrl, imageAssetId: null }),
    );
    const payload = updateRecipe.mock.lastCall?.[1];
    expect(payload?.imageAssetIds).toBeUndefined();
    expect(payload?.steps[0]).toMatchObject({ imageUrl: stepImageUrl, imageAssetId: null });
    expect(payload?.steps[0].imageAssetIds).toBeUndefined();
    await harness.navigateByUrl(EDIT_RECIPE);

    expectRecipeGalleryImage(harness, galleryImage, imageUrl);
    expectRecipeGalleryImage(harness, stepGalleryImage, stepImageUrl);
    expect(stored.servings).toBe(UPDATED_SERVINGS);
});

it.each([
    { slot: 'cover', owner: 'fd-recipe-basic-info' },
    { slot: 'first step', owner: 'fd-recipe-step-card' },
])('keeps an old $slot photo upload out of the recipe reached by browser Back', async ({ owner }) => {
    const { harness, earlier, earlierUrl, cover, step, editor, target, confirmation, assetId, fileUrl } =
        await openRecipeWithHeldGalleryUploadAsync(owner);
    const router = TestBed.inject(Router);
    await router.navigateByUrl(`${EDIT_RECIPE}?from=gallery`, { replaceUrl: true });
    harness.detectChanges();
    expect(recipeGalleryFileInput(harness.routeNativeElement, owner)).toBe(target);
    expect(confirmation.cancelled).toBe(false);
    expect(editor.querySelector('form')?.getAttribute('aria-busy')).toBe('true');

    const returned = firstValueFrom(
        router.events.pipe(filter(event => event instanceof NavigationEnd && event.urlAfterRedirects === earlierUrl)),
    );
    TestBed.inject(Location).back();
    await vi.waitFor(() => {
        expect(readRecipe).toHaveBeenLastCalledWith(earlier.id, false);
    });
    await returned;
    harness.detectChanges();
    expect(harness.routeNativeElement?.querySelector('fd-recipe-manage')).toBe(editor);
    expect(recipeName(harness).value).toBe(earlier.name);
    expectRecipeGalleryImage(harness, 'fd-recipe-basic-info fd-image-gallery-editor img', cover.imageUrl);
    expectRecipeGalleryImage(harness, 'fd-recipe-step-card fd-image-gallery-editor img', step.imageUrl);
    const cancelled = confirmation.cancelled;
    if (!cancelled) {
        confirmation.flush({ assetId, fileUrl });
    }
    await vi.waitFor(() => {
        harness.detectChanges();
        expect(editor.querySelector('form')?.getAttribute('aria-busy')).toBe('false');
    });

    const stalePreview = editor.querySelector(`fd-image-gallery-editor img[src="${fileUrl}"]`)?.getAttribute('src') ?? null;
    const submit = getButton(harness.routeNativeElement, 'button[type="submit"]');
    expect(submit.disabled).toBe(false);
    const currentInput = recipeGalleryFileInput(harness.routeNativeElement, owner);
    expect(currentInput).not.toBe(target);
    expect(currentInput.disabled).toBe(false);
    const uploaded = await holdRecipePhotoConfirmationAsync(currentInput, NEXT_RECIPE_PHOTO);
    uploaded.confirmation.flush({ assetId: uploaded.assetId, fileUrl: uploaded.fileUrl });
    await vi.waitFor(() => {
        harness.detectChanges();
        expectRecipeGalleryImage(harness, `${owner} fd-image-gallery-editor img[src="${uploaded.fileUrl}"]`, uploaded.fileUrl);
    });

    submit.click();
    const http = TestBed.inject(HttpTestingController);
    const write = await vi.waitFor(() => http.expectOne({ method: 'PATCH', url: `${environment.apiUrls.recipes}/${earlier.id}` }));
    expect({ stalePreview, cancelled, body: write.request.body as unknown }).toMatchObject({
        stalePreview: null,
        cancelled: true,
        body: {
            name: earlier.name,
            imageAssetIds: owner === 'fd-recipe-basic-info' ? [cover.imageAssetId, uploaded.assetId] : [cover.imageAssetId],
            steps: [
                expect.objectContaining({
                    imageAssetIds: owner === 'fd-recipe-step-card' ? [step.imageAssetId, uploaded.assetId] : [step.imageAssetId],
                }),
            ],
        },
    });
    write.flush(earlier);
    await vi.waitFor(() => {
        expect(router.url).toBe(RECIPES);
    });
});

type StoredGalleryPhoto = NonNullable<Recipe['images']>[number];

type HeldRecipePhotoFixture = {
    harness: RouterTestingHarness;
    earlier: Recipe;
    earlierUrl: string;
    cover: StoredGalleryPhoto;
    step: StoredGalleryPhoto;
    editor: HTMLElement;
    target: HTMLInputElement;
    confirmation: TestRequest;
    assetId: string;
    fileUrl: string;
};

async function openRecipeWithHeldGalleryUploadAsync(owner: string): Promise<HeldRecipePhotoFixture> {
    const source = confirmedRecipe();
    const currentPhoto = {
        imageAssetId: entityId<'image-asset'>('32510ca0-ae57-48d9-9063-a2f202600003'),
        imageUrl: '/photos/current.jpg',
    };
    const current: Recipe = {
        ...source,
        ...currentPhoto,
        images: [currentPhoto],
        steps: [{ ...source.steps[0], ...currentPhoto, images: [currentPhoto] }],
    };
    const earlierId = entityId<'recipe'>('94c09c27-33c5-4b9a-9c60-3b5d15c05002');
    const cover = { imageAssetId: entityId<'image-asset'>('32510ca0-ae57-48d9-9063-a2f202600001'), imageUrl: '/photos/earlier-cover.jpg' };
    const step = { imageAssetId: entityId<'image-asset'>('32510ca0-ae57-48d9-9063-a2f202600002'), imageUrl: '/photos/earlier-step.jpg' };
    const earlier: Recipe = {
        ...current,
        ...cover,
        id: earlierId,
        name: 'Earlier soup',
        images: [cover],
        steps: [{ ...current.steps[0], ...step, images: [step] }],
    };
    readRecipe.mockReturnValueOnce(of(earlier)).mockReturnValueOnce(of(current)).mockReturnValueOnce(of(earlier));
    const transport = TestBed.runInInjectionContext(() => new RecipeService());
    updateRecipe.mockImplementation((id, data) => transport.update(id, data));
    const router = TestBed.inject(Router);
    router.setUpLocationChangeListener();
    const earlierUrl = `/recipes/${earlier.id}/edit`;
    const harness = await RouterTestingHarness.create(earlierUrl);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(request => request.url.startsWith('./assets/i18n/en/core.json?')).flush({});
    http.expectOne(request => request.url.startsWith('./assets/i18n/en/app.json?')).flush({});
    await harness.navigateByUrl(EDIT_RECIPE);
    const editor = harness.routeNativeElement?.querySelector('fd-recipe-manage');
    if (!(editor instanceof HTMLElement)) {
        throw new Error('Expected the mounted recipe editor');
    }
    const target = recipeGalleryFileInput(harness.routeNativeElement, owner);
    expect(recipeName(harness).value).toBe(current.name);
    expectRecipeGalleryImage(harness, `${owner} fd-image-gallery-editor img`, currentPhoto.imageUrl);
    const { confirmation, assetId, fileUrl } = await holdRecipePhotoConfirmationAsync(target);
    harness.detectChanges();
    expect(editor.querySelector('form')?.getAttribute('aria-busy')).toBe('true');
    expect(updateRecipe).not.toHaveBeenCalled();
    return { harness, earlier, earlierUrl, cover, step, editor, target, confirmation, assetId, fileUrl };
}

it('keeps an existing asset-only cover when the owner uploads an additional recipe photo', async () => {
    const legacyAssetId = entityId<'image-asset'>('a1c759bd-70bc-4f9a-a36d-9905653f8ac6');
    const recipe = { ...confirmedRecipe(), imageUrl: null, imageAssetId: legacyAssetId, images: [] };
    readRecipe.mockReturnValueOnce(of(recipe));
    updateRecipe.mockReturnValueOnce(NEVER);
    const harness = await RouterTestingHarness.create(EDIT_RECIPE);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(request => request.url.startsWith('./assets/i18n/en/core.json?')).flush({});
    http.expectOne(request => request.url.startsWith('./assets/i18n/en/app.json?')).flush({});
    const gallery = harness.routeNativeElement?.querySelector('fd-recipe-basic-info fd-image-gallery-editor');
    const input = gallery?.querySelector('input[type="file"]');
    if (!(input instanceof HTMLInputElement)) {
        throw new Error('Expected the native recipe photo chooser');
    }

    const { confirmation, assetId, fileUrl } = await holdRecipePhotoConfirmationAsync(input);
    confirmation.flush({ assetId, fileUrl });
    await vi.waitFor(() => {
        harness.detectChanges();
        expect(gallery?.querySelector(`img[src="${fileUrl}"]`)).not.toBeNull();
    });

    getButton(harness.routeNativeElement, 'button[type="submit"]').click();
    await vi.waitFor(() => {
        expect(updateRecipe).toHaveBeenCalledOnce();
    });
    expect(updateRecipe).toHaveBeenCalledExactlyOnceWith(
        recipe.id,
        expect.objectContaining({ imageAssetId: legacyAssetId, imageAssetIds: [legacyAssetId, assetId] }),
    );
});

it.each([
    { slot: 'cover', owner: 'fd-recipe-basic-info' },
    { slot: 'first step', owner: 'fd-recipe-step-card' },
])('preserves both photos until the user resolves a mixed $slot gallery', async ({ owner }) => {
    const cover = { imageUrl: '/recipes/legacy-cover.jpg', imageAssetId: null, images: [] };
    const step = { imageUrl: '/recipes/legacy-step-cover.jpg', imageAssetId: null, images: [] };
    const source = confirmedRecipe();
    const legacy: Recipe = { ...source, ...cover, steps: [{ ...source.steps[0], ...step }] };
    readRecipe.mockReturnValueOnce(of(legacy));
    const transport = TestBed.runInInjectionContext(() => new RecipeService());
    updateRecipe.mockImplementation((id, data) => transport.update(id, data));
    const harness = await RouterTestingHarness.create(EDIT_RECIPE);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(request => request.url.startsWith('./assets/i18n/en/core.json?')).flush({});
    http.expectOne(request => request.url.startsWith('./assets/i18n/en/app.json?')).flush({});
    const current = owner === 'fd-recipe-basic-info' ? cover : step;
    const gallery = `${owner} fd-image-gallery-editor`;
    expectRecipeGalleryImage(harness, `${gallery} img`, current.imageUrl);

    const input = recipeGalleryFileInput(harness.routeNativeElement, owner);
    expect(input.disabled).toBe(false);
    const { confirmation, assetId, fileUrl } = await holdRecipePhotoConfirmationAsync(input);
    confirmation.flush({ assetId, fileUrl });
    await vi.waitFor(() => {
        harness.detectChanges();
        expectRecipeGalleryImage(harness, `${gallery} img[src="${fileUrl}"]`, fileUrl);
        expectRecipeGalleryImage(harness, `${gallery} img[src="${current.imageUrl}"]`, current.imageUrl);
    });

    const submit = getButton(harness.routeNativeElement, 'button[type="submit"]');
    const patch = { method: 'PATCH', url: `${environment.apiUrls.recipes}/${legacy.id}` };
    submit.click();
    const attemptedSave = await vi.waitFor(() => {
        harness.detectChanges();
        const writeCount = http.match(patch).length;
        const warning = harness.routeNativeElement?.querySelector('[role="alert"]')?.textContent.trim() ?? '';
        if (writeCount === 0 && warning.length === 0) {
            throw new Error('Expected either a gallery warning or an actual save request.');
        }
        return { writeCount, warning };
    });
    expect(attemptedSave).toEqual({ writeCount: 0, warning: 'RECIPE_MANAGE.MIXED_LEGACY_GALLERY' });
    expect(submit.disabled).toBe(true);
    expectRecipeGalleryImage(harness, `${gallery} img[src="${fileUrl}"]`, fileUrl);
    expectRecipeGalleryImage(harness, `${gallery} img[src="${current.imageUrl}"]`, current.imageUrl);

    const keepUpload = owner === 'fd-recipe-basic-info';
    const removeUrl = keepUpload ? current.imageUrl : fileUrl;
    const keepUrl = keepUpload ? fileUrl : current.imageUrl;
    const thumbnail = getButton(harness.routeNativeElement, `${gallery} button:has(img[src="${removeUrl}"])`);
    thumbnail.click();
    harness.detectChanges();
    recipeTextButton(document.querySelector('[role="menu"]'), 'IMAGE_UPLOAD_FIELD.REMOVE').click();
    harness.detectChanges();

    expect(harness.routeNativeElement?.querySelector('[role="alert"]')).toBeNull();
    expectRecipeGalleryImage(harness, `${gallery} img`, keepUrl);
    expect(harness.routeNativeElement?.querySelector(`${gallery} img[src="${removeUrl}"]`)).toBeNull();
    expect(submit.disabled).toBe(false);
    submit.click();
    const write = await vi.waitFor(() => http.expectOne(patch));
    const serialized: unknown = write.request.body;
    expect(serialized).toMatchObject(
        keepUpload
            ? { imageUrl: fileUrl, imageAssetId: assetId, imageAssetIds: [assetId] }
            : { steps: [expect.objectContaining({ imageUrl: step.imageUrl, imageAssetId: null })] },
    );
    if (!keepUpload) {
        expect(serialized).not.toMatchObject({ steps: [expect.objectContaining({ imageAssetIds: [assetId] })] });
    }
});

it('saves reordered recipe and step galleries while preserving hydrated manual nutrition', async () => {
    const source = confirmedRecipe();
    const cover = { imageAssetId: entityId<'image-asset'>('soup-front'), imageUrl: '/recipes/soup-front.jpg' };
    const alternateCover = { imageAssetId: entityId<'image-asset'>('soup-back'), imageUrl: '/recipes/soup-back.jpg' };
    const stepCover = { imageAssetId: entityId<'image-asset'>('stirring'), imageUrl: '/recipes/stirring.jpg' };
    const alternateStep = { imageAssetId: entityId<'image-asset'>('serving'), imageUrl: '/recipes/serving.jpg' };
    const stored: Recipe = {
        ...source,
        ...cover,
        images: [cover, alternateCover],
        isNutritionAutoCalculated: false,
        manualCalories: 210,
        manualProteins: 9.5,
        steps: [{ ...source.steps[0], ...stepCover, images: [stepCover, alternateStep] }],
    };
    readRecipe.mockReturnValueOnce(of(stored));
    updateRecipe.mockReturnValueOnce(of(stored));
    const harness = await RouterTestingHarness.create(EDIT_RECIPE);
    const calories = recipeNutritionInput(harness, 'calories');
    expect(calories.readOnly).toBe(false);
    expect(calories.value).toBe('210');

    chooseSecondGalleryPhoto(harness, 'fd-recipe-basic-info');
    chooseSecondGalleryPhoto(harness, 'fd-recipe-step-card');
    getButton(harness.routeNativeElement, 'button[type="submit"]').click();

    await vi.waitFor(() => {
        expect(updateRecipe).toHaveBeenCalledExactlyOnceWith(
            stored.id,
            expect.objectContaining({
                ...alternateCover,
                imageAssetIds: [alternateCover.imageAssetId, cover.imageAssetId],
                manualCalories: 210,
                manualProteins: 9.5,
            }),
        );
    });
    expect(updateRecipe.mock.lastCall?.[1].steps[0]).toMatchObject({
        ...alternateStep,
        imageAssetIds: [alternateStep.imageAssetId, stepCover.imageAssetId],
    });
});

it('keeps known nutrition when a stored nested ingredient lacks calories', async () => {
    const source = confirmedRecipe();
    const product = recipeIngredientFromStored({
        id: 'stored-flour',
        amount: 50,
        productId: 'flour',
        productName: 'Flour',
        productBaseUnit: 'G',
        productBaseAmount: 100,
        productCaloriesPerBase: 120,
        productProteinsPerBase: 10,
    });
    const nested = recipeIngredientFromStored({
        id: 'stored-sauce',
        amount: 1,
        nestedRecipeId: 'sauce',
        nestedRecipeName: 'Sauce',
        nestedRecipeServings: 3,
        nestedRecipeTotalCalories: null,
        nestedRecipeTotalProteins: 30,
        nestedRecipeMissingIngredientCount: 1,
    });
    readRecipe.mockReturnValueOnce(
        of({ ...source, missingIngredientCount: 1, steps: [{ ...source.steps[0], ingredients: [product, nested] }] }),
    );
    const harness = await RouterTestingHarness.create(EDIT_RECIPE);

    const calories = recipeNutritionInput(harness, 'calories');
    expect(calories.readOnly).toBe(true);
    expect(calories.value).toBe('60');
    expect(recipeNutritionInput(harness, 'proteins').value).toBe('15');
    expect(harness.routeNativeElement?.querySelector('fd-recipe-nutrition-editor [role="status"]')?.textContent).toContain(
        'NUTRITION_COMPLETENESS.PARTIAL',
    );
});

it('keeps known recipe nutrition when a stored product has zero base amount', async () => {
    const source = confirmedRecipe();
    const flour = recipeIngredientFromStored({
        id: 'stored-flour',
        amount: 50,
        productId: 'flour',
        productName: 'Flour',
        productBaseUnit: 'G',
        productBaseAmount: 100,
        productCaloriesPerBase: 120,
        productProteinsPerBase: 10,
    });
    const legacyOil = recipeIngredientFromStored({
        id: 'stored-oil',
        amount: 25,
        productId: 'legacy-oil',
        productName: 'Legacy oil',
        productBaseUnit: 'G',
        productBaseAmount: 0,
        productCaloriesPerBase: 40,
    });
    readRecipe.mockReturnValueOnce(of({ ...source, steps: [{ ...source.steps[0], ingredients: [flour, legacyOil] }] }));
    const harness = await RouterTestingHarness.create(EDIT_RECIPE);

    const names = harness.routeNativeElement?.querySelectorAll<HTMLInputElement>('fd-recipe-step-card .recipe-ingredient__name input');
    expect(Array.from(names ?? [], input => input.value)).toEqual(['Flour', 'Legacy oil']);
    const calories = recipeNutritionInput(harness, 'calories');
    expect(calories.readOnly).toBe(true);
    expect(calories.value).toBe('60');
    expect(recipeNutritionInput(harness, 'proteins').value).toBe('5');
});

it('saves a mixed stored ingredient as a nested recipe while keeping text as the first choice', async () => {
    const nestedServings = 1.25;
    const linkedIngredient = {
        id: 'stored-mixed-ingredient',
        productId: 'old-product',
        productName: 'Old product',
        nestedRecipeId: 'sauce-recipe',
        nestedRecipeName: 'Tomato sauce',
        nestedRecipeServings: 3,
        amount: nestedServings,
    };
    const stored = recipeFromSdk({
        ...RECIPE,
        language: 'en',
        steps: [
            {
                id: 'mixed-step',
                stepNumber: 1,
                instruction: INITIAL_INSTRUCTIONS,
                ingredients: [
                    linkedIngredient,
                    { ...linkedIngredient, id: 'stored-text-ingredient', textName: 'Sea salt', amountText: 'a pinch' },
                ],
            },
        ],
    });
    readRecipe.mockReturnValueOnce(of(stored));
    updateRecipe.mockReturnValueOnce(of(stored));
    const harness = await RouterTestingHarness.create(EDIT_RECIPE);

    const renderedNames = harness.routeNativeElement?.querySelectorAll<HTMLInputElement>(
        'fd-recipe-step-card .recipe-ingredient__name input',
    );
    expect(Array.from(renderedNames ?? [], input => input.value)).toEqual(['Tomato sauce', 'Sea salt']);
    typeText(recipeName(harness), 'Tomato soup with sauce');
    harness.detectChanges();
    getButton(harness.routeNativeElement, 'button[type="submit"]').click();

    await vi.waitFor(() => {
        expect(updateRecipe).toHaveBeenCalledExactlyOnceWith(stored.id, expect.objectContaining({ name: 'Tomato soup with sauce' }));
    });
    const ingredients = updateRecipe.mock.lastCall?.[1].steps[0]?.ingredients;
    expect(ingredients).toEqual([
        { productId: undefined, nestedRecipeId: linkedIngredient.nestedRecipeId, amount: nestedServings },
        { productId: undefined, nestedRecipeId: undefined, textName: 'Sea salt', amountText: 'a pinch', amount: 0 },
    ]);
});

it('preserves a stored text ingredient with no amount and accepts explicit zero after missing macros', async () => {
    const source = confirmedRecipe();
    const ingredient = recipeIngredientFromStored({ id: 'salt-ingredient', amount: 0, textName: 'Salt', amountText: null });
    const stored = { ...source, steps: [{ ...source.steps[0], ingredients: [ingredient] }], missingIngredientCount: 1 };
    readRecipe.mockReturnValueOnce(of(stored));
    updateRecipe.mockReturnValueOnce(of(stored));
    const harness = await RouterTestingHarness.create(EDIT_RECIPE);
    const host = harness.routeNativeElement;
    recipeTextButton(host, 'RECIPE_MANAGE.NUTRITION_MODE.MANUAL').click();
    harness.detectChanges();

    for (const nutrient of ['calories', 'proteins', 'fats', 'carbs', 'alcohol'] as const) {
        typeText(recipeNutritionInput(harness, nutrient), '');
    }
    harness.detectChanges();
    const protein = recipeNutritionInput(harness, 'proteins');
    expect(protein.value).toBe('');
    const submit = getButton(host, 'button[type="submit"]');
    submit.click();
    harness.detectChanges();
    expect(host?.textContent).toContain('PRODUCT_MANAGE.NUTRITION_ERRORS.MACROS_REQUIRED');
    expect(updateRecipe).not.toHaveBeenCalled();

    typeText(recipeNutritionInput(harness, 'calories'), '0');
    typeText(protein, '0');
    harness.detectChanges();
    expect(host?.textContent).not.toContain('PRODUCT_MANAGE.NUTRITION_ERRORS.MACROS_REQUIRED');
    submit.click();

    await vi.waitFor(() => {
        expect(updateRecipe).toHaveBeenCalledExactlyOnceWith(
            stored.id,
            expect.objectContaining({ calculateNutritionAutomatically: false, manualCalories: 0, manualProteins: 0 }),
        );
    });
    expect(updateRecipe.mock.lastCall?.[1].steps[0]?.ingredients).toEqual([{ textName: 'Salt', amountText: null, amount: 0 }]);
});

it.each(['calories', 'proteins'] as const)('keeps incomplete manual %s out of the save payload until corrected', async nutrient => {
    const stored = confirmedRecipe();
    readRecipe.mockReturnValueOnce(of(stored));
    updateRecipe.mockReturnValueOnce(of(stored));
    const harness = await RouterTestingHarness.create(EDIT_RECIPE);
    const host = harness.routeNativeElement;
    recipeTextButton(host, 'RECIPE_MANAGE.NUTRITION_MODE.MANUAL').click();
    harness.detectChanges();
    const calories = recipeNutritionInput(harness, 'calories');
    const protein = recipeNutritionInput(harness, 'proteins');
    const fat = recipeNutritionInput(harness, 'fats');
    const incomplete = recipeNutritionInput(harness, nutrient);
    typeText(calories, '200');
    typeText(protein, '10');
    typeText(fat, '10');
    typeText(incomplete, '-');
    harness.detectChanges();

    recipeTextButton(host, 'RECIPE_MANAGE.NUTRITION_SCALE_MODE.PORTION').click();
    harness.detectChanges();
    expect(incomplete.value).toBe('-');
    expect(fat.value).toBe('5');
    const submit = getButton(host, 'button[type="submit"]');
    submit.click();
    await waitForAsyncTasksAsync();
    await harness.fixture.whenStable();

    expect(updateRecipe).not.toHaveBeenCalled();
    expect(incomplete.getAttribute('aria-invalid')).toBe('true');
    expect(document.getElementById(`${incomplete.id}-error`)?.textContent.trim()).toBe('FORM_ERRORS.INVALID_NUMBER');
    typeText(calories, '100');
    typeText(protein, '2,5');
    harness.detectChanges();
    expect(incomplete.getAttribute('aria-invalid')).toBeNull();
    submit.click();

    await vi.waitFor(() => {
        expect(updateRecipe).toHaveBeenCalledExactlyOnceWith(
            stored.id,
            expect.objectContaining({ calculateNutritionAutomatically: false, manualCalories: 200, manualProteins: 5, manualFats: 10 }),
        );
    });
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

it('keeps the displayed English default for a legacy recipe with no language when the user confirms it', async () => {
    const instruction =
        'Разогрейте масло на сковороде и добавьте нарезанные помидоры с солью. Затем влейте воду, смешайте овощи и готовьте несколько минут на слабом огне.';
    const stored = recipeFromSdk({
        ...RECIPE,
        name: 'Овощной суп',
        steps: [{ id: 'legacy-recipe-step', stepNumber: 1, instruction, ingredients: [] }],
    });
    readRecipe.mockReturnValueOnce(of(stored));
    updateRecipe.mockReturnValueOnce(of({ ...stored, language: 'en', languageConfirmed: true }));
    const harness = await RouterTestingHarness.create(EDIT_RECIPE);
    const dialogs = TestBed.inject(FdUiDialogService);
    onTestFinished(() => {
        dialogs.closeAll();
    });
    await firstValueFrom(TestBed.inject(TranslateService).use('ru'));
    harness.detectChanges();

    const summary = harness.routeNativeElement?.querySelector('fd-recipe-basic-info details summary');
    if (!(summary instanceof HTMLElement)) {
        throw new Error('Expected the advanced recipe settings');
    }
    summary.click();
    harness.detectChanges();
    const language = getButton(harness.routeNativeElement, 'fd-recipe-basic-info details fd-ui-select button[aria-haspopup="listbox"]');
    expect(language.textContent).toContain('RECIPE_LANGUAGE.EN');

    getButton(harness.routeNativeElement, 'button[type="submit"]').click();
    const keep = await recipeLanguageActionAsync(harness, 'RECIPE_LANGUAGE.KEEP');
    expect(updateRecipe).not.toHaveBeenCalled();
    keep.click();
    await vi.waitFor(() => {
        expect(updateRecipe).toHaveBeenCalledExactlyOnceWith(
            stored.id,
            expect.objectContaining({ name: stored.name, language: 'en', languageConfirmed: true }),
        );
    });
    expect(updateRecipe.mock.lastCall?.[1].steps[0]?.description).toBe(instruction);
});

it.each([
    { returningGallery: 'empty', coverUrl: null },
    { returningGallery: 'legacy URL-only', coverUrl: '/recipes/earlier-legacy-cover.jpg' },
])('restores the $returningGallery gallery and stored recipe totals on browser Back', async ({ coverUrl }) => {
    const earlierId = '94c09c27-33c5-4b9a-9c60-3b5d15c05002';
    const currentId = '94c09c27-33c5-4b9a-9c60-3b5d15c05001';
    const { current, earlier } = storedRecipeGalleryFixtures(currentId, earlierId, coverUrl);
    const transport = TestBed.runInInjectionContext(() => new RecipeService());
    readRecipe.mockImplementation((id, includePublic) => transport.getById(id, includePublic));
    updateRecipe.mockImplementation((id, data) => transport.update(id, data));
    const http = TestBed.inject(HttpTestingController);
    const readStoredRecipeAsync = async (endpoint: string): Promise<TestRequest> =>
        vi.waitFor(() =>
            http.expectOne(
                request => request.method === 'GET' && request.url === endpoint && request.params.get('includePublic') === 'false',
            ),
        );
    const router = TestBed.inject(Router);
    const earlierUrl = `/recipes/${earlierId}/edit`;
    const earlierEndpoint = `${environment.apiUrls.recipes}/${earlierId}`;
    const opening = RouterTestingHarness.create(earlierUrl);
    const firstRead = await readStoredRecipeAsync(earlierEndpoint);
    firstRead.flush(earlier);
    const harness = await opening;
    http.expectOne(request => request.url.startsWith('./assets/i18n/en/core.json?')).flush({});
    http.expectOne(request => request.url.startsWith('./assets/i18n/en/app.json?')).flush({});
    expect(recipeEditorCoverUrl(harness)).toBe(coverUrl);
    const openingCurrent = harness.navigateByUrl(`/recipes/${currentId}/edit`);
    const currentRead = await readStoredRecipeAsync(`${environment.apiUrls.recipes}/${currentId}`);
    currentRead.flush(current);
    await openingCurrent;
    const editor = harness.routeNativeElement?.querySelector('fd-recipe-manage');
    expect(recipeEditorCoverUrl(harness)).toBe(current.imageUrl);
    const portionLabel = 'RECIPE_MANAGE.NUTRITION_SCALE_MODE.PORTION';
    const recipeLabel = 'RECIPE_MANAGE.NUTRITION_SCALE_MODE.RECIPE';
    recipeTextButton(harness.routeNativeElement, portionLabel).click();
    harness.detectChanges();
    expect(recipeTextButton(harness.routeNativeElement, portionLabel).getAttribute('aria-checked')).toBe('true');
    expect(recipeNutritionInput(harness, 'calories').value).toBe('128');

    router.setUpLocationChangeListener();
    const returned = firstValueFrom(
        router.events.pipe(filter(event => event instanceof NavigationEnd && event.urlAfterRedirects === earlierUrl)),
    );
    TestBed.inject(Location).back();
    const backRead = await readStoredRecipeAsync(earlierEndpoint);
    backRead.flush(earlier);
    await returned;
    harness.detectChanges();
    expect(harness.routeNativeElement?.querySelector('fd-recipe-manage')).toBe(editor);
    expect(recipeName(harness).value).toBe(earlier.name);
    const displayed = {
        whole: recipeTextButton(harness.routeNativeElement, recipeLabel).getAttribute('aria-checked'),
        portion: recipeTextButton(harness.routeNativeElement, portionLabel).getAttribute('aria-checked'),
        calories: recipeNutritionInput(harness, 'calories').value,
        proteins: recipeNutritionInput(harness, 'proteins').value,
    };
    const restoredCover = recipeEditorCoverUrl(harness);
    const newName = 'Vegetable soup with herbs';
    typeText(recipeName(harness), newName);
    harness.detectChanges();
    getButton(harness.routeNativeElement, 'button[type="submit"]').click();
    const write = await vi.waitFor(() => http.expectOne({ method: 'PATCH', url: earlierEndpoint }));
    expect(write.request.body as unknown).toMatchObject({
        name: newName,
        servings: earlier.servings,
        manualCalories: 320,
        manualProteins: 20,
        imageUrl: coverUrl,
        imageAssetId: null,
        steps: [expect.objectContaining({ description: INITIAL_INSTRUCTIONS })],
    });
    expect(displayed).toEqual({ whole: 'true', portion: 'false', calories: '320', proteins: '20' });
    expect({ visibleCover: restoredCover, gallerySent: Object.hasOwn(write.request.body as object, 'imageAssetIds') }).toEqual({
        visibleCover: coverUrl,
        gallerySent: false,
    });
    write.flush({ ...earlier, name: newName });
    await vi.waitFor(() => {
        expect(router.url).toBe(RECIPES);
    });
    http.verify();
});

it('keeps the previous recipe language confirmation from saving after a pending Back replaces the editor', async () => {
    const source = confirmedRecipe();
    const current = { ...source, languageConfirmed: false };
    const earlier = { ...source, id: entityId<'recipe'>('earlier-language-recipe'), name: 'Earlier vegetable soup' };
    const earlierUrl = `/recipes/${earlier.id}/edit`;
    const pendingBack = new Subject<Recipe>();
    readRecipe.mockImplementation(id => of(id === earlier.id ? earlier : current));
    updateRecipe.mockReturnValue(of(earlier));
    const router = TestBed.inject(Router);
    router.setUpLocationChangeListener();
    const dialogs = TestBed.inject(FdUiDialogService);
    onTestFinished(() => {
        dialogs.closeAll();
    });
    const harness = await RouterTestingHarness.create(earlierUrl);
    await harness.navigateByUrl(EDIT_RECIPE);
    const editor = harness.routeNativeElement?.querySelector('fd-recipe-manage');
    expect(recipeName(harness).value).toBe(current.name);
    const returned = firstValueFrom(
        router.events.pipe(filter(event => event instanceof NavigationEnd && event.urlAfterRedirects === earlierUrl)),
    );
    readRecipe.mockClear().mockImplementation(id => (id === earlier.id ? pendingBack : of(current)));
    TestBed.inject(Location).back();
    await vi.waitFor(() => {
        expect(readRecipe).toHaveBeenCalledWith(earlier.id, false);
        expect(pendingBack.observed).toBe(true);
    });

    getButton(harness.routeNativeElement, 'button[type="submit"]').click();
    const keep = await recipeLanguageActionAsync(harness, 'RECIPE_LANGUAGE.KEEP');
    expect(keep.disabled).toBe(false);
    expect(updateRecipe).not.toHaveBeenCalled();

    pendingBack.next(earlier);
    pendingBack.complete();
    await returned;
    harness.detectChanges();
    expect(harness.routeNativeElement?.querySelector('fd-recipe-manage')).toBe(editor);
    expect(recipeName(harness).value).toBe(earlier.name);
    expect(keep.isConnected).toBe(true);
    expect(keep.disabled).toBe(false);
    keep.click();
    await vi.waitFor(() => {
        harness.detectChanges();
        expect(document.querySelector(LANGUAGE_DIALOG)).toBeNull();
        expect(recipeName(harness).matches(':enabled')).toBe(true);
    });

    expect(updateRecipe).not.toHaveBeenCalled();
    const name = recipeName(harness);
    expect(name.value).toBe(earlier.name);
    typeText(name, 'Earlier vegetable soup with basil');
    harness.detectChanges();
    expect(recipeName(harness).value).toBe('Earlier vegetable soup with basil');
});

it('keeps a stale language confirmation from overwriting a different recipe with the same text', async () => {
    const source = confirmedRecipe();
    const current = { ...source, languageConfirmed: false };
    const earlier = { ...source, id: entityId<'recipe'>('recipe-copy-before-back'), servings: UPDATED_SERVINGS };
    const earlierUrl = `/recipes/${earlier.id}/edit`;
    const pendingBack = new Subject<Recipe>();
    readRecipe.mockReturnValueOnce(of(earlier)).mockReturnValueOnce(of(current)).mockReturnValueOnce(pendingBack);
    updateRecipe.mockReturnValue(of(earlier));
    const router = TestBed.inject(Router);
    router.setUpLocationChangeListener();
    const dialogs = TestBed.inject(FdUiDialogService);
    onTestFinished(() => {
        dialogs.closeAll();
    });
    const harness = await RouterTestingHarness.create(earlierUrl);
    await harness.navigateByUrl(EDIT_RECIPE);
    const editor = harness.routeNativeElement?.querySelector('fd-recipe-manage');
    const returned = firstValueFrom(
        router.events.pipe(filter(event => event instanceof NavigationEnd && event.urlAfterRedirects === earlierUrl)),
    );
    TestBed.inject(Location).back();
    await vi.waitFor(() => {
        expect(readRecipe).toHaveBeenLastCalledWith(earlier.id, false);
        expect(pendingBack.observed).toBe(true);
    });
    getButton(harness.routeNativeElement, 'button[type="submit"]').click();
    const keep = await recipeLanguageActionAsync(harness, 'RECIPE_LANGUAGE.KEEP');
    expect(keep.disabled).toBe(false);

    pendingBack.next(earlier);
    pendingBack.complete();
    await returned;
    harness.detectChanges();
    expect(harness.routeNativeElement?.querySelector('fd-recipe-manage')).toBe(editor);
    expect(router.url).toBe(earlierUrl);
    const fields = Array.from(harness.routeNativeElement?.querySelectorAll('fd-recipe-basic-info fd-ui-input') ?? []);
    const servings = fields.find(field => field.textContent.includes('RECIPE_MANAGE.SERVINGS'))?.querySelector('input');
    expect(servings?.value).toBe(String(UPDATED_SERVINGS));
    expect(keep.isConnected).toBe(true);
    expect(updateRecipe).not.toHaveBeenCalled();

    keep.click();
    await waitForAsyncTasksAsync();
    await harness.fixture.whenStable();
    harness.detectChanges();

    expect(updateRecipe.mock.calls.map(([id, dto]) => ({ id, servings: dto.servings }))).toEqual([]);
    expect(router.url).toBe(earlierUrl);
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

function recipeIngredientInput(harness: RouterTestingHarness): HTMLInputElement {
    const input = harness.routeNativeElement?.querySelector('.recipe-ingredient__name input');
    if (!(input instanceof HTMLInputElement)) {
        throw new Error('Expected the visible recipe ingredient input');
    }
    return input;
}

function openRecipeIngredientPicker(harness: RouterTestingHarness): HTMLElement {
    recipeIngredientInput(harness).click();
    harness.detectChanges();
    const picker = document.querySelector('fd-item-select-dialog');
    if (!(picker instanceof HTMLElement)) {
        throw new Error('Expected the opened recipe ingredient picker');
    }
    return picker;
}

function storedRecipeGalleryFixtures(
    currentId: string,
    earlierId: string,
    coverUrl: string | null,
): { current: RecipeHttpResponse; earlier: RecipeHttpResponse } {
    const storedCover = { imageAssetId: '941c9f07-20df-44df-8cce-3534084b4a35', imageUrl: '/recipes/current-asset-cover.jpg' };
    const currentRecipe = storedManualRecipe(currentId, 'Roasted vegetables', { servings: 2, calories: 256, proteins: 12 });
    return {
        current: {
            ...currentRecipe,
            ...storedCover,
            images: [storedCover],
            steps: currentRecipe.steps?.map(step => ({ ...step, instruction: REVISED_INSTRUCTIONS })),
        },
        earlier: {
            ...storedManualRecipe(earlierId, 'Vegetable soup', { servings: 4, calories: 320, proteins: 20 }),
            imageUrl: coverUrl,
            imageAssetId: null,
            images: [],
        },
    };
}

function storedManualRecipe(
    id: string,
    name: string,
    { servings, calories, proteins }: { servings: number; calories: number; proteins: number },
): RecipeHttpResponse {
    const uuidSuffixStart = -12;
    return {
        id,
        name,
        language: 'en',
        languageConfirmed: true,
        category: 'main_courses',
        visibility: RecipeVisibility.Private,
        createdAt: RECIPE.createdAt,
        usageCount: 0,
        isOwnedByCurrentUser: true,
        isNutritionAutoCalculated: false,
        servings,
        manualCalories: calories,
        manualProteins: proteins,
        totalCalories: calories,
        totalProteins: proteins,
        steps: [
            {
                id: `47f030da-8542-4376-a639-${id.slice(uuidSuffixStart)}`,
                stepNumber: 1,
                instruction: INITIAL_INSTRUCTIONS,
                ingredients: [],
            },
        ],
    };
}

function confirmedRecipe(): Recipe {
    return {
        ...RECIPE,
        language: 'ru',
        languageConfirmed: true,
        steps: [{ id: 'recipe-editor-step', stepNumber: 1, instruction: INITIAL_INSTRUCTIONS, ingredients: [] }],
    };
}

type PublicationFood = ProductHttpResponse & { id: string; name: string };

function publicationRecipeFixture(): { stored: Recipe; privateFood: PublicationFood; publicFood: PublicationFood } {
    const source = confirmedRecipe();
    const privateFood = {
        id: 'private-yogurt',
        name: 'Yogurt',
        baseUnit: 'G',
        baseAmount: 100,
        defaultPortionAmount: 100,
        caloriesPerBase: 61,
        proteinsPerBase: 3.5,
        fatsPerBase: 3.3,
        carbsPerBase: 4.7,
        fiberPerBase: 0,
        alcoholPerBase: 0,
        usageCount: 1,
        visibility: 'Private',
        createdAt: source.createdAt,
        isOwnedByCurrentUser: false,
        qualityScore: 70,
        qualityGrade: 'green',
    } satisfies PublicationFood;
    const publicFood = {
        ...privateFood,
        id: 'public-tomatoes',
        name: 'Tomatoes',
        caloriesPerBase: 18,
        proteinsPerBase: 0.9,
        fatsPerBase: 0.2,
        carbsPerBase: 3.9,
        fiberPerBase: 1.2,
        visibility: 'Public',
        isOwnedByCurrentUser: true,
    } satisfies PublicationFood;
    const ingredients = [
        recipeIngredientFromStored({
            id: 'yogurt',
            productId: privateFood.id,
            productName: privateFood.name,
            productBaseUnit: privateFood.baseUnit,
            amount: 150.5,
        }),
        recipeIngredientFromStored({
            id: 'tomatoes',
            productId: publicFood.id,
            productName: publicFood.name,
            productBaseUnit: publicFood.baseUnit,
            amount: 40,
        }),
    ];
    return { stored: { ...source, language: 'en', steps: [{ ...source.steps[0], ingredients }] }, privateFood, publicFood };
}

function typeText(input: HTMLInputElement | HTMLTextAreaElement, value: string): void {
    input.value = value;
    input.dispatchEvent(new Event('input', { bubbles: true }));
}

function recipeTextarea(host: ParentNode, selector: string): HTMLTextAreaElement {
    const input = host.querySelector(selector);
    if (!(input instanceof HTMLTextAreaElement)) {
        throw new Error(`Expected the rendered recipe field ${selector}`);
    }
    return input;
}

function recipeGalleryFileInput(host: ParentNode | null, owner: string): HTMLInputElement {
    const input = host?.querySelector(`${owner} fd-image-gallery-editor input[type="file"]`);
    if (!(input instanceof HTMLInputElement)) {
        throw new Error('Expected a native recipe gallery chooser in the current recipe');
    }
    return input;
}

const RECIPE_PHOTO = {
    filename: 'tomato-step.png',
    assetId: 'fba6230e-ab99-4eb4-a8bb-7ec39ffdd560',
    fileUrl: 'https://images.example.test/recipe-step.png',
    uploadUrl: 'https://uploads.example.test/recipe-step.png?signature=recipe-step',
};
const NEXT_RECIPE_PHOTO = {
    filename: 'fresh-soup.png',
    assetId: '98711ff7-9910-4cfe-8ab6-61890318dff7',
    fileUrl: 'https://images.example.test/fresh-soup.png',
    uploadUrl: 'https://uploads.example.test/fresh-soup.png?signature=fresh-soup',
};

async function holdRecipePhotoConfirmationAsync(
    input: HTMLInputElement,
    { filename, assetId, fileUrl, uploadUrl } = RECIPE_PHOTO,
): Promise<{ confirmation: TestRequest; fileUrl: string; assetId: string }> {
    // jsdom has no image decoder or native file picker. Exercise the existing upload pipeline after those browser boundaries.
    const decodedImage = document.createElement('img');
    Object.defineProperties(decodedImage, { naturalWidth: { value: 1 }, naturalHeight: { value: 1 } });
    vi.stubGlobal('Image', function createDecodedImage(): HTMLImageElement {
        return decodedImage;
    });
    const createObjectUrl = vi.spyOn(URL, 'createObjectURL').mockReturnValueOnce('blob:recipe-step-photo');
    const http = TestBed.inject(HttpTestingController);
    onTestFinished(() => {
        createObjectUrl.mockRestore();
        http.verify();
    });
    const png = atob('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aB5cAAAAASUVORK5CYII=');
    const file = new File([Uint8Array.from(png, byte => byte.codePointAt(0) ?? 0)], filename, { type: 'image/png' });
    Object.defineProperty(input, 'files', { configurable: true, value: [file] });
    input.dispatchEvent(new Event('change', { bubbles: true }));
    decodedImage.dispatchEvent(new Event('load'));

    const presign = await vi.waitFor(() =>
        http.expectOne(request => request.method === 'POST' && request.url.endsWith('/images/upload-url')),
    );
    expect(presign.request.body).toEqual({ fileName: file.name, contentType: file.type, fileSizeBytes: file.size });
    presign.flush({ assetId, uploadUrl, fileUrl, expiresAtUtc: '2026-05-17T10:00:00.000Z' });
    const upload = await vi.waitFor(() => http.expectOne({ method: 'PUT', url: uploadUrl }));
    expect(upload.request.body).toBe(file);
    upload.flush('');
    const confirmation = await vi.waitFor(() =>
        http.expectOne(request => request.method === 'POST' && request.url.endsWith(`/images/${assetId}/confirm`)),
    );
    return { confirmation, fileUrl, assetId };
}

function recipeEditorCoverUrl(harness: RouterTestingHarness): string | null {
    return harness.routeNativeElement?.querySelector('fd-recipe-basic-info fd-image-gallery-editor img')?.getAttribute('src') ?? null;
}

function expectRecipeGalleryImage(harness: RouterTestingHarness, selector: string, expectedUrl: string): void {
    expect(harness.routeNativeElement?.querySelector(selector)?.getAttribute('src')).toBe(expectedUrl);
}

function chooseSecondGalleryPhoto(harness: RouterTestingHarness, owner: string): void {
    const photos = harness.routeNativeElement?.querySelectorAll<HTMLButtonElement>(
        `${owner} fd-image-gallery-editor .recognition-photos__preview button`,
    );
    const second = photos?.[1];
    if (second === undefined) {
        throw new Error(`Expected two photos in ${owner}`);
    }
    second.click();
    harness.detectChanges();
    recipeTextButton(document.querySelector('[role="menu"]'), 'PRODUCT_AI_DIALOG.CHOOSE_COVER').click();
    harness.detectChanges();
}

async function goBackToRecipeAddAsync(harness: RouterTestingHarness, router: Router): Promise<void> {
    const returned = firstValueFrom(
        router.events.pipe(filter(event => event instanceof NavigationEnd && event.urlAfterRedirects === ADD_RECIPE)),
    );
    TestBed.inject(Location).back();
    await returned;
    harness.detectChanges();
}

function openRecipeImportSource(harness: RouterTestingHarness): HTMLInputElement {
    const host = harness.routeNativeElement;
    const summary = host?.querySelector('details.recipe-manage__import-card summary');
    if (!(summary instanceof HTMLElement)) {
        throw new Error('Expected the recipe import disclosure');
    }
    summary.click();
    harness.detectChanges();
    const source = host?.querySelector('input[aria-label="RECIPE_MANAGE.IMPORT.URL_LABEL"]');
    if (!(source instanceof HTMLInputElement)) {
        throw new Error('Expected the recipe source URL input');
    }
    return source;
}

function registerRecipeEditorScreen(): WebmcpScreenRegistry {
    const screens = new WebmcpScreenRegistry();
    TestBed.configureTestingModule({ providers: [{ provide: DEVELOPMENT_SCREENS, useValue: screens }] });
    return screens;
}

function recipeBasicInput(harness: RouterTestingHarness, key: string): HTMLInputElement {
    const labels = harness.routeNativeElement?.querySelectorAll<HTMLLabelElement>('fd-recipe-basic-info label') ?? [];
    const label = [...labels].find(element => element.textContent.includes(key));
    const control = label?.control;
    if (!(control instanceof HTMLInputElement)) {
        throw new Error(`Expected the recipe input ${key}`);
    }
    return control;
}

function recipeFieldError(input: HTMLInputElement | HTMLTextAreaElement): string | null {
    const description = input.getAttribute('aria-describedby');
    return description === null ? null : (document.getElementById(description)?.textContent.trim() ?? null);
}

function selectPublicRecipeVisibility(harness: RouterTestingHarness): void {
    const host = harness.routeNativeElement;
    const section = host?.querySelector('fd-recipe-basic-info details');
    if (!(section instanceof HTMLDetailsElement)) {
        throw new Error('Expected the recipe advanced options');
    }
    section.querySelector('summary')?.click();
    harness.detectChanges();
    const label = [...section.querySelectorAll('label')].find(element => element.textContent.includes('RECIPE_MANAGE.VISIBILITY'));
    const trigger = label?.control;
    if (!(trigger instanceof HTMLButtonElement)) {
        throw new Error('Expected the recipe visibility control');
    }
    trigger.click();
    harness.detectChanges();
    recipeTextButton(document.querySelector('[role="listbox"][aria-label="RECIPE_MANAGE.VISIBILITY"]'), 'RECIPE_VISIBILITY.Public').click();
    harness.detectChanges();
    expect(trigger.textContent).toContain('RECIPE_VISIBILITY.Public');
}

function replyWithRecipePublicationFoods(http: HttpTestingController, foods: readonly PublicationFood[]): void {
    for (const food of foods) {
        http.expectOne(request => request.method === 'GET' && request.url.endsWith(`/api/v1/products/${food.id}`)).flush(food);
    }
}

async function recipePublicationActionAsync(harness: RouterTestingHarness, label: string): Promise<HTMLButtonElement> {
    return vi.waitFor(() => {
        harness.detectChanges();
        return recipeTextButton(document.querySelector(PUBLICATION_DIALOG), label);
    });
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

async function chooseRecipeConfirmationAsync(harness: RouterTestingHarness, label: string): Promise<void> {
    await vi.waitFor(() => {
        harness.detectChanges();
        recipeTextButton(document.querySelector('fd-confirm-delete-dialog'), label).click();
    });
}

function recipeTextButton(host: ParentNode | null, label: string): HTMLButtonElement {
    const button = [...(host?.querySelectorAll('button') ?? [])].find(
        candidate => (candidate.querySelector('.fd-ui-button__label')?.textContent ?? candidate.textContent).trim() === label,
    );
    if (button === undefined) {
        throw new Error(`Expected the rendered recipe control ${label}`);
    }
    return button;
}

function recipeNutritionInput(
    harness: RouterTestingHarness,
    nutrient: 'calories' | 'proteins' | 'fats' | 'carbs' | 'alcohol',
): HTMLInputElement {
    const input = harness.routeNativeElement?.querySelector(`.nutrition-editor__input--${nutrient} input`);
    if (!(input instanceof HTMLInputElement)) {
        throw new Error(`Expected the rendered recipe ${nutrient} field`);
    }
    return input;
}

function recipeName(harness: RouterTestingHarness): HTMLInputElement {
    const name = harness.routeNativeElement?.querySelector('fd-recipe-basic-info input');
    if (!(name instanceof HTMLInputElement)) {
        throw new Error('Expected the rendered recipe name input');
    }
    return name;
}
