import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogShellComponent } from 'fd-ui-kit/dialog-shell/fd-ui-dialog-shell';

import type { FastingCheckIn } from '../../models/fasting.data';
import { FastingCheckInChartComponent } from '../fasting-check-in-chart/fasting-check-in-chart';

export type FastingCheckInChartDialogData = {
    title: string;
    subtitle: string;
    checkIns: FastingCheckIn[];
};

@Component({
    selector: 'fd-fasting-checkin-chart-dialog',
    imports: [FdUiDialogShellComponent, FastingCheckInChartComponent],
    templateUrl: './fasting-checkin-chart-dialog.html',
    styleUrl: './fasting-checkin-chart-dialog.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FastingCheckInChartDialogComponent {
    protected readonly data = inject<FastingCheckInChartDialogData>(FD_UI_DIALOG_DATA);
}
