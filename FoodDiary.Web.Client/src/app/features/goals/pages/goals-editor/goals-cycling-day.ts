import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import type { DayCalorieKey } from '../../../../shared/models/goals.data';
import { GoalsNumberInputDirective } from './goals-number-input.directive';

@Component({
    selector: 'fd-goals-cycling-day',
    imports: [TranslatePipe, GoalsNumberInputDirective],
    templateUrl: './goals-cycling-day.html',
    styleUrl: './goals-editor.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GoalsCyclingDayComponent {
    public readonly dayKey = input.required<DayCalorieKey>();
    public readonly labelKey = input.required<string>();
    public readonly value = input.required<number>();
    public readonly incomplete = input.required<boolean>();
    public readonly barPercent = input.required<number>();
    public readonly weekend = input(false);
    public readonly weekendPosition = input<'start' | 'end' | null>(null);
    public readonly valueChange = output<{ key: DayCalorieKey; value: number | null }>();

    protected updateValue(value: number | null, field: HTMLInputElement): void {
        if (value === null) {
            this.valueChange.emit({ key: this.dayKey(), value });
            return;
        }

        const calories = Math.max(0, Math.round(value));
        field.value = String(calories);
        this.valueChange.emit({ key: this.dayKey(), value: calories });
    }
}
