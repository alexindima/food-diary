import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

@Component({
    selector: 'fd-admin-billing-metadata-panel',
    imports: [],
    templateUrl: './admin-billing-metadata-panel.component.html',
    styleUrl: './admin-billing.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminBillingMetadataPanelComponent {
    public readonly metadata = input.required<string>();

    public readonly closePanel = output<void>();
}
