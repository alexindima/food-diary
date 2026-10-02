import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AdminCatalogService } from '../api/admin-catalog.service';
import type { CatalogRecipe } from '../models/catalog-transfer';
import { AdminCatalogFacade } from './admin-catalog.facade';

const firstId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
const secondId = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';

function recipe(id: string, nestedRecipeId: string | null = null): CatalogRecipe {
    return {
        id,
        name: 'Каша',
        description: null,
        category: null,
        imageUrl: null,
        prepTime: null,
        cookTime: null,
        servings: 1,
        language: 'ru',
        languageConfirmed: true,
        calculateNutritionAutomatically: true,
        manualCalories: null,
        manualProteins: null,
        manualFats: null,
        manualCarbs: null,
        manualFiber: null,
        manualAlcohol: null,
        steps: [
            {
                order: 1,
                description: 'Варить',
                title: null,
                imageUrl: null,
                ingredients: nestedRecipeId === null ? [] : [{ productId: null, nestedRecipeId, amount: 1 }],
            },
        ],
    };
}

function file(recipes: CatalogRecipe[]): File {
    const text = JSON.stringify({ format: 'fooddiary-catalog', version: 1, products: [], recipes });
    const selected = new File([text], 'catalog.json');
    Object.defineProperty(selected, 'text', { value: vi.fn().mockResolvedValue(text) });
    return selected;
}

describe('AdminCatalogFacade transfer recovery', () => {
    const api = {
        exportCatalog: vi.fn(),
        preview: vi.fn(),
        importItem: vi.fn(),
    };
    let facade: AdminCatalogFacade;

    beforeEach(() => {
        vi.resetAllMocks();
        api.exportCatalog.mockReturnValue(of({ products: [], recipes: [] }));
        api.preview.mockImplementation((_kind: string, item: CatalogRecipe) => of({ id: item.id, status: 'ready', errors: [] }));
        api.importItem.mockImplementation((_kind: string, item: CatalogRecipe) => of({ id: item.id, status: 'imported', errors: [] }));
        TestBed.configureTestingModule({ providers: [AdminCatalogFacade, { provide: AdminCatalogService, useValue: api }] });
        facade = TestBed.inject(AdminCatalogFacade);
    });

    it('imports dependencies first and retries only the failed remainder', async () => {
        await facade.previewFileAsync(file([recipe(firstId, secondId), recipe(secondId)]));
        api.importItem
            .mockReturnValueOnce(of({ id: secondId, status: 'imported', errors: [] }))
            .mockReturnValueOnce(throwError(() => new Error('Offline')));
        await facade.importAsync();
        expect(api.importItem.mock.calls.map(call => (call[1] as CatalogRecipe).id)).toEqual([secondId, firstId]);
        expect(facade.report().map(row => row.status)).toEqual(['failed', 'imported']);
        expect(facade.error()).toBe('ADMIN_CATALOG_TRANSFER.IMPORT_STOPPED');
        expect(facade.canImport()).toBe(true);
        await facade.importAsync();
        expect(api.importItem.mock.calls.map(call => (call[1] as CatalogRecipe).id)).toEqual([secondId, firstId, firstId]);
        expect(facade.total()).toBe(1);
        expect(facade.completed()).toBe(1);
        expect(facade.canImport()).toBe(false);
        expect(facade.error()).toBeNull();
        expect(facade.busy()).toBe(false);
    });

    it('rejects unresolved dependencies before any preview or import request', async () => {
        await facade.previewFileAsync(file([recipe(firstId, secondId)]));
        expect(facade.error()).toBe('ADMIN_CATALOG_TRANSFER.RECIPE_LINKS');
        expect(api.preview).not.toHaveBeenCalled();
        await facade.importAsync();
        expect(api.importItem).not.toHaveBeenCalled();
        expect(facade.busy()).toBe(false);
    });

    it('clears an earlier valid preview when a replacement file fails', async () => {
        await facade.previewFileAsync(file([recipe(firstId)]));
        expect(facade.canImport()).toBe(true);
        const oversized = new File([], 'large.json');
        Object.defineProperty(oversized, 'size', { value: 5_242_881 });
        await facade.previewFileAsync(oversized);
        expect(facade.error()).toBe('ADMIN_CATALOG_TRANSFER.FILE_TOO_LARGE');
        expect(facade.report()).toEqual([]);
        expect(facade.canImport()).toBe(false);
        await facade.importAsync();
        expect(api.importItem).not.toHaveBeenCalled();
    });
});
