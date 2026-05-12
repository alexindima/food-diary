import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';

import type { AdminBillingWebhookEventViewModel } from './admin-billing.component';

@Component({
    selector: 'fd-admin-billing-webhooks-table',
    imports: [FdUiButtonComponent],
    templateUrl: './admin-billing-webhooks-table.component.html',
    styleUrl: './admin-billing.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminBillingWebhooksTableComponent {
    public readonly items = input.required<AdminBillingWebhookEventViewModel[]>();

    public readonly metadataOpen = output<string | null | undefined>();
}
