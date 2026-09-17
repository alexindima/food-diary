import { DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiCardComponent, FdUiPaginationComponent } from 'fd-ui-kit';

import { AdminLoadErrorComponent } from '../../../shared/feedback/admin-load-error';
import { AdminDailyAdvicesFacade } from '../lib/admin-daily-advices.facade';
import { DAILY_ADVICE_IMPORT_EXAMPLE } from '../lib/daily-advice-import';

@Component({
    selector: 'fd-admin-daily-advices',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiCardComponent, FdUiPaginationComponent, AdminLoadErrorComponent],
    providers: [AdminDailyAdvicesFacade],
    templateUrl: './admin-daily-advices.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminDailyAdvicesComponent {
    private readonly facade = inject(AdminDailyAdvicesFacade);
    private readonly document = inject(DOCUMENT);
    protected readonly advices = this.facade.advices;
    protected readonly loading = this.facade.loading;
    protected readonly loadFailed = this.facade.loadFailed;
    protected readonly importing = this.facade.importing;
    protected readonly importError = this.facade.importError;
    protected readonly importResult = this.facade.importResult;
    protected readonly page = this.facade.page;
    protected readonly pageSize = 25;
    protected readonly pageItems = computed(() => this.advices().slice(this.page() * this.pageSize, (this.page() + 1) * this.pageSize));

    public constructor() {
        this.loadAdvices();
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
