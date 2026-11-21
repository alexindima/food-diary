import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ChangeDetectionStrategy, Component, signal, type WritableSignal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { of, Subject, throwError } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../environments/environment';
import { provideTranslateTesting } from '../../../testing/translate-testing.module';
import { MEAL_DEVELOPMENT_TOOLS, type MealDevelopmentScreen } from '../../features/meals/contracts/meal-development-tools';
import { ProductService } from '../../features/products/api/product.service';
import { RecipeService } from '../../features/recipes/api/recipe.service';
import { WeightHistoryEntryDialogComponent } from '../../features/weight-history/dialogs/weight-history-entry-dialog/weight-history-entry-dialog';
import { WeightHistoryFacade } from '../../features/weight-history/lib/weight-history.facade';
import { AuthService } from '../../services/auth.service';
import { DEVELOPMENT_REQUESTS } from '../../shared/development/development-tools';
import { MeasurementSystemService } from '../../shared/measurements/measurement-system.service';
import { createEmptyProductSnapshot, createEmptyRecipeSnapshot } from '../../shared/models/meal.data';
import type { PageOf } from '../../shared/models/page-of.data';
import type { Product } from '../../shared/models/product.data';
import type { Recipe } from '../../shared/models/recipe.data';
import { entityId } from '../../shared/models/semantics/entity-id';
import { LOCAL_WEBMCP_PROVIDERS } from './local-webmcp.providers';
import { LOCAL_WEBMCP_PROVIDERS as PRODUCTION_WEBMCP_PROVIDERS } from './local-webmcp.providers.production';
import type { LocalModelContext, LocalTool } from './local-webmcp.types';
import { LocalWebmcpPilotService } from './local-webmcp-pilot.service';
import { LocalWebmcpRequestsService } from './local-webmcp-requests.service';
import { LocalWebmcpScreensService } from './local-webmcp-screens.service';

const BROWSER_DIAGNOSTICS_SELECTOR = 'meta[name="fooddiary-webmcp"]';
const CATALOG_LIMIT = 25;
const BUILD_FINGERPRINT_LENGTH = 64;
const GIT_REVISION_LENGTH = 40;
const RECIPE_SERVINGS = 3;
const DRAFT_SERVINGS = 1.5;
const PRODUCT_CALORIES = 52;
const SOURCE_RECIPE_CALORIES = 175;
const INGREDIENT_LIMIT_EXCEEDED = 21;
const INITIAL_USER = 'local-account-a';
const REPLACEMENT_USER = 'local-account-b';
const RECIPE = {
    ...createEmptyRecipeSnapshot(),
    id: entityId<'recipe'>('oat-bowl'),
    name: 'Oat bowl',
    servings: RECIPE_SERVINGS,
    totalCalories: SOURCE_RECIPE_CALORIES,
};
const PRODUCT = {
    ...createEmptyProductSnapshot(),
    id: entityId<'product'>('apple'),
    name: 'Apple',
    caloriesPerBase: PRODUCT_CALORIES,
};
const MEAL_DRAFT = {
    date: '2026-10-17',
    time: '12:30',
    mealType: 'LUNCH',
    comment: 'Pack lunch',
    items: [{ recipeId: RECIPE.id, servings: DRAFT_SERVINGS }],
};

@Component({ template: '', changeDetection: ChangeDetectionStrategy.OnPush })
class LocalRenderFixture {}

type PilotContext = {
    tools: Map<string, LocalTool>;
    account: WritableSignal<string | null>;
    fixture: ComponentFixture<LocalRenderFixture>;
    router: { url: string; navigate: ReturnType<typeof vi.fn<Router['navigate']>> };
    products: { query: ReturnType<typeof vi.fn<ProductService['query']>> };
    recipes: { query: ReturnType<typeof vi.fn<RecipeService['query']>>; getById: ReturnType<typeof vi.fn<RecipeService['getById']>> };
    registerTool: ReturnType<typeof vi.fn<LocalModelContext['registerTool']>>;
};

let previousModelContext: PropertyDescriptor | undefined;

beforeEach(() => {
    previousModelContext = Object.getOwnPropertyDescriptor(document, 'modelContext');
});

afterEach(() => {
    TestBed.resetTestingModule();
    document.querySelector(BROWSER_DIAGNOSTICS_SELECTOR)?.remove();
    if (previousModelContext === undefined) {
        Reflect.deleteProperty(document, 'modelContext');
    } else {
        Object.defineProperty(document, 'modelContext', previousModelContext);
    }
});

describe('registered local WebMCP tools and account lifecycle', () => {
    it('registers once on sign-in, clears old provenance and unregisters session tools on account changes', async () => {
        const { account, fixture, tools, registerTool } = createPilot(null);
        await fixture.whenStable();
        expect(readDiagnostics()).toMatchObject({ authenticated: false, registration: 'signed-out', registeredToolCount: 0 });
        expect(tools.size).toBe(0);

        account.set(INITIAL_USER);
        fixture.detectChanges();
        await waitForRegistrationAsync();
        const registeredCount = tools.size;
        expect(registeredCount).toBeGreaterThan(0);
        TestBed.runInInjectionContext(() => {
            TestBed.inject(LocalWebmcpPilotService).initialize();
        });
        expect(registerTool).toHaveBeenCalledTimes(registeredCount);

        const binaryFingerprint = 'a'.repeat(BUILD_FINGERPRINT_LENGTH);
        const instanceId = '96ea80f4-bbab-4fc4-9698-6572d54dc8ae';
        const observer = TestBed.inject(DEVELOPMENT_REQUESTS);
        const unknownPath = observer.observe('GET', 'http://[');
        expect(unknownPath.requestId).toBeUndefined();
        unknownPath();
        const interrupted = observer.observe('GET', '/api/v1/dashboard');
        interrupted();
        const observed = observer.observe('GET', '/api/v1/recipes?search=private-recipe');
        observed(HttpStatusCode.Ok, { apiBuild: binaryFingerprint, apiInstance: instanceId });
        const originalTool = lookup(tools, 'fooddiary_read_screen_context');
        const original = await originalTool.execute({});
        expect(original).toMatchObject({
            provenance: { sessionRevision: 1, apiRuntimeStatus: 'observed-response-headers' },
            application: {
                apiRuntimeObservations: [{ binaryFingerprint, instanceId, requestId: observed.requestId }],
                requests: [
                    { id: interrupted.requestId, resource: 'dashboard', status: null, pending: false },
                    { id: observed.requestId, resource: 'recipes', status: HttpStatusCode.Ok, pending: false },
                ],
            },
        });
        expect(JSON.stringify(original)).not.toContain('private-recipe');

        account.set(REPLACEMENT_USER);
        fixture.detectChanges();
        await vi.waitFor(() => {
            expect(registerTool).toHaveBeenCalledTimes(registeredCount * 2);
        });
        await waitForRegistrationAsync();
        expect(tools.size).toBe(registeredCount);
        expect(await originalTool.execute({})).toMatchObject({ ok: false, error: 'The authenticated session changed.' });
        const currentTool = lookup(tools, 'fooddiary_read_screen_context');
        expect(currentTool).not.toBe(originalTool);
        expect(await currentTool.execute({})).toMatchObject({
            provenance: { sessionRevision: 2, apiRuntimeStatus: 'not-observed' },
            application: { requests: [] },
        });

        account.set(null);
        fixture.detectChanges();
        await vi.waitFor(() => {
            expect(tools.size).toBe(0);
            expect(readDiagnostics()).toMatchObject({ authenticated: false, registration: 'signed-out', registeredToolCount: 0 });
        });
    });

    it('leaves request observation and tool registration disabled when the browser API is unavailable', async () => {
        const { fixture, tools, registerTool } = createPilot(INITIAL_USER, false);
        await fixture.whenStable();
        expect(readDiagnostics()).toMatchObject({ authenticated: true, browserApi: false, registration: 'not-registered' });
        const completion = TestBed.inject(DEVELOPMENT_REQUESTS).observe('GET', '/api/v1/recipes');
        completion(HttpStatusCode.Ok);
        expect(completion.requestId).toBeUndefined();
        expect(TestBed.inject(LocalWebmcpRequestsService).read()).toEqual([]);
        expect(tools.size).toBe(0);
        expect(registerTool).not.toHaveBeenCalled();
    });
});

describe('registered local WebMCP build provenance', () => {
    it('reports the task-emitted frontend source capture without inferring an API build', async () => {
        const savedCapture = Object.getOwnPropertyDescriptor(environment, 'developmentBuild');
        const sourceCapture = {
            revision: 'a'.repeat(GIT_REVISION_LENGTH),
            sourceFingerprint: 'b'.repeat(BUILD_FINGERPRINT_LENGTH),
            capturedAtUtc: '2026-11-21T09:30:00.000Z',
            fingerprintBasis: 'frontend-source-and-task-configuration',
        };
        try {
            environment.developmentBuild = sourceCapture;
            const { tools } = createPilot();
            await waitForRegistrationAsync();

            const result = await lookup(tools, 'fooddiary_read_screen_context').execute({});
            expect(result).toMatchObject({
                provenance: { apiRuntimeStatus: 'not-observed' },
                application: {
                    buildRevision: sourceCapture.revision,
                    frontendBuild: { version: environment.buildVersion, availability: 'emitted', sourceCapture },
                    apiRuntimeObservations: [],
                    requests: [],
                },
            });
        } finally {
            if (savedCapture === undefined) {
                delete environment.developmentBuild;
            } else {
                Object.defineProperty(environment, 'developmentBuild', savedCapture);
            }
        }
    });

    it('installs no development entry points from the production provider variant on loopback', async () => {
        const { fixture, registerTool } = createPilot(INITIAL_USER, true, PRODUCTION_WEBMCP_PROVIDERS);
        await fixture.whenStable();

        expect(TestBed.inject(MEAL_DEVELOPMENT_TOOLS, null)).toBeNull();
        expect(TestBed.inject(DEVELOPMENT_REQUESTS, null)).toBeNull();
        expect(readDiagnostics()).toBeNull();
        expect(registerTool).not.toHaveBeenCalled();
    });
});

describe('registered local WebMCP meal draft operations', () => {
    it('fills a registered meal draft with the exact recipe and accepts explicit discard without submitting', async () => {
        const { tools, recipes, router } = createPilot();
        await waitForRegistrationAsync();
        recipes.getById.mockReturnValueOnce(of(null));
        let note = '';
        const fillDraft = vi.fn<NonNullable<MealDevelopmentScreen['fillDraft']>>((draft, requireSession) => {
            requireSession();
            note = draft.comment;
        });
        const owner = TestBed.inject(MEAL_DEVELOPMENT_TOOLS);
        const detach = owner.attach({
            read: () => ({ note }),
            isDirty: () => note !== '',
            fillDraft,
            clearDraft: () => {
                note = '';
            },
        });
        const tool = lookup(tools, 'fooddiary_fill_meal_draft');
        expect(await tool.execute(MEAL_DRAFT)).toMatchObject({ ok: false, error: 'Recipe missing or unavailable.' });
        expect(fillDraft).not.toHaveBeenCalled();
        expect(await tool.execute(MEAL_DRAFT)).toMatchObject({ saved: false, screen: { kind: 'meal-editor', dirty: true } });
        expect(recipes.getById).toHaveBeenLastCalledWith(RECIPE.id);
        expect(fillDraft).toHaveBeenCalledExactlyOnceWith(
            { ...MEAL_DRAFT, items: [{ recipe: RECIPE, servings: DRAFT_SERVINGS }] },
            expect.any(Function),
        );
        expect(router.navigate).not.toHaveBeenCalled();

        const screen = TestBed.inject(LocalWebmcpScreensService).read();
        expect(
            await lookup(tools, 'fooddiary_clear_current_draft').execute({ revision: screen?.revision, confirmDiscard: true }),
        ).toMatchObject({
            screen: { kind: 'meal-editor', dirty: false, data: { note: '' } },
        });
        detach();
        expect(await lookup(tools, 'fooddiary_read_screen_context').execute({})).toMatchObject({ screen: null });
    });
});

describe('registered local WebMCP navigation and lookup failure', () => {
    it('preserves the current screen when diary navigation is declined or no unsaved-change capability is published', async () => {
        const { tools, router } = createPilot();
        await waitForRegistrationAsync();
        router.navigate.mockResolvedValueOnce(false);
        const diary = lookup(tools, 'fooddiary_open_diary_date');
        expect(await diary.execute({ date: MEAL_DRAFT.date })).toMatchObject({ ok: false, error: 'Diary navigation was cancelled.' });
        router.url = '/lessons';
        expect(await diary.execute({ date: MEAL_DRAFT.date })).toMatchObject({
            ok: false,
            error: 'This screen does not publish unsaved-change state. Navigate through the interface first.',
        });
        expect(router.navigate).toHaveBeenCalledOnce();
    });

    it('ignores resolved recipe data after navigation and reports a canceled or unpublished editor on retry', async () => {
        const { tools, router, recipes } = createPilot();
        await waitForRegistrationAsync();
        const pending = new Subject<Recipe>();
        recipes.getById.mockReturnValueOnce(pending);
        const fill = lookup(tools, 'fooddiary_fill_meal_draft');
        const resolving = fill.execute(MEAL_DRAFT);
        await vi.waitFor(() => {
            expect(recipes.getById).toHaveBeenCalledOnce();
        });
        router.url = '/recipes';
        pending.next(RECIPE);
        pending.complete();
        expect(await resolving).toMatchObject({
            ok: false,
            error: 'Screen changed while loading products. Retry from the current screen.',
        });
        expect(router.navigate).not.toHaveBeenCalled();

        router.navigate.mockResolvedValueOnce(false);
        expect(await fill.execute(MEAL_DRAFT)).toMatchObject({ ok: false, error: 'Editor navigation was cancelled.' });
        expect(await fill.execute(MEAL_DRAFT)).toMatchObject({ ok: false, error: 'A clean new meal editor is required.' });
        expect(router.navigate).toHaveBeenCalledWith(['/meals/add'], { queryParams: { mealType: MEAL_DRAFT.mealType } });
    });

    it('reports only public error text when an internal lookup fails', async () => {
        const { tools, recipes, router } = createPilot();
        await waitForRegistrationAsync();
        recipes.getById.mockReturnValueOnce(throwError(() => new Error('private internal error')));

        expect(await lookup(tools, 'fooddiary_fill_meal_draft').execute(MEAL_DRAFT)).toEqual({
            ok: false,
            error: 'The tool could not complete. Inspect the current screen and retry.',
        });
        expect(router.navigate).not.toHaveBeenCalled();
    });
});

describe('registered local WebMCP catalog reads', () => {
    it('projects product and recipe search results using bounded existing catalog queries', async () => {
        const { tools, products, recipes } = createPilot();
        await waitForRegistrationAsync();
        const search = lookup(tools, 'fooddiary_search_catalog');

        expect(await search.execute({ kind: 'product', search: 'Apple' })).toMatchObject({
            page: 1,
            totalItems: 1,
            data: [{ id: PRODUCT.id, name: PRODUCT.name, unit: PRODUCT.baseUnit, caloriesPerBase: PRODUCT_CALORIES }],
        });
        expect(products.query).toHaveBeenCalledExactlyOnceWith(1, CATALOG_LIMIT, { search: 'Apple' });
        recipes.query.mockReturnValueOnce(of(makePage(RECIPE, 2)));
        expect(await search.execute({ kind: 'recipe', search: 'Oats', page: 2 })).toEqual({
            ...makePage(RECIPE, 2),
            data: [{ id: RECIPE.id, name: RECIPE.name, servings: RECIPE_SERVINGS, totalCalories: SOURCE_RECIPE_CALORIES }],
        });
        expect(recipes.query).toHaveBeenCalledExactlyOnceWith(2, CATALOG_LIMIT, { search: 'Oats' });

        expect(await search.execute({ kind: 'recipe', search: 'Oats', page: DRAFT_SERVINGS })).toEqual({
            ok: false,
            error: 'Page must be an integer.',
        });
        expect(recipes.query).toHaveBeenCalledOnce();
    });

    it('refuses invalid servings and oversized ingredient lists before handing data to the current recipe editor', async () => {
        const { tools, products } = createPilot();
        await waitForRegistrationAsync();
        const fillDraft = vi.fn();
        TestBed.inject(LocalWebmcpScreensService).attach({ kind: 'recipe-editor', read: () => ({}), fillDraft });
        const fill = lookup(tools, 'fooddiary_fill_current_draft');
        const draft = { name: 'Oat bowl', servings: DRAFT_SERVINGS, steps: [{ description: 'Mix', ingredients: [] }] };
        expect(await fill.execute({ kind: 'recipe-editor', draft })).toMatchObject({
            ok: false,
            error: 'Expected integer servings and 1–10 steps.',
        });

        const ingredients = Array.from({ length: INGREDIENT_LIMIT_EXCEEDED }, () => ({ productId: PRODUCT.id, amount: 1, unit: 'G' }));
        const oversized = { ...draft, servings: RECIPE_SERVINGS, steps: [{ description: 'Mix', ingredients }] };
        expect(await fill.execute({ kind: 'recipe-editor', draft: oversized })).toEqual({ ok: false, error: 'Too many ingredients.' });
        expect(fillDraft).not.toHaveBeenCalled();
        expect(products.query).not.toHaveBeenCalled();
    });
});

describe('registered local WebMCP weight drafts', () => {
    it('requires displayed units and preserves a rendered unsaved weight instead of overwriting it', async () => {
        const close = vi.fn();
        const { tools, router } = createPilot(INITIAL_USER, true, [
            ...LOCAL_WEBMCP_PROVIDERS,
            provideHttpClient(),
            provideHttpClientTesting(),
            provideTranslateTesting(),
            WeightHistoryFacade,
            { provide: FdUiDialogRef, useValue: { close } },
        ]);
        await waitForRegistrationAsync();

        const measurement = TestBed.inject(MeasurementSystemService);
        const originalSystem = measurement.system();
        measurement.setSystem('imperial');
        try {
            const dialog = TestBed.createComponent(WeightHistoryEntryDialogComponent);
            dialog.detectChanges();
            const host = dialog.nativeElement as HTMLElement;
            const input = host.querySelector<HTMLInputElement>('fd-ui-input input');
            if (input === null) {
                throw new Error('Expected a rendered weight input');
            }

            const read = lookup(tools, 'fooddiary_read_screen_context');
            expect(await read.execute({})).toMatchObject({ screen: { kind: 'weight-editor', dirty: false, data: { unit: 'lb' } } });
            const fill = lookup(tools, 'fooddiary_fill_current_draft');
            const draft = { date: '2026-06-21', value: 180, unit: 'kg' };
            expect(await fill.execute({ kind: 'weight-editor', draft })).toMatchObject({
                ok: false,
                error: 'Measurement display unit does not match.',
            });
            dialog.detectChanges();
            expect(input.value).toBe('');

            const corrected = { ...draft, unit: 'lb' };
            expect(await fill.execute({ kind: 'weight-editor', draft: corrected })).toMatchObject({ saved: false });
            dialog.detectChanges();
            expect(input.value).toBe(String(corrected.value));

            input.value = '181';
            input.dispatchEvent(new Event('input', { bubbles: true }));
            dialog.detectChanges();
            expect(await fill.execute({ kind: 'weight-editor', draft: corrected })).toMatchObject({
                ok: false,
                error: 'Unsaved draft exists. Review, save or discard it in the interface first.',
            });
            expect(input.value).toBe('181');
            expect(close).not.toHaveBeenCalled();
            expect(router.navigate).not.toHaveBeenCalled();
            TestBed.inject(HttpTestingController).verify();
        } finally {
            measurement.setSystem(originalSystem);
        }
    });
});

function createPilot(initialUser: string | null = INITIAL_USER, browserApi = true, providers = LOCAL_WEBMCP_PROVIDERS): PilotContext {
    const account = signal(initialUser);
    const tools = new Map<string, LocalTool>();
    const registerTool = vi.fn<LocalModelContext['registerTool']>((tool, options) => {
        tools.set(tool.name, tool);
        options?.signal?.addEventListener(
            'abort',
            () => {
                if (tools.get(tool.name) === tool) {
                    tools.delete(tool.name);
                }
            },
            { once: true },
        );
    });
    Object.defineProperty(document, 'modelContext', { value: browserApi ? { registerTool } : undefined, configurable: true });
    const router = { url: '/meals', navigate: vi.fn<Router['navigate']>().mockResolvedValue(true) };
    const products = { query: vi.fn<ProductService['query']>().mockReturnValue(of(makePage(PRODUCT))) };
    const recipes = {
        query: vi.fn<RecipeService['query']>().mockReturnValue(of(makePage(RECIPE))),
        getById: vi.fn<RecipeService['getById']>().mockReturnValue(of(RECIPE)),
    };
    TestBed.configureTestingModule({
        imports: [LocalRenderFixture],
        providers: [
            ...providers,
            { provide: Router, useValue: router },
            {
                provide: AuthService,
                useValue: {
                    getUserId: account,
                    isAuthenticated: (): boolean => account() !== null,
                    isEmailConfirmed: (): boolean => account() !== null,
                    isAdmin: (): boolean => false,
                    isDietologist: (): boolean => false,
                },
            },
            { provide: ProductService, useValue: products },
            { provide: RecipeService, useValue: recipes },
        ],
    });
    const fixture = TestBed.createComponent(LocalRenderFixture);
    fixture.detectChanges();
    return { tools, account, fixture, router, products, recipes, registerTool };
}

function makePage<T extends Product | Recipe>(item: T, page = 1): PageOf<T> {
    return { data: [item], page, limit: CATALOG_LIMIT, totalPages: page, totalItems: (page - 1) * CATALOG_LIMIT + 1 };
}

function lookup(tools: ReadonlyMap<string, LocalTool>, name: string): LocalTool {
    const tool = tools.get(name);
    if (tool === undefined) {
        throw new Error(`Missing registered tool "${name}"`);
    }
    return tool;
}

function readDiagnostics(): unknown {
    const content = document.querySelector<HTMLMetaElement>(BROWSER_DIAGNOSTICS_SELECTOR)?.content;
    return content === undefined ? null : (JSON.parse(content) as unknown);
}

async function waitForRegistrationAsync(): Promise<void> {
    await vi.waitFor(() => {
        expect(readDiagnostics()).toMatchObject({ authenticated: true, registration: 'ready' });
    });
}
