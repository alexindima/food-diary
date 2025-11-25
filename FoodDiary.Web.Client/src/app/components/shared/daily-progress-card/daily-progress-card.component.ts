import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { DynamicProgressBarComponent } from '../dynamic-progress-bar/dynamic-progress-bar.component';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardActionsDirective } from 'fd-ui-kit/card/fd-ui-card-actions.directive';

@Component({
    selector: 'fd-daily-progress-card',
    standalone: true,
    imports: [
        CommonModule,
        TranslatePipe,
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
    public readonly settingsClick = output<void>();
    public readonly setGoalClick = output<void>();

    public readonly hasGoal = computed(() => this.goal() > 0);

    public readonly progressPercent = computed(() => {
        if (!this.hasGoal()) {
            return 0;
        }
        return Math.round(Math.max(0, (this.consumed() / this.goal()) * 100));
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
        if (!goalValue || goalValue <= 0) {
            return null;
        }

        const consumedValue = this.consumed();
        if (consumedValue <= 0) {
            return 'DAILY_PROGRESS_CARD.MOTIVATION.NONE';
        }

        const pct = (consumedValue / goalValue) * 100;

        if (pct <= 10) return 'DAILY_PROGRESS_CARD.MOTIVATION.P0_10';
        if (pct <= 20) return 'DAILY_PROGRESS_CARD.MOTIVATION.P10_20';
        if (pct <= 30) return 'DAILY_PROGRESS_CARD.MOTIVATION.P20_30';
        if (pct <= 40) return 'DAILY_PROGRESS_CARD.MOTIVATION.P30_40';
        if (pct <= 50) return 'DAILY_PROGRESS_CARD.MOTIVATION.P40_50';
        if (pct <= 60) return 'DAILY_PROGRESS_CARD.MOTIVATION.P50_60';
        if (pct <= 70) return 'DAILY_PROGRESS_CARD.MOTIVATION.P60_70';
        if (pct <= 80) return 'DAILY_PROGRESS_CARD.MOTIVATION.P70_80';
        if (pct <= 90) return 'DAILY_PROGRESS_CARD.MOTIVATION.P80_90';
        if (pct <= 110) return 'DAILY_PROGRESS_CARD.MOTIVATION.P90_110';
        if (pct <= 200) return 'DAILY_PROGRESS_CARD.MOTIVATION.P110_200';
        return 'DAILY_PROGRESS_CARD.MOTIVATION.ABOVE_200';
    });
}
