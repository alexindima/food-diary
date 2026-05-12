import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import type { AdminBillingSubscriptionViewModel } from './admin-billing.component';

@Component({
    selector: 'fd-admin-billing-subscriptions-table',
    imports: [],
    templateUrl: './admin-billing-subscriptions-table.component.html',
    styleUrl: './admin-billing.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminBillingSubscriptionsTableComponent {
    public readonly items = input.required<AdminBillingSubscriptionViewModel[]>();
}
