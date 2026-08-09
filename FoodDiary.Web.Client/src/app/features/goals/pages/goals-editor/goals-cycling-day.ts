import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import type { DayCalorieKey } from '../../models/goals.data';

@Component({
    selector: 'fd-goals-cycling-day',
    imports: [TranslatePipe],
    templateUrl: './goals-cycling-day.html',
    styleUrl: './goals-editor.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GoalsCyclingDayComponent {
    public readonly dayKey = input.required<DayCalorieKey>();
    public readonly labelKey = input.required<string>();
    public readonly value = input.required<number>();
    public readonly barPercent = input.required<number>();
    public readonly weekend = input(false);
    public readonly weekendPosition = input<'start' | 'end' | null>(null);
    public readonly valueChange = output<{ key: DayCalorieKey; value: number }>();

    protected updateValue(event: Event): void {
        if (!(event.target instanceof HTMLInputElement)) {
            return;
        }

        this.valueChange.emit({ key: this.dayKey(), value: Math.max(0, Math.round(Number(event.target.value))) });
    }
}
