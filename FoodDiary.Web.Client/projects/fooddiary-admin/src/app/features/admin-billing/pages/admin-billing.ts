import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiTabsComponent } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination';

import { adminPeriod } from '../../../shared/period/admin-period';
import { AdminPeriodControlComponent } from '../../../shared/period/admin-period-control';
import { adminPage } from '../../../shared/period/admin-query';
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
    ],
    templateUrl: './admin-billing.html',
    styleUrl: './admin-billing.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminBillingComponent {
    protected readonly billing = inject(AdminBillingFacade);
    private readonly route = inject(ActivatedRoute);

    protected onTabChange(value: string): void {
        if (value === 'subscriptions' || value === 'payments' || value === 'webhook-events' || value === 'cancellations') {
            this.billing.setTab(value);
        }
    }

    public constructor() {
        this.route.queryParamMap.pipe(takeUntilDestroyed()).subscribe(params => {
            this.billing.search.set(params.get('search') ?? '');
            this.billing.provider.set(params.get('provider') ?? '');
            this.billing.status.set(params.get('status') ?? '');
            this.billing.kind.set(params.get('kind') ?? '');
            const tab = params.get('tab');
            this.billing.activeTab.set(tab === 'payments' || tab === 'webhook-events' || tab === 'cancellations' ? tab : 'subscriptions');
            this.billing.page.set(adminPage(params.get('page')));
            const range = adminPeriod(params);
            if (range === null) {
                this.billing.clearInvalidPeriod();
                return;
            }
            this.billing.setPeriod(range);
            this.billing.load();
        });
    }
}
