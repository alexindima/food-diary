import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiDialogShellComponent } from 'fd-ui-kit/dialog-shell/fd-ui-dialog-shell';

import { injectCurrentLanguage } from '../../../../shared/i18n/inject-current-language';
import { LocalizedNumberPipe } from '../../../../shared/i18n/localized-number.pipe';
import type { FastingCheckIn, FastingSession } from '../../../../shared/models/fasting.data';
import type { FastingCheckInViewModel } from '../../lib/fasting-page.types';
import { getFastingDurationDisplay } from '../../lib/fasting-session-state';
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
        LocalizedNumberPipe,
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
    protected readonly locale = injectCurrentLanguage();
    protected readonly data = inject<FastingSessionDetailsDialogData>(FD_UI_DIALOG_DATA);
    private readonly dialogRef = inject<FdUiDialogRef<FastingSessionDetailsDialogComponent, void>>(FdUiDialogRef);

    protected readonly durationDisplay = computed(() => getFastingDurationDisplay(this.data.session));
    protected readonly periodLabel = computed(() =>
        this.data.endedAtLabel === null ? this.data.startedAtLabel : `${this.data.startedAtLabel} → ${this.data.endedAtLabel}`,
    );

    protected close(): void {
        this.dialogRef.close();
    }
}
