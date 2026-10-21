import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { ProductService } from '../../features/products/api/product.service';
import { RecipeService } from '../../features/recipes/api/recipe.service';
import { RecipeDevelopmentDraft } from '../../features/recipes/contracts/recipe-development-draft';
import { DevelopmentToolError } from '../../shared/development/development-tools';
import { createEmptyProductSnapshot, createEmptyRecipeSnapshot } from '../../shared/models/meal.data';
import { entityId } from '../../shared/models/semantics/entity-id';
import { LocalWebmcpApplicationService } from './local-webmcp-application.service';
import { LocalWebmcpCatalogService } from './local-webmcp-catalog.service';
import { LocalWebmcpRequestsService } from './local-webmcp-requests.service';
import { LocalWebmcpScreensService } from './local-webmcp-screens.service';

const APPLE_AMOUNT = 150;
const APPLE_CALORIES = 52;
const SMALL_VALID_INGREDIENT_AMOUNT = 0.001;
const HTTP_OK = 200;

describe('published development screen ownership', () => {
    it('closes a clean published dialog before navigation and keeps dirty dialogs intact', () => {
        const screens = new LocalWebmcpScreensService();
        let dirty = true;
        const beforeNavigate = vi.fn();
        screens.attach({ kind: 'dialog', read: () => ({}), isDirty: () => dirty, beforeNavigate });
        expect(() => {
            screens.prepareNavigation();
        }).toThrow('Unsaved');
        expect(beforeNavigate).not.toHaveBeenCalled();
        dirty = false;
        screens.prepareNavigation();
        expect(beforeNavigate).toHaveBeenCalledOnce();
    });
    it('restores the page after a dialog closes and preserves underlying dirty state', () => {
        const screens = new LocalWebmcpScreensService();
        const page = { kind: 'page', read: (): unknown => ({ value: 'owned' }), isDirty: (): boolean => true };
        const disposePage = screens.attach(page);
        const disposeDialog = screens.attach({ kind: 'dialog', read: () => ({}) });
        expect(screens.current()?.kind).toBe('dialog');
        expect(screens.isDirty()).toBe(true);
        disposeDialog();
        expect(screens.current()).toBe(page);
        disposePage();
        expect(screens.read()).toBeNull();
    });

    it('rejects a stale discard revision after a user edits the draft', () => {
        const screens = new LocalWebmcpScreensService();
        let value = 'first';
        const clearDraft = vi.fn();
        screens.attach({ kind: 'new-editor', read: () => ({ value }), clearDraft });
        screens.read();
        value = 'edited';
        expect(() => {
            screens.clear(1);
        }).toThrow(DevelopmentToolError);
        expect(clearDraft).not.toHaveBeenCalled();
        screens.clear(2);
        expect(clearDraft).toHaveBeenCalledOnce();
    });
});

describe('audit development operations', () => {
    let screens: LocalWebmcpScreensService;
    const navigateByUrl = vi.fn().mockResolvedValue(true);
    const requireClean = vi.fn();
    const requireSession = vi.fn();
    const read = (): unknown => ({ screen: screens.read() });
    const callAsync = async (name: string, input: unknown): Promise<unknown> => {
        const service = TestBed.inject(LocalWebmcpApplicationService);
        const tool = service.operations(read, requireClean, requireSession).find(operation => operation.name === `fooddiary_${name}`);
        if (tool === undefined) {
            throw new Error('Missing operation.');
        }
        return await tool.run(input);
    };

    beforeEach(() => {
        vi.clearAllMocks();
        TestBed.configureTestingModule({ providers: [{ provide: Router, useValue: { url: '/dashboard', navigateByUrl } }] });
        screens = TestBed.inject(LocalWebmcpScreensService);
    });

    it('refuses an arbitrary route before invoking navigation', async () => {
        await expect(callAsync('open_section', { section: 'https://external.example' })).rejects.toThrow(DevelopmentToolError);
        expect(navigateByUrl).not.toHaveBeenCalled();
    });

    it('checks unsaved changes before navigating', async () => {
        requireClean.mockImplementationOnce(() => {
            throw new DevelopmentToolError('Unsaved');
        });
        await expect(callAsync('open_section', { section: 'recipes' })).rejects.toThrow('Unsaved');
        expect(navigateByUrl).not.toHaveBeenCalled();
    });

    it('passes a draft only to the current matching capability and never submits', async () => {
        const fillDraft = vi.fn();
        screens.attach({ kind: 'product-editor', read: () => ({}), fillDraft });
        await callAsync('fill_current_draft', { kind: 'product-editor', draft: { name: 'Apple' } });
        expect(fillDraft).toHaveBeenCalledWith({ name: 'Apple' });
        expect(navigateByUrl).not.toHaveBeenCalled();
        expect(requireSession).toHaveBeenCalled();
        await expect(callAsync('fill_current_draft', { kind: 'weight-editor', draft: {} })).rejects.toThrow(DevelopmentToolError);
        expect(fillDraft).toHaveBeenCalledOnce();
    });

    it('requires the current revision before filling cycle settings', async () => {
        const initial = 28;
        const changed = 30;
        let value = initial;
        const fillDraft = vi.fn();
        screens.attach({ kind: 'cycle-settings', read: () => ({ value }), fillDraft });
        screens.read();
        await expect(callAsync('fill_current_draft', { kind: 'cycle-settings', draft: {} })).rejects.toThrow();
        await callAsync('fill_current_draft', { kind: 'cycle-settings', expectedRevision: 1, draft: { averageCycleLength: 29 } });
        value = changed;
        await expect(callAsync('fill_current_draft', { kind: 'cycle-settings', expectedRevision: 1, draft: {} })).rejects.toThrow(
            'Screen changed',
        );
        expect(fillDraft).toHaveBeenCalledOnce();
    });

    it('rejects a mismatched timezone before changing the calendar range', async () => {
        const setFiltersAsync = vi.fn();
        screens.attach({ kind: 'meal-diary', read: () => ({}), setFiltersAsync });
        await expect(
            callAsync('set_calendar_period', { dateFrom: '2026-10-16', dateTo: '2026-10-17', timeZone: 'Invalid/Zone' }),
        ).rejects.toThrow(DevelopmentToolError);
        expect(setFiltersAsync).not.toHaveBeenCalled();
    });
});

describe('catalog fractional compatibility', () => {
    it('preserves small positive product and nested quantities while rejecting zero and negative amounts', async () => {
        const product = { ...createEmptyProductSnapshot(), id: entityId<'product'>('owned-product') };
        const recipe = { ...createEmptyRecipeSnapshot(), id: entityId<'recipe'>('owned-recipe'), servings: 1 };
        TestBed.configureTestingModule({
            providers: [
                { provide: ProductService, useValue: { getById: vi.fn().mockReturnValue(of(product)) } },
                { provide: RecipeService, useValue: { getById: vi.fn().mockReturnValue(of(recipe)) } },
            ],
        });
        const catalog = TestBed.inject(LocalWebmcpCatalogService);
        const ingredients = [
            { productId: product.id, amount: SMALL_VALID_INGREDIENT_AMOUNT, unit: 'G' },
            { recipeId: recipe.id, amount: SMALL_VALID_INGREDIENT_AMOUNT, unit: 'servings' },
        ];
        const draft = await catalog.recipeDraftAsync({
            name: 'Small quantities',
            servings: 1,
            steps: [{ description: 'Mix', ingredients }],
        });
        expect(draft.steps[0].ingredients.map(item => item.amount)).toEqual([SMALL_VALID_INGREDIENT_AMOUNT, SMALL_VALID_INGREDIENT_AMOUNT]);

        for (const amount of [0, -1]) {
            await expect(
                catalog.recipeDraftAsync({
                    name: 'Invalid',
                    servings: 1,
                    steps: [{ description: 'Mix', ingredients: [{ ...ingredients[0], amount }] }],
                }),
            ).rejects.toThrow('outside the supported range');
        }
    });
});

describe('catalog resolution and safe request diagnostics', () => {
    it('resolves an exact accessible nested recipe and rejects mixed sources or mass units', async () => {
        const recipe = { ...createEmptyRecipeSnapshot(), id: entityId<'recipe'>('nested-recipe'), servings: 2 };
        const getById = vi.fn().mockReturnValue(of(recipe));
        TestBed.configureTestingModule({
            providers: [
                { provide: ProductService, useValue: {} },
                { provide: RecipeService, useValue: { getById } },
            ],
        });
        const catalog = TestBed.inject(LocalWebmcpCatalogService);
        const base = {
            name: 'Parent',
            servings: 1,
            steps: [{ description: 'Combine', ingredients: [{ recipeId: recipe.id, amount: 0.5, unit: 'servings' }] }],
        };
        const draft = await catalog.recipeDraftAsync(base);
        expect(getById).toHaveBeenCalledWith(recipe.id);
        expect(draft.steps[0].ingredients).toEqual([{ recipe, amount: 0.5 }]);
        for (const ingredient of [
            { recipeId: recipe.id, amount: 1, unit: 'G' },
            { recipeId: recipe.id, productId: 'mixed', amount: 1, unit: 'servings' },
        ]) {
            await expect(
                catalog.recipeDraftAsync({ ...base, steps: [{ description: 'Combine', ingredients: [ingredient] }] }),
            ).rejects.toThrow();
        }
        getById.mockReturnValueOnce(of(null));
        await expect(catalog.recipeDraftAsync(base)).rejects.toThrow('unavailable');
    });
    it('resolves existing ingredient IDs and retains product amount and declared units', async () => {
        const product = { ...createEmptyProductSnapshot(), id: entityId<'product'>('owned-apple'), caloriesPerBase: APPLE_CALORIES };
        const getById = vi.fn().mockReturnValue(of(product));
        TestBed.configureTestingModule({ providers: [{ provide: ProductService, useValue: { getById } }] });
        const catalog = TestBed.inject(LocalWebmcpCatalogService);
        const draft = await catalog.recipeDraftAsync({
            name: 'Apple recipe',
            servings: 2,
            steps: [{ description: 'Slice', ingredients: [{ productId: product.id, amount: APPLE_AMOUNT, unit: 'G' }] }],
        });
        expect(draft).toBeInstanceOf(RecipeDevelopmentDraft);
        expect(draft.steps[0].ingredients[0]).toEqual({ product, amount: APPLE_AMOUNT });
        await expect(
            catalog.recipeDraftAsync({
                name: 'Wrong unit',
                servings: 1,
                steps: [{ description: '', ingredients: [{ productId: product.id, amount: 1, unit: 'ML' }] }],
            }),
        ).rejects.toThrow('unit mismatch');
    });

    it('does not expose paths, tokens, query values or auth requests in diagnostics', () => {
        const requests = TestBed.inject(LocalWebmcpRequestsService);
        requests.begin('POST', new URL('http://localhost/api/v1/auth?token=secret'))();
        const finish = requests.begin('GET', new URL('http://localhost/api/v1/products/private-id?code=secret'));
        expect(requests.read()).toHaveLength(1);
        expect(requests.read()[0].pending).toBe(true);
        finish(HTTP_OK);
        expect(requests.read()[0].status).toBe(HTTP_OK);
        expect(JSON.stringify(requests.read())).not.toMatch(/private-id|secret|token|code|auth/);
        requests.clear();
        expect(requests.read()).toHaveLength(0);
    });
});
