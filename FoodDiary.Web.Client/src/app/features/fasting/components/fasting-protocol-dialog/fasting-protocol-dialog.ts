import { ChangeDetectionStrategy, Component, effect, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiDialogShellComponent } from 'fd-ui-kit/dialog-shell/fd-ui-dialog-shell';

import { FastingFacade } from '../../lib/fasting.facade';
import { FastingControlsComponent } from '../fasting-controls/fasting-controls';

@Component({
    selector: 'fd-fasting-protocol-dialog',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiDialogFooterDirective, FdUiDialogShellComponent, FastingControlsComponent],
    templateUrl: './fasting-protocol-dialog.html',
    styleUrl: './fasting-protocol-dialog.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FastingProtocolDialogComponent {
    private readonly facade = inject(FastingFacade);
    private readonly dialogRef = inject<FdUiDialogRef<FastingProtocolDialogComponent, void>>(FdUiDialogRef);
    protected readonly isStarting = this.facade.isStarting;

    public constructor() {
        effect(() => {
            if (this.facade.isActive()) {
                this.dialogRef.close();
            }
        });
    }

    protected startFasting(): void {
        this.facade.startFasting();
    }
}
