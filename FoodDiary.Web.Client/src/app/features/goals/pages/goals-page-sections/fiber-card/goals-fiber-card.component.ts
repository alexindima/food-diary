import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';

import type { MacroInputChange, MacroSliderView } from '../../goals-page-lib/goals-page.models';
import { GoalsMacroSliderComponent } from '../macro-slider/goals-macro-slider.component';

@Component({
    selector: 'fd-goals-fiber-card',
    imports: [TranslatePipe, FdUiCardComponent, GoalsMacroSliderComponent],
    templateUrl: './goals-fiber-card.component.html',
    styleUrl: '../../goals-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GoalsFiberCardComponent {
    public readonly fiber = input<MacroSliderView | null>(null);

    public readonly macroInput = output<MacroInputChange>();
    public readonly macroSlider = output<MacroInputChange>();

    protected emitMacroInput(fiber: MacroSliderView, event: Event): void {
        if (fiber.key !== undefined) {
            this.macroInput.emit({ key: fiber.key, event });
        }
    }

    protected emitMacroSlider(fiber: MacroSliderView, event: Event): void {
        if (fiber.key !== undefined) {
            this.macroSlider.emit({ key: fiber.key, event });
        }
    }
}
