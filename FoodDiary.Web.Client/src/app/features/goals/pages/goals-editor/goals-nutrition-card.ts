import { UpperCasePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, model, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiFormErrorComponent } from 'fd-ui-kit/form-error/fd-ui-form-error';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';
import type { FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select';
import { FdUiSelectComponent } from 'fd-ui-kit/select/fd-ui-select';

import type { MacroKey, MacroPresetKey } from '../../lib/goals.facade';
import { type GoalNumberKey, type GoalsMacroDraft, isGoalNumberValid } from './goals-editor.models';
import { GoalsNumberInputDirective } from './goals-number-input.directive';

@Component({
    selector: 'fd-goals-nutrition-card',
    imports: [UpperCasePipe, TranslatePipe, FdUiSelectComponent, FdUiIconComponent, FdUiFormErrorComponent, GoalsNumberInputDirective],
    templateUrl: './goals-nutrition-card.html',
    styleUrl: './goals-editor.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GoalsNutritionCardComponent {
    public readonly calories = input.required<number>();
    public readonly macros = input.required<GoalsMacroDraft[]>();
    public readonly incompleteNumbers = input.required<ReadonlySet<GoalNumberKey>>();
    public readonly preset = model.required<MacroPresetKey>();
    public readonly presetOptions = input.required<Array<FdUiSelectOption<MacroPresetKey>>>();

    public readonly caloriesChange = output<number | null>();
    public readonly macroChange = output<{ key: MacroKey; value: number | null }>();
    protected readonly caloriesIncomplete = computed(() => this.incompleteNumbers().has('calories'));
    protected readonly caloriesInvalid = computed(() => this.caloriesIncomplete() || !isGoalNumberValid(this.calories()));
    protected readonly editableMacros = computed(() =>
        this.macros().map(macro => {
            const incomplete = this.incompleteNumbers().has(macro.key);
            return { ...macro, incomplete, invalid: incomplete || !isGoalNumberValid(macro.value) };
        }),
    );
}
