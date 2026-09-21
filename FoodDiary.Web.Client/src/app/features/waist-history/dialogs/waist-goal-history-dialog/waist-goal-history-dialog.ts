import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';

import { injectCurrentLanguage } from '../../../../shared/i18n/inject-current-language';
import { LocalizedNumberPipe } from '../../../../shared/i18n/localized-number.pipe';
import { formatGoalHistoryDates } from '../../../../shared/lib/goal-history-date.utils';
import { MeasurementUnitPipe, MeasurementValuePipe } from '../../../../shared/measurements/measurement-display.pipe';
import { MeasurementSystemService } from '../../../../shared/measurements/measurement-system.service';
import type { WaistGoalHistoryItem } from '../../../../shared/models/user.data';
import { WaistHistoryFacade } from '../../lib/waist-history.facade';

type ViewModel = WaistGoalHistoryItem & {
    dateRange: string;
    startDate: string;
    endDate: string | null;
    displayEndWaist: number;
    change: number;
    progress: number | null;
    statusKey: string;
};
const PERCENT_MAX = 100;

@Component({
    selector: 'fd-waist-goal-history-dialog',
    imports: [LocalizedNumberPipe, FdUiDialogComponent, MeasurementUnitPipe, MeasurementValuePipe, TranslatePipe],
    templateUrl: './waist-goal-history-dialog.html',
    styleUrl: './waist-goal-history-dialog.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WaistGoalHistoryDialogComponent {
    protected readonly locale = injectCurrentLanguage();
    protected readonly measurements = inject(MeasurementSystemService);
    private readonly facade = inject(WaistHistoryFacade);
    private readonly dialogRef = inject(FdUiDialogRef<WaistGoalHistoryDialogComponent, void>);
    protected readonly goals = computed<ViewModel[]>(() => {
        const current = this.facade.latestWaist();
        const language = this.locale();
        return [...this.facade.waistGoalHistory()]
            .sort((a, b) => Number(b.status === 'Active') - Number(a.status === 'Active'))
            .map(goal => {
                const displayEndWaist = goal.status === 'Active' ? (current ?? goal.startWaistCm) : (goal.endWaistCm ?? goal.startWaistCm);
                return {
                    ...goal,
                    ...formatGoalHistoryDates(goal.startedAtUtc, goal.endedAtUtc, language),
                    displayEndWaist,
                    change: displayEndWaist - goal.startWaistCm,
                    progress: goal.status === 'Active' ? this.calculateProgress(goal, displayEndWaist) : null,
                    statusKey: `WAIST_HISTORY.GOAL_STATUS_${goal.status.toUpperCase()}`,
                };
            });
    });
    protected close(): void {
        this.dialogRef.close();
    }
    private calculateProgress(goal: WaistGoalHistoryItem, current: number): number {
        const total = Math.abs(goal.targetWaistCm - goal.startWaistCm);
        if (total === 0) {
            return PERCENT_MAX;
        }
        return Math.min(
            PERCENT_MAX,
            Math.max(0, (((current - goal.startWaistCm) * Math.sign(goal.targetWaistCm - goal.startWaistCm)) / total) * PERCENT_MAX),
        );
    }
}
