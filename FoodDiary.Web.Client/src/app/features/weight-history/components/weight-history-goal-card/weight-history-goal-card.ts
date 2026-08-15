import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';

import { resolveTranslateLanguage } from '../../../../shared/i18n/translate-language.utils';
import { formatDateValue } from '../../../../shared/lib/local-date.utils';
import { MeasurementUnitPipe, MeasurementValuePipe } from '../../../../shared/measurements/measurement-display.pipe';
import { MeasurementSystemService } from '../../../../shared/measurements/measurement-system.service';
import type { WeightGoalHistoryItem } from '../../../../shared/models/user.data';
import { getWeightRemainingToGoal } from '../../lib/weight-history-progress.utils';

const PERCENT_MAX = 100;
const DAYS_PER_WEEK = 7;
const MILLISECONDS_PER_DAY = 86_400_000;

@Component({
    selector: 'fd-weight-history-goal-card',
    imports: [
        DecimalPipe,
        FdUiButtonComponent,
        FdUiCardComponent,
        FdUiIconComponent,
        MeasurementUnitPipe,
        MeasurementValuePipe,
        TranslatePipe,
    ],
    templateUrl: './weight-history-goal-card.html',
    styleUrl: '../../pages/weight-history-page/weight-history-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WeightHistoryGoalCardComponent {
    protected readonly measurements = inject(MeasurementSystemService);
    private readonly translateService = inject(TranslateService);
    public readonly currentWeight = input.required<number | null>();
    public readonly currentWeightDate = input.required<string | null>();
    public readonly desiredWeightKg = input.required<number | null>();
    public readonly startWeightKg = input.required<number | null>();
    public readonly startedAtUtc = input.required<string | null>();
    public readonly hasGoalHistory = input.required<boolean>();
    public readonly lastCompletedGoal = input.required<WeightGoalHistoryItem | null>();

    public readonly configureGoal = output();
    public readonly viewGoalHistory = output();

    protected readonly startDateLabel = computed(() =>
        formatDateValue(this.startedAtUtc(), resolveTranslateLanguage(this.translateService), {
            day: 'numeric',
            month: 'long',
            year: 'numeric',
        }),
    );
    protected readonly lastGoalSummary = computed(() => {
        const goal = this.lastCompletedGoal();
        if (goal === null) {
            return null;
        }

        const language = resolveTranslateLanguage(this.translateService);
        const dateOptions = { day: 'numeric', month: 'short', year: 'numeric' } as const;
        const startDate = formatDateValue(goal.startedAtUtc, language, dateOptions);
        const endDate = formatDateValue(goal.endedAtUtc, language, dateOptions);
        const change = goal.endWeightKg === null ? null : goal.endWeightKg - goal.startWeightKg;
        return { ...goal, startDate, endDate, change };
    });
    protected readonly progress = computed(() => {
        const current = this.currentWeight();
        const goal = this.desiredWeightKg();
        const oldest = this.startWeightKg();
        if (current === null || goal === null || oldest === null) {
            return null;
        }

        const goalDirection = Math.sign(goal - oldest);
        const totalDistance = Math.abs(goal - oldest);
        const completedDistance = (current - oldest) * goalDirection;
        const percent =
            totalDistance === 0 ? PERCENT_MAX : Math.min(PERCENT_MAX, Math.max(0, (completedDistance / totalDistance) * PERCENT_MAX));
        const lost = completedDistance;
        const remaining = getWeightRemainingToGoal(oldest, current, goal);
        const daysElapsed = this.daysBetweenEntries();
        const weeklyRate = daysElapsed > 0 ? (completedDistance / daysElapsed) * DAYS_PER_WEEK : 0;
        const daysToGoal = weeklyRate > 0 ? Math.ceil((remaining / weeklyRate) * DAYS_PER_WEEK) : null;

        return { percent, lost, remaining, weeklyRate, daysToGoal, startWeightKg: oldest, currentWeight: current };
    });

    private daysBetweenEntries(): number {
        const latestDate = this.currentWeightDate();
        const oldestDate = this.startedAtUtc();
        if (latestDate === null || oldestDate === null) {
            return 0;
        }

        const difference = new Date(latestDate).getTime() - new Date(oldestDate).getTime();
        return Number.isFinite(difference) ? Math.max(0, difference / MILLISECONDS_PER_DAY) : 0;
    }
}
