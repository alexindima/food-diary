import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';

@Component({
    selector: 'fd-unsaved-changes-bar',
    imports: [FdUiButtonComponent],
    templateUrl: './unsaved-changes-bar.html',
    styleUrl: './unsaved-changes-bar.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UnsavedChangesBarComponent {
    public readonly message = input.required<string>();
    public readonly discardLabel = input.required<string>();
    public readonly saveLabel = input.required<string>();
    public readonly saving = input(false);
    public readonly save = output();
    public readonly discard = output();
}
