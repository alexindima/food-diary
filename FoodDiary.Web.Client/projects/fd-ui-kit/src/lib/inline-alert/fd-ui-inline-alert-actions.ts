import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

@Component({
    selector: 'fd-ui-inline-alert-actions',
    templateUrl: './fd-ui-inline-alert-actions.html',
    styleUrl: './fd-ui-inline-alert.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiInlineAlertActionsComponent {
    public readonly primaryActionLabel = input<string | null>(null);
    public readonly secondaryActionLabel = input<string | null>(null);

    public readonly primaryAction = output();
    public readonly secondaryAction = output();
}
