import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';

import type { DayCalorieKey } from '../../../models/goals.data';
import type { CyclingDayControl, DayCaloriesInputChange } from '../../goals-page-lib/goals-page.models';

@Component({
    selector: 'fd-goals-cycling-card',
    imports: [TranslatePipe, FdUiCardComponent],
    templateUrl: './goals-cycling-card.component.html',
    styleUrl: '../../goals-page.component.scss',
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
