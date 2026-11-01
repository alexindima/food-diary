import { ChangeDetectionStrategy, Component, inject } from '@angular/core';

import { AdminBillingFacade } from '../lib/admin-billing.facade';
import { AdminBillingCancellationsTableComponent } from './admin-billing-cancellations-table';
import { AdminBillingPaymentsTableComponent } from './admin-billing-payments-table';
import { AdminBillingSubscriptionsTableComponent } from './admin-billing-subscriptions-table';
import { AdminBillingWebhooksTableComponent } from './admin-billing-webhooks-table';

@Component({
    selector: 'fd-admin-billing-records',
    imports: [
        AdminBillingCancellationsTableComponent,
        AdminBillingPaymentsTableComponent,
        AdminBillingSubscriptionsTableComponent,
        AdminBillingWebhooksTableComponent,
    ],
    templateUrl: './admin-billing-records.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminBillingRecordsComponent {
    protected readonly billing = inject(AdminBillingFacade);
}
