import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { FdUiButtonComponent } from '../button/fd-ui-button';
import { FdUiIconComponent } from '../icon/fd-ui-icon';

@Component({
    selector: 'fd-ui-section-error-state',
    imports: [FdUiButtonComponent, FdUiIconComponent],
    templateUrl: './fd-ui-section-error-state.html',
    styleUrl: './fd-ui-section-state.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiSectionErrorStateComponent {
    public readonly errorIcon = input.required<string>();
    public readonly errorTitle = input.required<string>();
    public readonly errorMessage = input.required<string>();
    public readonly retryLabel = input<string | null>(null);

    public readonly retry = output();
}
