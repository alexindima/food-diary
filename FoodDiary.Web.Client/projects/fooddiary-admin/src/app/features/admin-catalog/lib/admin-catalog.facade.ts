import { computed, DestroyRef, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { firstValueFrom, from, mergeMap, type Observable, toArray } from 'rxjs';

import { AdminCatalogService } from '../api/admin-catalog.service';
import { catalogProductReference, decodeCatalogIngredientSource } from '../models/catalog-ingredient-source';
import type { CatalogFile, CatalogKind, CatalogProduct, CatalogRecipe, CatalogReportRow, CatalogResult } from '../models/catalog-transfer';
import { MAX_CATALOG_FILE_BYTES, orderCatalogRecipes, parseCatalogFile } from './catalog-file';

const PREVIEW_CONCURRENCY = 8;

@Injectable()
export class AdminCatalogFacade {
    private readonly api = inject(AdminCatalogService);
    private readonly destroyRef = inject(DestroyRef);
    private file: CatalogFile | null = null;
    private orderedRecipes: CatalogRecipe[] = [];

    public readonly busy = signal(false);
    public readonly error = signal<string | null>(null);
    public readonly report = signal<CatalogReportRow[]>([]);
    public readonly completed = signal(0);
    public readonly total = signal(0);
    public readonly canImport = computed(
        () =>
            !this.busy() &&
            this.report().length > 0 &&
            !this.report().some(row => row.status === 'invalid') &&
            this.report().some(row => row.status === 'ready' || row.status === 'failed'),
    );

    public exportCatalog(): Observable<{ products: CatalogProduct[]; recipes: CatalogRecipe[] }> {
        return this.api.exportCatalog();
    }

    public preview(kind: CatalogKind, item: CatalogProduct | CatalogRecipe): Observable<CatalogResult> {
        return this.api.preview(kind, item);
    }

    public importItem(kind: CatalogKind, item: CatalogProduct | CatalogRecipe): Observable<CatalogResult> {
        return this.api.importItem(kind, item);
    }

    public async previewFileAsync(selected: File): Promise<void> {
        if (this.busy()) {
            return;
        }
        this.resetPreview();
        this.busy.set(true);
        try {
            if (selected.size > MAX_CATALOG_FILE_BYTES) {
                this.error.set('ADMIN_CATALOG_TRANSFER.FILE_TOO_LARGE');
                return;
            }
            const file = parseCatalogFile(await selected.text());
            const existing = await firstValueFrom(this.exportCatalog().pipe(takeUntilDestroyed(this.destroyRef)));
            this.orderedRecipes = orderCatalogRecipes(file.recipes, new Set(existing.recipes.map(item => item.id)));
            const productIds = new Set([...existing.products, ...file.products].map(item => item.id));
            const newRecipes = file.recipes.filter(item => !existing.recipes.some(current => current.id === item.id));
            if (
                newRecipes.some(item =>
                    item.steps.some(step =>
                        step.ingredients.some(ingredient => {
                            const productId = catalogProductReference(decodeCatalogIngredientSource(ingredient));
                            return productId !== null && !productIds.has(productId);
                        }),
                    ),
                )
            ) {
                this.error.set('ADMIN_CATALOG_TRANSFER.PRODUCT_LINKS');
                return;
            }
            const items = [
                ...file.products.map(item => ({ kind: 'products' as const, item })),
                ...file.recipes.map(item => ({ kind: 'recipes' as const, item })),
            ];
            this.total.set(items.length);
            const rows = await firstValueFrom(
                from(items).pipe(
                    mergeMap(async ({ kind, item }) => {
                        const result = await firstValueFrom(this.preview(kind, item).pipe(takeUntilDestroyed(this.destroyRef)));
                        this.completed.update(value => value + 1);
                        return { ...result, kind, name: item.name };
                    }, PREVIEW_CONCURRENCY),
                    toArray(),
                ),
            );
            this.file = file;
            this.report.set(rows);
        } catch (error: unknown) {
            this.error.set(
                error instanceof Error && error.message.startsWith('ADMIN_CATALOG_TRANSFER.')
                    ? error.message
                    : 'ADMIN_CATALOG_TRANSFER.PREVIEW_FAILED',
            );
        } finally {
            this.busy.set(false);
        }
    }

    public async importAsync(): Promise<void> {
        if (this.file === null || !this.canImport()) {
            return;
        }
        this.busy.set(true);
        this.error.set(null);
        this.completed.set(0);
        const items: Array<{ kind: CatalogKind; item: CatalogProduct | CatalogRecipe }> = [
            ...this.file.products.map(item => ({ kind: 'products' as const, item })),
            ...this.orderedRecipes.map(item => ({ kind: 'recipes' as const, item })),
        ];
        const pending = items.filter(({ kind, item }) =>
            this.report().some(row => row.kind === kind && row.id === item.id && (row.status === 'ready' || row.status === 'failed')),
        );
        this.total.set(pending.length);
        try {
            for (const { kind, item } of pending) {
                try {
                    const result = await firstValueFrom(this.importItem(kind, item).pipe(takeUntilDestroyed(this.destroyRef)));
                    this.replaceRow({ ...result, kind, name: item.name });
                    this.completed.update(value => value + 1);
                    if (result.status === 'invalid') {
                        this.error.set('ADMIN_CATALOG_TRANSFER.IMPORT_STOPPED');
                        break;
                    }
                } catch {
                    this.replaceRow({
                        id: item.id,
                        kind,
                        name: item.name,
                        status: 'failed',
                        errors: ['ADMIN_CATALOG_TRANSFER.ITEM_FAILED'],
                    });
                    this.error.set('ADMIN_CATALOG_TRANSFER.IMPORT_STOPPED');
                    break;
                }
            }
        } finally {
            this.busy.set(false);
        }
    }

    private resetPreview(): void {
        this.file = null;
        this.orderedRecipes = [];
        this.report.set([]);
        this.error.set(null);
        this.completed.set(0);
        this.total.set(0);
    }

    private replaceRow(next: CatalogReportRow): void {
        this.report.update(rows => rows.map(row => (row.id === next.id && row.kind === next.kind ? next : row)));
    }
}
