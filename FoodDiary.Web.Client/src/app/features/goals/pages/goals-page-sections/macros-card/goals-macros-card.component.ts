import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiSelectComponent, type FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select.component';

import type { MacroKey, MacroPresetKey } from '../../../lib/goals.facade';
import type { MacroInputChange, MacroSliderView } from '../../goals-page-lib/goals-page.models';
import { GoalsMacroSliderComponent } from '../macro-slider/goals-macro-slider.component';

@Component({
    selector: 'fd-goals-macros-card',
    imports: [FormsModule, TranslatePipe, FdUiCardComponent, FdUiSelectComponent, GoalsMacroSliderComponent],
    templateUrl: './goals-macros-card.component.html',
    styleUrl: '../../goals-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GoalsMacrosCardComponent {
    public readonly macroPresetOptions = input.required<Array<FdUiSelectOption<MacroPresetKey>>>();
    public readonly selectedPreset = input.required<MacroPresetKey>();
    public readonly macros = input.required<MacroSliderView[]>();

    public readonly presetChange = output<MacroPresetKey | null>();
    public readonly macroInput = output<MacroInputChange>();
    public readonly macroSlider = output<MacroInputChange>();

    protected emitMacroInput(key: MacroKey | undefined, event: Event): void {
        if (key !== undefined) {
            this.macroInput.emit({ key, event });
        }
    }

    protected emitMacroSlider(key: MacroKey | undefined, event: Event): void {
        if (key !== undefined) {
            this.macroSlider.emit({ key, event });
        }
    }
}
