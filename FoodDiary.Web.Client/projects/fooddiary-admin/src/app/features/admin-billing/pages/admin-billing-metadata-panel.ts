import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

@Component({
    selector: 'fd-admin-billing-metadata-panel',
    imports: [],
    templateUrl: './admin-billing-metadata-panel.html',
    styleUrl: './admin-billing.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminBillingMetadataPanelComponent {
    public readonly metadata = input.required<string>();

    public readonly closePanel = output();
}
