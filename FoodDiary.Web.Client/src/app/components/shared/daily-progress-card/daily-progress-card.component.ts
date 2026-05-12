import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiCardActionsDirective } from 'fd-ui-kit/card/fd-ui-card-actions.directive';

import { DynamicProgressBarComponent } from '../dynamic-progress-bar/dynamic-progress-bar.component';

const PERCENT_MULTIPLIER = 100;

const MOTIVATION_THRESHOLDS: ReadonlyArray<{ maxPercent: number; key: string }> = [
    { maxPercent: 10, key: 'DAILY_PROGRESS_CARD.MOTIVATION.P0_10' },
    { maxPercent: 20, key: 'DAILY_PROGRESS_CARD.MOTIVATION.P10_20' },
    { maxPercent: 30, key: 'DAILY_PROGRESS_CARD.MOTIVATION.P20_30' },
    { maxPercent: 40, key: 'DAILY_PROGRESS_CARD.MOTIVATION.P30_40' },
    { maxPercent: 50, key: 'DAILY_PROGRESS_CARD.MOTIVATION.P40_50' },
    { maxPercent: 60, key: 'DAILY_PROGRESS_CARD.MOTIVATION.P50_60' },
    { maxPercent: 70, key: 'DAILY_PROGRESS_CARD.MOTIVATION.P60_70' },
    { maxPercent: 80, key: 'DAILY_PROGRESS_CARD.MOTIVATION.P70_80' },
    { maxPercent: 90, key: 'DAILY_PROGRESS_CARD.MOTIVATION.P80_90' },
    { maxPercent: 110, key: 'DAILY_PROGRESS_CARD.MOTIVATION.P90_110' },
    { maxPercent: 200, key: 'DAILY_PROGRESS_CARD.MOTIVATION.P110_200' },
];

@Component({
    selector: 'fd-daily-progress-card',
    standalone: true,
    imports: [
        CommonModule,
        TranslatePipe,
        FdUiHintDirective,
        FdUiCardComponent,
        DynamicProgressBarComponent,
        FdUiButtonComponent,
        FdUiCardActionsDirective,
    ],
    templateUrl: './daily-progress-card.component.html',
    styleUrl: './daily-progress-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DailyProgressCardComponent {
    public readonly date = input.required<Date>();
    public readonly consumed = input<number>(0);
    public readonly goal = input<number>(0);
    public readonly settingsClick = output();
    public readonly setGoalClick = output();

    public readonly hasGoal = computed(() => this.goal() > 0);

    public readonly progressPercent = computed(() => {
        if (!this.hasGoal()) {
            return 0;
        }
        return Math.round(Math.max(0, (this.consumed() / this.goal()) * PERCENT_MULTIPLIER));
    });

    public readonly remaining = computed(() => {
        if (!this.hasGoal()) {
            return null;
        }
        const remaining = this.goal() - this.consumed();
        return remaining > 0 ? remaining : 0;
    });

    public readonly motivationKey = computed(() => {
        if (!this.hasGoal()) {
            return null;
        }

        const goalValue = this.goal();
        if (goalValue <= 0) {
            return null;
        }

        const consumedValue = this.consumed();
        if (consumedValue <= 0) {
            return 'DAILY_PROGRESS_CARD.MOTIVATION.NONE';
        }

        const percent = (consumedValue / goalValue) * PERCENT_MULTIPLIER;
        return MOTIVATION_THRESHOLDS.find(item => percent <= item.maxPercent)?.key ?? 'DAILY_PROGRESS_CARD.MOTIVATION.ABOVE_200';
    });
}
