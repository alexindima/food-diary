import { UpperCasePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, model, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiFormErrorComponent } from 'fd-ui-kit/form-error/fd-ui-form-error';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';
import type { FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select';
import { FdUiSelectComponent } from 'fd-ui-kit/select/fd-ui-select';

import type { MacroKey, MacroPresetKey } from '../../lib/goals.facade';
import { type GoalsMacroDraft, isGoalNumberValid } from './goals-editor.models';

@Component({
    selector: 'fd-goals-nutrition-card',
    imports: [UpperCasePipe, TranslatePipe, FdUiSelectComponent, FdUiIconComponent, FdUiFormErrorComponent],
    templateUrl: './goals-nutrition-card.html',
    styleUrl: './goals-editor.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GoalsNutritionCardComponent {
    public readonly calories = model.required<number>();
    public readonly macros = input.required<GoalsMacroDraft[]>();
    public readonly preset = model.required<MacroPresetKey>();
    public readonly presetOptions = input.required<Array<FdUiSelectOption<MacroPresetKey>>>();

    public readonly macroChange = output<{ key: MacroKey; value: number }>();
    protected readonly caloriesInvalid = computed(() => !isGoalNumberValid(this.calories()));
    protected readonly editableMacros = computed(() =>
        this.macros().map(macro => ({ ...macro, invalid: !isGoalNumberValid(macro.value) })),
    );

    protected emitNumber(event: Event, emit: (value: number) => void): void {
        if (event.target instanceof HTMLInputElement) {
            emit(Number(event.target.value));
        }
    }
}
