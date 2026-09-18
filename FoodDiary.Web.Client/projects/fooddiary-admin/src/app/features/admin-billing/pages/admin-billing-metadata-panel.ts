import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FdUiButtonComponent } from 'fd-ui-kit';

@Component({
    selector: 'fd-admin-billing-metadata-panel',
    imports: [FdUiButtonComponent],
    templateUrl: './admin-billing-metadata-panel.html',
    styleUrl: './admin-billing.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminBillingMetadataPanelComponent {
    public readonly metadata = input.required<string>();

    public readonly closePanel = output();
}
