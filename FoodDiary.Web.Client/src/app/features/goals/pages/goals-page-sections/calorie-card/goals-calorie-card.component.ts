import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';

@Component({
    selector: 'fd-goals-calorie-card',
    imports: [TranslatePipe, FdUiCardComponent],
    templateUrl: './goals-calorie-card.component.html',
    styleUrl: '../../goals-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GoalsCalorieCardComponent {
    public readonly minCalories = input.required<number>();
    public readonly maxCalories = input.required<number>();
    public readonly calorieTarget = input.required<number>();
    public readonly ringProgressOffset = input.required<string>();
    public readonly accentColor = input.required<string>();
    public readonly ringKnobAngle = input.required<string>();

    public readonly ringPointerDown = output<PointerEvent>();
    public readonly ringPointerMove = output<PointerEvent>();
    public readonly ringPointerLeave = output<PointerEvent>();
    public readonly caloriesInput = output<Event>();
    public readonly caloriesBlur = output<Event>();
    public readonly sliderInput = output<Event>();
}
