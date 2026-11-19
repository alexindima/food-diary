import { PLATFORM_ID } from '@angular/core';
import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { AdminCatalogService } from '../api/admin-catalog.service';
import type { CatalogFile, CatalogProduct, CatalogRecipe, CatalogResult } from '../models/catalog-transfer';
import { AdminCatalogComponent } from './admin-catalog';

describe('AdminCatalog export concurrency and recovery', () => {
    let fixture: ComponentFixture<AdminCatalogComponent>;
    const exportCatalog = vi.fn();

    beforeEach(async () => {
        exportCatalog.mockReset();
        await TestBed.configureTestingModule({
            imports: [AdminCatalogComponent],
            providers: [...provideTranslateTesting(), { provide: AdminCatalogService, useValue: { exportCatalog } }],
        }).compileComponents();
        fixture = TestBed.createComponent(AdminCatalogComponent);
        fixture.detectChanges();
    });

    it('ignores a second click before disabled state renders and permits retry after an error', async () => {
        const pending = new Subject<{ products: CatalogProduct[]; recipes: CatalogRecipe[] }>();
        exportCatalog.mockReturnValueOnce(pending).mockReturnValueOnce(throwError(() => new Error('Retry unavailable')));
        const element = fixture.nativeElement as HTMLElement;
        const button = element.querySelector<HTMLButtonElement>('fd-ui-button button');
        if (button === null) {
            throw new Error('Export button is missing');
        }

        button.click();
        button.click();
        expect(exportCatalog).toHaveBeenCalledTimes(1);
        fixture.detectChanges();
        expect(button.disabled).toBe(true);

        pending.error(new Error('Export unavailable'));
        await fixture.whenStable();
        fixture.detectChanges();
        expect(button.disabled).toBe(false);
        expect(element.querySelector('[role=alert]')?.textContent).toContain('ADMIN_CATALOG_TRANSFER.EXPORT_FAILED');

        button.click();
        await fixture.whenStable();
        fixture.detectChanges();
        expect(exportCatalog).toHaveBeenCalledTimes(2);
        expect(button.disabled).toBe(false);
        expect(element.querySelector('[role=alert]')?.textContent).toContain('ADMIN_CATALOG_TRANSFER.EXPORT_FAILED');
    });
});

const catalogProduct: CatalogProduct = {
    id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
    name: 'Овёс',
    barcode: null,
    brand: null,
    productType: 'Standard',
    category: null,
    description: null,
    imageUrl: null,
    baseUnit: 'Grams',
    baseAmount: 100,
    defaultPortionAmount: 50,
    caloriesPerBase: 380,
    proteinsPerBase: 13,
    fatsPerBase: 7,
    carbsPerBase: 68,
    fiberPerBase: 10,
    alcoholPerBase: 0,
};
const catalogFile: CatalogFile = { format: 'fooddiary-catalog', version: 1, products: [catalogProduct], recipes: [] };
const baseRecipe: CatalogRecipe = {
    id: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
    name: 'Porridge',
    description: null,
    category: null,
    imageUrl: null,
    prepTime: null,
    cookTime: null,
    servings: 2,
    language: 'en',
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
            description: 'Cook the oats',
            title: null,
            imageUrl: null,
            ingredients: [{ productId: catalogProduct.id, nestedRecipeId: null, amount: 50 }],
        },
    ],
};

function catalogButton(root: HTMLElement, suffix: string): HTMLButtonElement {
    const result = Array.from(root.querySelectorAll('button')).find(item => item.textContent.trim() === `ADMIN_CATALOG_TRANSFER.${suffix}`);
    if (result === undefined) {
        throw new Error(`Missing catalog button ${suffix}`);
    }
    return result;
}

function chooseFile(root: HTMLElement, contents: string | null): void {
    const input = root.querySelector<HTMLInputElement>('#catalog-file');
    if (input === null) {
        throw new Error('Missing catalog file input');
    }
    const files: File[] = [];
    if (contents !== null) {
        const file = new File([contents], 'catalog.json', { type: 'application/json' });
        Object.defineProperty(file, 'text', { value: vi.fn<() => Promise<string>>().mockResolvedValue(contents) });
        files.push(file);
    }
    Object.defineProperty(input, 'files', { configurable: true, value: files });
    input.dispatchEvent(new Event('change', { bubbles: true }));
}

async function readFileAsync(blob: Blob | undefined): Promise<unknown> {
    if (blob === undefined) {
        throw new Error('No catalog download was created');
    }
    const contents = await new Promise<string>((resolve, reject) => {
        const reader = new FileReader();
        reader.addEventListener('load', () => {
            if (typeof reader.result === 'string') {
                resolve(reader.result);
            } else {
                reject(new Error('Could not read text from downloaded catalog'));
            }
        });
        reader.addEventListener('error', () => {
            reject(new Error('Could not read downloaded catalog'));
        });
        reader.readAsText(blob);
    });
    return JSON.parse(contents) as unknown;
}

async function setupFileTransferAsync(platform = 'browser'): Promise<{
    fixture: ComponentFixture<AdminCatalogComponent>;
    root: HTMLElement;
    service: {
        exportCatalog: ReturnType<typeof vi.fn<AdminCatalogService['exportCatalog']>>;
        preview: ReturnType<typeof vi.fn<AdminCatalogService['preview']>>;
        importItem: ReturnType<typeof vi.fn<AdminCatalogService['importItem']>>;
    };
}> {
    const service = {
        exportCatalog: vi.fn<AdminCatalogService['exportCatalog']>().mockReturnValue(of({ products: [], recipes: [] })),
        preview: vi.fn<AdminCatalogService['preview']>().mockReturnValue(of({ id: catalogProduct.id, status: 'ready', errors: [] })),
        importItem: vi
            .fn<AdminCatalogService['importItem']>()
            .mockReturnValue(of({ id: catalogProduct.id, status: 'imported', errors: [] })),
    };
    await TestBed.configureTestingModule({
        imports: [AdminCatalogComponent],
        providers: [
            ...provideTranslateTesting(),
            { provide: AdminCatalogService, useValue: service },
            { provide: PLATFORM_ID, useValue: platform },
        ],
    }).compileComponents();
    const fixture = TestBed.createComponent(AdminCatalogComponent);
    fixture.detectChanges();
    return { fixture, root: fixture.nativeElement as HTMLElement, service };
}

describe('AdminCatalog import and file downloads', () => {
    const blobUrl = 'blob:catalog-test';

    afterEach(() => {
        vi.restoreAllMocks();
    });

    it('requires explicit confirmation after file preview and downloads the completed import report', async () => {
        const { fixture, root, service } = await setupFileTransferAsync();
        const objectUrl = vi.spyOn(URL, 'createObjectURL').mockReturnValue(blobUrl);
        const revoke = vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => {});
        const download = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => {});
        const pending = new Subject<CatalogResult>();
        service.importItem.mockReturnValueOnce(pending);
        chooseFile(root, JSON.stringify(catalogFile));
        await fixture.whenStable();
        fixture.detectChanges();
        expect(service.preview).toHaveBeenCalledExactlyOnceWith('products', catalogProduct);
        expect(service.importItem).not.toHaveBeenCalled();
        expect(root.querySelector('article')?.textContent).toContain('ADMIN_CATALOG_TRANSFER.STATUS.ready');

        catalogButton(root, 'CONFIRM').click();
        fixture.detectChanges();
        expect(catalogButton(root, 'CONFIRM').disabled).toBe(true);
        expect(catalogButton(root, 'REPORT').disabled).toBe(true);
        expect(service.importItem).toHaveBeenCalledExactlyOnceWith('products', catalogProduct);
        pending.next({ id: catalogProduct.id, status: 'imported', errors: [] });
        await fixture.whenStable();
        fixture.detectChanges();

        expect(root.querySelector('article strong')?.textContent).toBe(catalogProduct.name);
        expect(root.querySelector('article')?.textContent).toContain('ADMIN_CATALOG_TRANSFER.STATUS.imported');
        expect(catalogButton(root, 'CONFIRM').disabled).toBe(true);
        catalogButton(root, 'REPORT').click();
        const anchor = download.mock.contexts[0];
        expect(anchor).toMatchObject({ download: 'fooddiary-catalog-report.json', href: blobUrl });
        expect(revoke).toHaveBeenCalledExactlyOnceWith(blobUrl);
        const blob = objectUrl.mock.calls[0]?.[0];
        expect(blob).toBeInstanceOf(Blob);
        const report = await readFileAsync(blob instanceof Blob ? blob : undefined);
        expect(report).toMatchObject({
            rows: [{ id: catalogProduct.id, name: catalogProduct.name, kind: 'products', status: 'imported', errors: [] }],
        });
    });

    it('exports a reusable versioned catalog containing the products returned by the service', async () => {
        const { fixture, root, service } = await setupFileTransferAsync();
        const objectUrl = vi.spyOn(URL, 'createObjectURL').mockReturnValue(blobUrl);
        const revoke = vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => {});
        const download = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => {});
        service.exportCatalog.mockReturnValueOnce(of({ products: [catalogProduct], recipes: [] }));

        catalogButton(root, 'EXPORT').click();
        await fixture.whenStable();
        fixture.detectChanges();

        expect(service.exportCatalog).toHaveBeenCalledTimes(1);
        expect(root.querySelector('[role="alert"]')).toBeNull();
        expect(catalogButton(root, 'EXPORT').disabled).toBe(false);
        expect(download.mock.contexts[0]).toMatchObject({ download: 'fooddiary-catalog.json', href: blobUrl });
        expect(revoke).toHaveBeenCalledExactlyOnceWith(blobUrl);
        const blob = objectUrl.mock.calls[0]?.[0];
        expect(await readFileAsync(blob instanceof Blob ? blob : undefined)).toEqual(catalogFile);
    });
});

describe('Catalog file picker and browser lifecycle', () => {
    it('retains the first file when another selection arrives early and keeps its preview after cancellation', async () => {
        const { fixture, root, service } = await setupFileTransferAsync();
        const pending = new Subject<CatalogResult>();
        service.preview.mockReturnValueOnce(pending);
        chooseFile(root, JSON.stringify(catalogFile));
        chooseFile(root, 'obsolete invalid file');
        await vi.waitFor(() => {
            expect(service.preview).toHaveBeenCalledExactlyOnceWith('products', catalogProduct);
        });
        pending.next({ id: catalogProduct.id, status: 'ready', errors: [] });
        pending.complete();
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(root.querySelector('article strong')?.textContent).toBe(catalogProduct.name);
        });
        chooseFile(root, null);
        await fixture.whenStable();
        fixture.detectChanges();
        expect(root.querySelector('article strong')?.textContent).toBe(catalogProduct.name);
        expect(catalogButton(root, 'CONFIRM').disabled).toBe(false);
        expect(root.querySelector('[role="alert"]')).toBeNull();
        expect(service.exportCatalog).toHaveBeenCalledOnce();
    });

    it('does not create file URLs when export runs without a browser platform', async () => {
        const { fixture, root, service } = await setupFileTransferAsync('server');
        const objectUrl = vi.spyOn(URL, 'createObjectURL');
        try {
            catalogButton(root, 'EXPORT').click();
            await fixture.whenStable();
            fixture.detectChanges();
            expect(service.exportCatalog).toHaveBeenCalledOnce();
            expect(objectUrl).not.toHaveBeenCalled();
            expect(catalogButton(root, 'EXPORT').disabled).toBe(false);
        } finally {
            objectUrl.mockRestore();
        }
    });
});

describe('AdminCatalog file transfer failures', () => {
    it('blocks recipe preview when an ingredient product is absent from both catalogs', async () => {
        const { fixture, root, service } = await setupFileTransferAsync();
        chooseFile(root, JSON.stringify({ ...catalogFile, products: [], recipes: [baseRecipe] }));
        await fixture.whenStable();
        fixture.detectChanges();

        expect(service.exportCatalog).toHaveBeenCalledOnce();
        expect(root.querySelector('[role="alert"]')?.textContent).toContain('ADMIN_CATALOG_TRANSFER.PRODUCT_LINKS');
        expect(root.querySelector('article')).toBeNull();
        expect(service.preview).not.toHaveBeenCalled();
        expect(service.importItem).not.toHaveBeenCalled();
    });

    it('orders recipe dependencies and stops before importing a dependent recipe when an earlier row is invalid', async () => {
        const { fixture, root, service } = await setupFileTransferAsync();
        const dependentRecipe: CatalogRecipe = {
            ...baseRecipe,
            id: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
            name: 'Fruit porridge',
            steps: [
                {
                    ...baseRecipe.steps[0],
                    ingredients: [{ productId: null, nestedRecipeId: baseRecipe.id, amount: 1 }],
                },
            ],
        };
        service.exportCatalog.mockReturnValueOnce(of({ products: [catalogProduct], recipes: [] }));
        service.preview.mockImplementation((_kind, item) => of({ id: item.id, status: 'ready', errors: [] }));
        service.importItem.mockReturnValueOnce(of({ id: baseRecipe.id, status: 'invalid', errors: ['Ingredient no longer available'] }));
        chooseFile(root, JSON.stringify({ ...catalogFile, products: [], recipes: [dependentRecipe, baseRecipe] }));
        await fixture.whenStable();
        fixture.detectChanges();
        expect(catalogButton(root, 'CONFIRM').disabled).toBe(false);

        catalogButton(root, 'CONFIRM').click();
        await fixture.whenStable();
        fixture.detectChanges();

        expect(service.importItem).toHaveBeenCalledExactlyOnceWith('recipes', baseRecipe);
        expect(root.querySelector('[role="alert"]')?.textContent).toContain('ADMIN_CATALOG_TRANSFER.IMPORT_STOPPED');
        expect(root.textContent).toContain('Ingredient no longer available');
        expect(catalogButton(root, 'CONFIRM').disabled).toBe(true);
    });

    it('renders a preview service failure and starts over with the next selected file', async () => {
        const { fixture, root, service } = await setupFileTransferAsync();
        service.preview.mockReturnValueOnce(throwError(() => new Error('Preview unavailable')));
        chooseFile(root, JSON.stringify(catalogFile));
        await fixture.whenStable();
        fixture.detectChanges();
        expect(root.querySelector('[role="alert"]')?.textContent).toContain('ADMIN_CATALOG_TRANSFER.PREVIEW_FAILED');
        expect(root.querySelector('article')).toBeNull();
        expect(service.importItem).not.toHaveBeenCalled();

        chooseFile(root, JSON.stringify(catalogFile));
        await fixture.whenStable();
        fixture.detectChanges();
        expect(root.querySelector('[role="alert"]')).toBeNull();
        expect(root.querySelector('article strong')?.textContent).toBe(catalogProduct.name);
        expect(catalogButton(root, 'CONFIRM').disabled).toBe(false);
    });
});
