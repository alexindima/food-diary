import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';

import type { AdminBillingCancellation, AdminBillingCancellationReason } from '../models/admin-billing.models';

@Component({
    selector: 'fd-admin-billing-cancellations-table',
    imports: [DatePipe, RouterLink, TranslatePipe],
    templateUrl: './admin-billing-cancellations-table.html',
    styleUrl: './admin-billing.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminBillingCancellationsTableComponent {
    public readonly items = input.required<AdminBillingCancellation[]>();
    public readonly reasons = input.required<AdminBillingCancellationReason[]>();
    public readonly feedbackCount = input.required<number>();
    public readonly totalItems = input.required<number>();
}
