import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, ViewEncapsulation, input } from '@angular/core';
import { FdUiDialogComponent, FdUiDialogSize } from '../dialog/fd-ui-dialog.component';

@Component({
    selector: 'fd-ui-dialog-shell',
    standalone: true,
    imports: [CommonModule, FdUiDialogComponent],
    templateUrl: './fd-ui-dialog-shell.component.html',
    styleUrls: ['./fd-ui-dialog-shell.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    encapsulation: ViewEncapsulation.None,
})
export class FdUiDialogShellComponent {
    public readonly title = input<string | undefined>();
    public readonly subtitle = input<string | undefined>();
    public readonly size = input<FdUiDialogSize>('md');
    public readonly dismissible = input(true);
    public readonly flush = input(false);
}
