import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';

import type { CyclingDayControl } from './goals-page.models';

export type DayCaloriesInputChange = {
    key: string;
    event: Event;
};

@Component({
    selector: 'fd-goals-cycling-card',
    imports: [TranslatePipe, FdUiCardComponent],
    templateUrl: './goals-cycling-card.component.html',
    styleUrl: './goals-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GoalsCyclingCardComponent {
    public readonly enabled = input.required<boolean>();
    public readonly dayControls = input.required<CyclingDayControl[]>();
    public readonly dayCalories = input.required<Record<string, number>>();
    public readonly maxCalories = input.required<number>();

    public readonly enabledToggle = output();
    public readonly dayCaloriesInput = output<DayCaloriesInputChange>();
}
