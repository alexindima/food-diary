import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, effect, inject, input, untracked } from '@angular/core';
import { convertToParamMap } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiTabsComponent } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination';

import { AdminLoadErrorComponent } from '../../../shared/feedback/admin-load-error';
import { adminPeriod } from '../../../shared/period/admin-period';
import { AdminPeriodControlComponent } from '../../../shared/period/admin-period-control';
import { adminPage, firstAdminQueryValue } from '../../../shared/period/admin-query';
import { AdminBillingFacade } from '../lib/admin-billing.facade';
import { AdminBillingFiltersComponent } from './admin-billing-filters';
import { AdminBillingMetadataPanelComponent } from './admin-billing-metadata-panel';
import { AdminBillingRecordsComponent } from './admin-billing-records';

@Component({
    selector: 'fd-admin-billing',
    imports: [
        FdUiTabsComponent,
        AdminBillingRecordsComponent,
        CommonModule,
        AdminPeriodControlComponent,
        TranslatePipe,
        FdUiButtonComponent,
        FdUiPaginationComponent,
        AdminBillingFiltersComponent,
        AdminBillingMetadataPanelComponent,
        AdminLoadErrorComponent,
    ],
    providers: [AdminBillingFacade],
    templateUrl: './admin-billing.html',
    styleUrl: './admin-billing.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminBillingComponent {
    public readonly search = input(undefined, { transform: firstAdminQueryValue });
    public readonly provider = input(undefined, { transform: firstAdminQueryValue });
    public readonly status = input(undefined, { transform: firstAdminQueryValue });
    public readonly kind = input(undefined, { transform: firstAdminQueryValue });
    public readonly tab = input(undefined, { transform: firstAdminQueryValue });
    public readonly page = input(undefined, { transform: firstAdminQueryValue });
    public readonly period = input(undefined, { transform: firstAdminQueryValue });
    public readonly from = input(undefined, { transform: firstAdminQueryValue });
    public readonly to = input(undefined, { transform: firstAdminQueryValue });

    protected readonly billing = inject(AdminBillingFacade);
    private readonly appliedFilters = computed(() => ({
        search: this.search() ?? '',
        provider: this.provider() ?? '',
        status: this.status() ?? '',
        kind: this.kind() ?? '',
        tab: this.tab(),
        page: adminPage(this.page() ?? null),
        period: this.period() ?? null,
        from: this.from() ?? null,
        to: this.to() ?? null,
    }));

    protected onTabChange(value: string): void {
        if (value === 'subscriptions' || value === 'payments' || value === 'webhook-events' || value === 'cancellations') {
            this.billing.setTab(value);
        }
    }

    protected loadPeriod(): void {
        const { period, from, to } = this.appliedFilters();
        const range = adminPeriod(convertToParamMap({ period, from, to }));
        if (range === null) {
            this.billing.clearInvalidPeriod();
            return;
        }
        this.billing.setPeriod(range);
        this.billing.load();
    }

    public constructor() {
        effect(() => {
            const { search, provider, status, kind, tab, page } = this.appliedFilters();
            untracked(() => {
                this.billing.search.set(search);
                this.billing.provider.set(provider);
                this.billing.status.set(status);
                this.billing.kind.set(kind);
                this.billing.activeTab.set(
                    tab === 'payments' || tab === 'webhook-events' || tab === 'cancellations' ? tab : 'subscriptions',
                );
                this.billing.page.set(page);
                this.loadPeriod();
            });
        });
    }
}
