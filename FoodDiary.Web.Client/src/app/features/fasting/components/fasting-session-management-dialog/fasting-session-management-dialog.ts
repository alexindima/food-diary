import { ChangeDetectionStrategy, Component, effect, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiDialogShellComponent } from 'fd-ui-kit/dialog-shell/fd-ui-dialog-shell';

import { FastingFacade } from '../../lib/fasting.facade';
import { FastingControlsComponent } from '../fasting-controls/fasting-controls';

@Component({
    selector: 'fd-fasting-session-management-dialog',
    imports: [TranslatePipe, FdUiDialogShellComponent, FastingControlsComponent],
    templateUrl: './fasting-session-management-dialog.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FastingSessionManagementDialogComponent {
    private readonly facade = inject(FastingFacade);
    private readonly dialogRef = inject<FdUiDialogRef<FastingSessionManagementDialogComponent, void>>(FdUiDialogRef);

    public constructor() {
        effect(() => {
            if (!this.facade.isActive()) {
                this.dialogRef.close();
            }
        });
    }
}
