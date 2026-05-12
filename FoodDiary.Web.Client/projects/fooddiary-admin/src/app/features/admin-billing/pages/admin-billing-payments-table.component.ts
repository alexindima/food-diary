import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';

import type { AdminBillingPaymentViewModel } from './admin-billing.types';

@Component({
    selector: 'fd-admin-billing-payments-table',
    imports: [FdUiButtonComponent],
    templateUrl: './admin-billing-payments-table.component.html',
    styleUrl: './admin-billing.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminBillingPaymentsTableComponent {
    public readonly items = input.required<AdminBillingPaymentViewModel[]>();

    public readonly metadataOpen = output<string | null | undefined>();
}
