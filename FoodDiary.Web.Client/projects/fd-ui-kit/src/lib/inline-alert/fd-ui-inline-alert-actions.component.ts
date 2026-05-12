import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

@Component({
    selector: 'fd-ui-inline-alert-actions',
    templateUrl: './fd-ui-inline-alert-actions.component.html',
    styleUrl: './fd-ui-inline-alert.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiInlineAlertActionsComponent {
    public readonly primaryActionLabel = input<string | null>(null);
    public readonly secondaryActionLabel = input<string | null>(null);

    public readonly primaryAction = output<void>();
    public readonly secondaryAction = output<void>();
}
