import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';

import { injectCurrentLanguage } from '../../../../shared/i18n/inject-current-language';
import { LocalizedNumberPipe } from '../../../../shared/i18n/localized-number.pipe';
import { formatGoalHistoryDates } from '../../../../shared/lib/goal-history-date.utils';
import { MeasurementUnitPipe, MeasurementValuePipe } from '../../../../shared/measurements/measurement-display.pipe';
import { MeasurementSystemService } from '../../../../shared/measurements/measurement-system.service';
import type { WeightGoalHistoryItem } from '../../../../shared/models/user.data';
import { WeightHistoryFacade } from '../../lib/weight-history.facade';

type WeightGoalHistoryViewModel = WeightGoalHistoryItem & {
    dateRange: string;
    startDate: string;
    endDate: string | null;
    displayEndWeight: number;
    change: number;
    progress: number | null;
    statusKey: string;
};

const PERCENT_MAX = 100;

@Component({
    selector: 'fd-weight-goal-history-dialog',
    imports: [LocalizedNumberPipe, FdUiDialogComponent, MeasurementUnitPipe, MeasurementValuePipe, TranslatePipe],
    templateUrl: './weight-goal-history-dialog.html',
    styleUrl: './weight-goal-history-dialog.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WeightGoalHistoryDialogComponent {
    protected readonly locale = injectCurrentLanguage();
    protected readonly measurements = inject(MeasurementSystemService);
    private readonly facade = inject(WeightHistoryFacade);
    private readonly dialogRef = inject(FdUiDialogRef<WeightGoalHistoryDialogComponent, void>);

    protected readonly goals = computed<WeightGoalHistoryViewModel[]>(() => {
        const currentWeight = this.facade.latestWeight();
        const language = this.locale();
        return [...this.facade.weightGoalHistory()]
            .sort((a, b) => Number(b.status === 'Active') - Number(a.status === 'Active'))
            .map(goal => {
                const displayEndWeight =
                    goal.status === 'Active' ? (currentWeight ?? goal.startWeightKg) : (goal.endWeightKg ?? goal.startWeightKg);
                return {
                    ...goal,
                    ...formatGoalHistoryDates(goal.startedAtUtc, goal.endedAtUtc, language),
                    displayEndWeight,
                    change: displayEndWeight - goal.startWeightKg,
                    progress: goal.status === 'Active' ? this.calculateProgress(goal, displayEndWeight) : null,
                    statusKey: `WEIGHT_HISTORY.GOAL_STATUS_${goal.status.toUpperCase()}`,
                };
            });
    });

    protected close(): void {
        this.dialogRef.close();
    }

    private calculateProgress(goal: WeightGoalHistoryItem, currentWeight: number): number {
        const totalDistance = Math.abs(goal.targetWeightKg - goal.startWeightKg);
        if (totalDistance === 0) {
            return PERCENT_MAX;
        }
        const direction = Math.sign(goal.targetWeightKg - goal.startWeightKg);
        const completed = (currentWeight - goal.startWeightKg) * direction;
        return Math.min(PERCENT_MAX, Math.max(0, (completed / totalDistance) * PERCENT_MAX));
    }
}
