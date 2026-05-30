import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';

import type { MacroSliderView } from '../../goals-page-lib/goals-page.models';
import { GoalsMacroSliderComponent } from '../macro-slider/goals-macro-slider';

@Component({
    selector: 'fd-goals-water-card',
    imports: [TranslatePipe, FdUiCardComponent, GoalsMacroSliderComponent],
    templateUrl: './goals-water-card.html',
    styleUrl: '../../goals-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GoalsWaterCardComponent {
    public readonly water = input.required<MacroSliderView>();

    public readonly waterInput = output<Event>();
    public readonly waterSlider = output<Event>();
}
