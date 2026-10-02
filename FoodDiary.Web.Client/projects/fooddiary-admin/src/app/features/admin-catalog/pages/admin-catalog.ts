import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, PLATFORM_ID, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiPaginationComponent } from 'fd-ui-kit';
import { firstValueFrom } from 'rxjs';

import { AdminCatalogFacade } from '../lib/admin-catalog.facade';

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

    protected readonly busy = this.api.busy;
    protected readonly error = this.api.error;
    protected readonly report = this.api.report;
    protected readonly completed = this.api.completed;
    protected readonly total = this.api.total;
    protected readonly page = signal(0);
    protected readonly pageSize = 20;
    protected readonly rows = computed(() => this.report().slice(this.page() * this.pageSize, (this.page() + 1) * this.pageSize));
    protected readonly canImport = this.api.canImport;
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
        if (selected !== undefined) {
            this.page.set(0);
            await this.api.previewFileAsync(selected);
        }
    }

    protected async importAsync(): Promise<void> {
        await this.api.importAsync();
    }

    protected downloadReport(): void {
        this.download({ generatedAt: new Date().toISOString(), rows: this.report() }, 'fooddiary-catalog-report.json');
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
