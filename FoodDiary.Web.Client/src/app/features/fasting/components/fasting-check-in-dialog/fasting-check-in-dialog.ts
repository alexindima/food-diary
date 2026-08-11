import { ChangeDetectionStrategy, Component, effect, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiDialogShellComponent } from 'fd-ui-kit/dialog-shell/fd-ui-dialog-shell';

import { FastingFacade } from '../../lib/fasting.facade';
import { FastingCheckInCardComponent } from '../fasting-check-in-card/fasting-check-in-card';

export type FastingCheckInDialogResult = 'saved' | 'cancel';

@Component({
    selector: 'fd-fasting-check-in-dialog',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiDialogFooterDirective, FdUiDialogShellComponent, FastingCheckInCardComponent],
    templateUrl: './fasting-check-in-dialog.html',
    styleUrl: './fasting-check-in-dialog.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FastingCheckInDialogComponent {
    private readonly facade = inject(FastingFacade);
    private readonly dialogRef = inject<FdUiDialogRef<FastingCheckInDialogComponent, FastingCheckInDialogResult>>(FdUiDialogRef);
    private readonly initialSavedVersion = this.facade.checkInSavedVersion();

    protected readonly isSaving = this.facade.isSavingCheckIn;
    protected readonly isEnding = this.facade.isEnding;
    protected readonly isUpdatingCycle = this.facade.isUpdatingCycle;
    protected readonly hungerLevel = this.facade.hungerLevel;
    protected readonly energyLevel = this.facade.energyLevel;
    protected readonly moodLevel = this.facade.moodLevel;
    protected readonly selectedSymptoms = this.facade.selectedSymptoms;
    protected readonly notes = this.facade.checkInNotes;
    protected readonly saveDisabled = (): boolean => this.isSaving() || this.isEnding() || this.isUpdatingCycle();

    public constructor() {
        effect(() => {
            if (this.facade.checkInSavedVersion() > this.initialSavedVersion) {
                this.dialogRef.close('saved');
            }
        });
    }

    protected save(): void {
        this.facade.saveCheckIn();
    }
}
