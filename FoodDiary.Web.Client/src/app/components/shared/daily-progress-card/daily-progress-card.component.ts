import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiCardActionsDirective } from 'fd-ui-kit/card/fd-ui-card-actions.directive';

import { DynamicProgressBarComponent } from '../dynamic-progress-bar/dynamic-progress-bar.component';
import { calculateDailyProgressPercent, calculateRemainingCalories, resolveDailyProgressMotivationKey } from './daily-progress-card.utils';

@Component({
    selector: 'fd-daily-progress-card',
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

    public readonly progressPercent = computed(() => calculateDailyProgressPercent(this.consumed(), this.goal()));

    public readonly remaining = computed(() => calculateRemainingCalories(this.consumed(), this.goal()));

    public readonly motivationKey = computed(() => resolveDailyProgressMotivationKey(this.consumed(), this.goal()));
}
