import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';

import { resolveTranslateLanguage } from '../../../../shared/i18n/translate-language.utils';
import { formatDateValue } from '../../../../shared/lib/local-date.utils';
import type { WeightGoalHistoryItem } from '../../../../shared/models/user.data';
import { WeightHistoryFacade } from '../../lib/weight-history.facade';

type WeightGoalHistoryViewModel = WeightGoalHistoryItem & {
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
    imports: [DecimalPipe, FdUiDialogComponent, FdUiIconComponent, TranslatePipe],
    templateUrl: './weight-goal-history-dialog.html',
    styleUrl: './weight-goal-history-dialog.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WeightGoalHistoryDialogComponent {
    private readonly facade = inject(WeightHistoryFacade);
    private readonly translate = inject(TranslateService);
    private readonly dialogRef = inject(FdUiDialogRef<WeightGoalHistoryDialogComponent, void>);

    protected readonly goals = computed<WeightGoalHistoryViewModel[]>(() => {
        const currentWeight = this.facade.latestWeight();
        const language = resolveTranslateLanguage(this.translate);
        return this.facade.weightGoalHistory().map(goal => {
            const displayEndWeight =
                goal.status === 'Active' ? (currentWeight ?? goal.startWeightKg) : (goal.endWeightKg ?? goal.startWeightKg);
            return {
                ...goal,
                startDate: formatDateValue(goal.startedAtUtc, language, { day: 'numeric', month: 'long', year: 'numeric' }) ?? '',
                endDate:
                    goal.endedAtUtc !== null
                        ? formatDateValue(goal.endedAtUtc, language, { day: 'numeric', month: 'long', year: 'numeric' })
                        : null,
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
