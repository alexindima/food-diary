import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';

import type { AdminBillingWebhookEventViewModel } from './admin-billing.types';

@Component({
    selector: 'fd-admin-billing-webhooks-table',
    imports: [FdUiButtonComponent],
    templateUrl: './admin-billing-webhooks-table.html',
    styleUrl: './admin-billing.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminBillingWebhooksTableComponent {
    public readonly items = input.required<AdminBillingWebhookEventViewModel[]>();

    public readonly metadataOpen = output<string | null | undefined>();
}
