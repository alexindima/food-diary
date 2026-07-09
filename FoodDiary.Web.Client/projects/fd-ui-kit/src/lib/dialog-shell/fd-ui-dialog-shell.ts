import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { type FdUiDialogBodyScrollInset, FdUiDialogComponent, type FdUiDialogSize } from '../dialog/fd-ui-dialog';

@Component({
    selector: 'fd-ui-dialog-shell',
    imports: [CommonModule, FdUiDialogComponent],
    templateUrl: './fd-ui-dialog-shell.html',
    styleUrls: ['./fd-ui-dialog-shell.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiDialogShellComponent {
    public readonly title = input<string | undefined>();
    public readonly subtitle = input<string | undefined>();
    public readonly size = input<FdUiDialogSize>('md');
    public readonly bodyScrollInset = input<FdUiDialogBodyScrollInset>('default');
    public readonly dismissible = input(true);
}
