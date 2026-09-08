import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination';

import { adminPeriod } from '../../../shared/period/admin-period';
import { AdminPeriodControlComponent } from '../../../shared/period/admin-period-control';
import { adminPage } from '../../../shared/period/admin-query';
import { AdminBillingFacade } from '../lib/admin-billing.facade';
import { AdminBillingFiltersComponent } from './admin-billing-filters';
import { AdminBillingMetadataPanelComponent } from './admin-billing-metadata-panel';
import { AdminBillingPaymentsTableComponent } from './admin-billing-payments-table';
import { AdminBillingSubscriptionsTableComponent } from './admin-billing-subscriptions-table';
import { AdminBillingWebhooksTableComponent } from './admin-billing-webhooks-table';

@Component({
    selector: 'fd-admin-billing',
    imports: [
        CommonModule,
        AdminPeriodControlComponent,
        TranslatePipe,
        FdUiButtonComponent,
        FdUiPaginationComponent,
        AdminBillingFiltersComponent,
        AdminBillingSubscriptionsTableComponent,
        AdminBillingPaymentsTableComponent,
        AdminBillingWebhooksTableComponent,
        AdminBillingMetadataPanelComponent,
    ],
    templateUrl: './admin-billing.html',
    styleUrl: './admin-billing.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminBillingComponent {
    protected readonly billing = inject(AdminBillingFacade);
    private readonly route = inject(ActivatedRoute);

    public constructor() {
        this.route.queryParamMap.pipe(takeUntilDestroyed()).subscribe(params => {
            const range = adminPeriod(params);
            if (range === null) {
                this.billing.clearInvalidPeriod();
                return;
            }
            this.billing.fromDate.set(range.from ?? '');
            this.billing.toDate.set(range.to ?? '');
            this.billing.search.set(params.get('search') ?? '');
            this.billing.provider.set(params.get('provider') ?? '');
            this.billing.status.set(params.get('status') ?? '');
            this.billing.kind.set(params.get('kind') ?? '');
            const tab = params.get('tab');
            this.billing.activeTab.set(tab === 'payments' || tab === 'webhook-events' ? tab : 'subscriptions');
            this.billing.page.set(adminPage(params.get('page')));
            this.billing.load();
        });
    }
}
