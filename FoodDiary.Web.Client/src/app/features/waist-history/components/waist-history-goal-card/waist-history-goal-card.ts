import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';

import { resolveTranslateLanguage } from '../../../../shared/i18n/translate-language.utils';
import { formatDateValue } from '../../../../shared/lib/local-date.utils';
import type { WaistGoalHistoryItem } from '../../../../shared/models/user.data';

const PERCENT_MAX = 100;
const DAYS_PER_WEEK = 7;
const MILLISECONDS_PER_DAY = 86_400_000;

@Component({
    selector: 'fd-waist-history-goal-card',
    imports: [DecimalPipe, FdUiButtonComponent, FdUiCardComponent, FdUiIconComponent, TranslatePipe],
    templateUrl: './waist-history-goal-card.html',
    styleUrl: '../../pages/waist-history-page/waist-history-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WaistHistoryGoalCardComponent {
    private readonly translateService = inject(TranslateService);
    public readonly currentWaist = input.required<number | null>();
    public readonly currentWaistDate = input.required<string | null>();
    public readonly desiredWaist = input.required<number | null>();
    public readonly startWaist = input.required<number | null>();
    public readonly startedAtUtc = input.required<string | null>();
    public readonly hasGoalHistory = input.required<boolean>();
    public readonly lastCompletedGoal = input.required<WaistGoalHistoryItem | null>();
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
        const options = { day: 'numeric', month: 'short', year: 'numeric' } as const;
        const change = goal.endWaist === null ? null : goal.endWaist - goal.startWaist;
        return {
            ...goal,
            startDate: formatDateValue(goal.startedAtUtc, language, options),
            endDate: formatDateValue(goal.endedAtUtc, language, options),
            change,
        };
    });

    protected readonly progress = computed(() => {
        const current = this.currentWaist();
        const goal = this.desiredWaist();
        const start = this.startWaist();
        if (current === null || goal === null || start === null) {
            return null;
        }
        const direction = Math.sign(goal - start);
        const totalDistance = Math.abs(goal - start);
        const completedDistance = (current - start) * direction;
        const percent =
            totalDistance === 0 ? PERCENT_MAX : Math.min(PERCENT_MAX, Math.max(0, (completedDistance / totalDistance) * PERCENT_MAX));
        const remaining = Math.max(0, (goal - current) * direction);
        const daysElapsed = this.daysBetweenEntries();
        const weeklyRate = daysElapsed > 0 ? (completedDistance / daysElapsed) * DAYS_PER_WEEK : 0;
        const daysToGoal = weeklyRate > 0 ? Math.ceil((remaining / weeklyRate) * DAYS_PER_WEEK) : null;
        return { percent, change: completedDistance, remaining, weeklyRate, daysToGoal, startWaist: start, currentWaist: current };
    });

    private daysBetweenEntries(): number {
        const latest = this.currentWaistDate();
        const oldest = this.startedAtUtc();
        if (latest === null || oldest === null) {
            return 0;
        }
        const difference = new Date(latest).getTime() - new Date(oldest).getTime();
        return Number.isFinite(difference) ? Math.max(0, difference / MILLISECONDS_PER_DAY) : 0;
    }
}
