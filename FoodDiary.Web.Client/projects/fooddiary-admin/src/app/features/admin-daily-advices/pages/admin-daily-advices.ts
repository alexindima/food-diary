import { DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiPaginationComponent } from 'fd-ui-kit';

import { AdminLoadErrorComponent } from '../../../shared/feedback/admin-load-error';
import { adminPageWithinTotal, restoreAdminPage } from '../../../shared/period/admin-pagination';
import { adminPage } from '../../../shared/period/admin-query';
import { DailyAdviceGroupCardComponent } from '../components/daily-advice-group-card';
import { AdminDailyAdvicesFacade } from '../lib/admin-daily-advices.facade';
import { DAILY_ADVICE_IMPORT_EXAMPLE } from '../lib/daily-advice-import';

@Component({
    selector: 'fd-admin-daily-advices',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiPaginationComponent, AdminLoadErrorComponent, DailyAdviceGroupCardComponent],
    providers: [AdminDailyAdvicesFacade],
    templateUrl: './admin-daily-advices.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminDailyAdvicesComponent {
    private readonly facade = inject(AdminDailyAdvicesFacade);
    private readonly document = inject(DOCUMENT);
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly requestedPage = signal(0);
    protected readonly advices = this.facade.advices;
    protected readonly loading = this.facade.loading;
    protected readonly loadFailed = this.facade.loadFailed;
    protected readonly importing = this.facade.importing;
    protected readonly importError = this.facade.importError;
    protected readonly importResult = this.facade.importResult;
    protected readonly pageSize = 25;
    protected readonly page = computed(() => adminPageWithinTotal(this.requestedPage() + 1, this.advices().length, this.pageSize) - 1);
    protected readonly pageItems = computed(() => this.advices().slice(this.page() * this.pageSize, (this.page() + 1) * this.pageSize));

    public constructor() {
        this.route.queryParamMap.pipe(takeUntilDestroyed()).subscribe(params => {
            this.requestedPage.set(adminPage(params.get('page')) - 1);
        });
        effect(() => {
            if (!this.loading() && !this.loadFailed()) {
                restoreAdminPage(this.router, this.route, this.page() + 1, { totalItems: this.advices().length, pageSize: this.pageSize });
            }
        });
        this.loadAdvices();
    }

    protected goToPage(page: number): void {
        void this.router.navigate([], { relativeTo: this.route, queryParams: { page: page + 1 }, queryParamsHandling: 'merge' });
    }

    protected loadAdvices(): void {
        this.facade.loadAdvices();
    }

    protected downloadExample(): void {
        const url = URL.createObjectURL(new Blob([JSON.stringify(DAILY_ADVICE_IMPORT_EXAMPLE, null, 2)], { type: 'application/json' }));
        const anchor = this.document.createElement('a');
        anchor.href = url;
        anchor.download = 'fooddiary-daily-advices.example.json';
        anchor.click();
        URL.revokeObjectURL(url);
    }

    protected async importFileAsync(event: Event): Promise<void> {
        const input = event.target instanceof HTMLInputElement ? event.target : null;
        const file = input?.files?.[0];
        if (input === null || file === undefined) {
            return;
        }
        input.value = '';
        await this.facade.importFileAsync(file);
    }
}
