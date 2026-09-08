import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';

import type { AdminBillingSubscriptionViewModel } from './admin-billing.types';

@Component({
    selector: 'fd-admin-billing-subscriptions-table',
    imports: [RouterLink, TranslatePipe],
    templateUrl: './admin-billing-subscriptions-table.html',
    styleUrl: './admin-billing.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminBillingSubscriptionsTableComponent {
    public readonly items = input.required<AdminBillingSubscriptionViewModel[]>();
}
