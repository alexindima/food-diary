import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';

import { resolveTranslateLanguage } from '../../../../shared/i18n/translate-language.utils';
import { formatDateValue } from '../../../../shared/lib/local-date.utils';
import type { WaistGoalHistoryItem } from '../../../../shared/models/user.data';
import { WaistHistoryFacade } from '../../lib/waist-history.facade';

type ViewModel = WaistGoalHistoryItem & {
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
    imports: [DecimalPipe, FdUiDialogComponent, FdUiIconComponent, TranslatePipe],
    templateUrl: './waist-goal-history-dialog.html',
    styleUrl: './waist-goal-history-dialog.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WaistGoalHistoryDialogComponent {
    private readonly facade = inject(WaistHistoryFacade);
    private readonly translate = inject(TranslateService);
    private readonly dialogRef = inject(FdUiDialogRef<WaistGoalHistoryDialogComponent, void>);
    protected readonly goals = computed<ViewModel[]>(() => {
        const current = this.facade.latestWaist();
        const language = resolveTranslateLanguage(this.translate);
        return this.facade.waistGoalHistory().map(goal => {
            const displayEndWaist = goal.status === 'Active' ? (current ?? goal.startWaistCm) : (goal.endWaistCm ?? goal.startWaistCm);
            return {
                ...goal,
                startDate: formatDateValue(goal.startedAtUtc, language, { day: 'numeric', month: 'long', year: 'numeric' }) ?? '',
                endDate:
                    goal.endedAtUtc !== null
                        ? formatDateValue(goal.endedAtUtc, language, { day: 'numeric', month: 'long', year: 'numeric' })
                        : null,
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
