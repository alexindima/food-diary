import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import type { MacroSliderView } from './goals-page.models';

@Component({
    selector: 'fd-goals-macro-slider',
    imports: [TranslatePipe],
    templateUrl: './goals-macro-slider.component.html',
    styleUrl: './goals-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GoalsMacroSliderComponent {
    public readonly macro = input.required<MacroSliderView>();

    public readonly valueInput = output<Event>();
    public readonly sliderInput = output<Event>();
}
