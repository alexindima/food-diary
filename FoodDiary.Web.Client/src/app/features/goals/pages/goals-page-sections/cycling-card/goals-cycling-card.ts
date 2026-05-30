import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';

import type { DayCalorieKey } from '../../../models/goals.data';
import type { CyclingDayControl, DayCaloriesInputChange } from '../../goals-page-lib/goals-page.models';

@Component({
    selector: 'fd-goals-cycling-card',
    imports: [TranslatePipe, FdUiCardComponent],
    templateUrl: './goals-cycling-card.html',
    styleUrl: '../../goals-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GoalsCyclingCardComponent {
    public readonly enabled = input.required<boolean>();
    public readonly dayControls = input.required<CyclingDayControl[]>();
    public readonly dayCalories = input.required<Record<DayCalorieKey, number>>();
    public readonly maxCalories = input.required<number>();

    public readonly enabledToggle = output();
    public readonly dayCaloriesInput = output<DayCaloriesInputChange>();
}
