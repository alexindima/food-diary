import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiDialogShellComponent } from 'fd-ui-kit/dialog-shell/fd-ui-dialog-shell';

import { MS_PER_HOUR } from '../../../../shared/lib/time.constants';
import type { FastingCheckIn, FastingSession } from '../../models/fasting.data';
import type { FastingCheckInViewModel } from '../../pages/fasting-page-lib/fasting-page.types';
import { FastingCheckInChartComponent } from '../fasting-check-in-chart/fasting-check-in-chart';
import { FastingHistoryCheckInEntryComponent } from '../fasting-history-check-in-entry/fasting-history-check-in-entry';

export type FastingSessionDetailsDialogData = {
    session: FastingSession;
    startedAtLabel: string;
    endedAtLabel: string | null;
    sessionTypeLabel: string;
    protocolDisplay: string;
    badgeKey: string;
    checkIns: readonly FastingCheckInViewModel[];
    chartCheckIns: readonly FastingCheckIn[];
};

@Component({
    selector: 'fd-fasting-session-details-dialog',
    imports: [
        DecimalPipe,
        TranslatePipe,
        FdUiButtonComponent,
        FdUiDialogFooterDirective,
        FdUiDialogShellComponent,
        FastingCheckInChartComponent,
        FastingHistoryCheckInEntryComponent,
    ],
    templateUrl: './fasting-session-details-dialog.html',
    styleUrl: './fasting-session-details-dialog.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FastingSessionDetailsDialogComponent {
    protected readonly data = inject<FastingSessionDetailsDialogData>(FD_UI_DIALOG_DATA);
    private readonly dialogRef = inject<FdUiDialogRef<FastingSessionDetailsDialogComponent, void>>(FdUiDialogRef);

    protected readonly durationHours = computed(() => {
        const end = this.data.session.endedAtUtc === null ? new Date() : new Date(this.data.session.endedAtUtc);
        return Math.max(0, (end.getTime() - new Date(this.data.session.startedAtUtc).getTime()) / MS_PER_HOUR);
    });
    protected readonly periodLabel = computed(() =>
        this.data.endedAtLabel === null ? this.data.startedAtLabel : `${this.data.startedAtLabel} → ${this.data.endedAtLabel}`,
    );

    protected close(): void {
        this.dialogRef.close();
    }
}
