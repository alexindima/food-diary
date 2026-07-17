import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination';

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

    public constructor() {
        this.billing.load();
    }
}
