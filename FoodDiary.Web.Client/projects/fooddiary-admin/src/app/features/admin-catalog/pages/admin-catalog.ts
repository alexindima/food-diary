import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, PLATFORM_ID, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiPaginationComponent } from 'fd-ui-kit';
import { firstValueFrom, from, mergeMap, toArray } from 'rxjs';

import { AdminCatalogFacade } from '../lib/admin-catalog.facade';
import { MAX_CATALOG_FILE_BYTES, orderCatalogRecipes, parseCatalogFile } from '../lib/catalog-file';
import type { CatalogFile, CatalogKind, CatalogProduct, CatalogRecipe, CatalogReportRow } from '../models/catalog-transfer';

const PREVIEW_CONCURRENCY = 8;

@Component({
    selector: 'fd-admin-catalog',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiPaginationComponent],
    templateUrl: './admin-catalog.html',
    providers: [AdminCatalogFacade],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminCatalogComponent {
    private readonly api = inject(AdminCatalogFacade);
    private readonly destroyRef = inject(DestroyRef);
    private readonly document = inject(DOCUMENT);
    private readonly platformId = inject(PLATFORM_ID);
    private file: CatalogFile | null = null;
    private orderedRecipes: CatalogRecipe[] = [];

    protected readonly busy = signal(false);
    protected readonly error = signal<string | null>(null);
    protected readonly report = signal<CatalogReportRow[]>([]);
    protected readonly completed = signal(0);
    protected readonly total = signal(0);
    protected readonly page = signal(0);
    protected readonly pageSize = 20;
    protected readonly rows = computed(() => this.report().slice(this.page() * this.pageSize, (this.page() + 1) * this.pageSize));
    protected readonly canImport = computed(
        () =>
            !this.busy() &&
            this.report().length > 0 &&
            !this.report().some(row => row.status === 'invalid') &&
            this.report().some(row => row.status === 'ready' || row.status === 'failed'),
    );
    protected readonly counts = computed(() => ({
        ready: this.report().filter(row => row.status === 'ready').length,
        skipped: this.report().filter(row => row.status === 'skipped').length,
        imported: this.report().filter(row => row.status === 'imported').length,
        invalid: this.report().filter(row => row.status === 'invalid' || row.status === 'failed').length,
    }));

    protected async exportAsync(): Promise<void> {
        this.busy.set(true);
        this.error.set(null);
        try {
            const catalog = await firstValueFrom(this.api.exportCatalog().pipe(takeUntilDestroyed(this.destroyRef)));
            this.download({ format: 'fooddiary-catalog', version: 1, ...catalog }, 'fooddiary-catalog.json');
        } catch {
            this.error.set('ADMIN_CATALOG_TRANSFER.EXPORT_FAILED');
        } finally {
            this.busy.set(false);
        }
    }

    protected async previewFileAsync(event: Event): Promise<void> {
        const input = event.target;
        if (!(input instanceof HTMLInputElement) || this.busy()) {
            return;
        }
        const selected = input.files?.[0];
        input.value = '';
        if (selected === undefined) {
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
            const existing = await firstValueFrom(this.api.exportCatalog().pipe(takeUntilDestroyed(this.destroyRef)));
            this.orderedRecipes = orderCatalogRecipes(file.recipes, new Set(existing.recipes.map(item => item.id)));
            const productIds = new Set([...existing.products, ...file.products].map(item => item.id));
            const newRecipes = file.recipes.filter(item => !existing.recipes.some(current => current.id === item.id));
            if (
                newRecipes.some(item =>
                    item.steps.some(step =>
                        step.ingredients.some(ingredient => ingredient.productId !== null && !productIds.has(ingredient.productId)),
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
                        const result = await firstValueFrom(this.api.preview(kind, item).pipe(takeUntilDestroyed(this.destroyRef)));
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

    protected async importAsync(): Promise<void> {
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
                    const result = await firstValueFrom(this.api.importItem(kind, item).pipe(takeUntilDestroyed(this.destroyRef)));
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

    protected downloadReport(): void {
        this.download({ generatedAt: new Date().toISOString(), rows: this.report() }, 'fooddiary-catalog-report.json');
    }

    private resetPreview(): void {
        this.file = null;
        this.orderedRecipes = [];
        this.report.set([]);
        this.error.set(null);
        this.page.set(0);
        this.completed.set(0);
        this.total.set(0);
    }

    private replaceRow(next: CatalogReportRow): void {
        this.report.update(rows => rows.map(row => (row.id === next.id && row.kind === next.kind ? next : row)));
    }

    private download(value: unknown, fileName: string): void {
        if (!isPlatformBrowser(this.platformId)) {
            return;
        }
        const url = URL.createObjectURL(new Blob([JSON.stringify(value, null, 2)], { type: 'application/json;charset=utf-8' }));
        const anchor = this.document.createElement('a');
        anchor.href = url;
        anchor.download = fileName;
        anchor.click();
        URL.revokeObjectURL(url);
    }
}
