import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';
import type { FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select';
import { FdUiSelectComponent } from 'fd-ui-kit/select/fd-ui-select';

import type { MacroKey, MacroPresetKey } from '../../lib/goals.facade';
import type { GoalsMacroDraft } from './goals-page-v2.models';

@Component({
    selector: 'fd-goals-nutrition-card-v2',
    imports: [TranslatePipe, FdUiSelectComponent, FdUiIconComponent],
    templateUrl: './goals-nutrition-card.html',
    styleUrl: './goals-page-v2.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GoalsNutritionCardV2Component {
    public readonly calories = input.required<number>();
    public readonly macros = input.required<GoalsMacroDraft[]>();
    public readonly preset = input.required<MacroPresetKey>();
    public readonly presetOptions = input.required<Array<FdUiSelectOption<MacroPresetKey>>>();
    public readonly caloriesChange = output<number>();
    public readonly macroChange = output<{ key: MacroKey; value: number }>();
    public readonly presetChange = output<MacroPresetKey>();

    protected emitNumber(event: Event, emit: (value: number) => void): void {
        if (event.target instanceof HTMLInputElement) {
            emit(Number(event.target.value));
        }
    }
}
