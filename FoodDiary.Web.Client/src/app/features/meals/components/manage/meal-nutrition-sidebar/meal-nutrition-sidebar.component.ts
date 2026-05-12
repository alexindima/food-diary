import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { type FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiFormErrorComponent } from 'fd-ui-kit/form-error/fd-ui-form-error.component';
import { FdUiSegmentedToggleComponent, type FdUiSegmentedToggleOption } from 'fd-ui-kit/segmented-toggle/fd-ui-segmented-toggle.component';

import {
    type NutritionControlNames,
    NutritionEditorComponent,
} from '../../../../../components/shared/nutrition-editor/nutrition-editor.component';
import type { Consumption } from '../../../models/meal.data';
import type { CalorieMismatchWarning, ConsumptionFormData, MacroBarState, NutritionMode } from '../base-meal-manage.types';

@Component({
    selector: 'fd-meal-nutrition-sidebar',
    templateUrl: './meal-nutrition-sidebar.component.html',
    styleUrls: ['../base-meal-manage.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        FdUiHintDirective,
        FdUiCardComponent,
        FdUiButtonComponent,
        FdUiSegmentedToggleComponent,
        FdUiFormErrorComponent,
        NutritionEditorComponent,
    ],
})
export class MealNutritionSidebarComponent {
    public readonly consumptionForm = input.required<FormGroup<ConsumptionFormData>>();
    public readonly nutritionControlNames = input.required<NutritionControlNames>();
    public readonly macroBarState = input.required<MacroBarState>();
    public readonly nutritionMode = input.required<NutritionMode>();
    public readonly nutritionModeOptions = input.required<FdUiSegmentedToggleOption[]>();
    public readonly nutritionWarning = input.required<CalorieMismatchWarning | null>();
    public readonly caloriesError = input.required<string | null>();
    public readonly macrosError = input.required<string | null>();
    public readonly consumption = input.required<Consumption | null>();
    public readonly globalError = input.required<string | null>();

    public readonly nutritionModeChange = output<string>();
    public readonly cancelRequested = output();
    public readonly isNutritionReadonly = computed(() => this.nutritionMode() === 'auto');
    public readonly showManualNutritionHint = computed(() => !this.isNutritionReadonly());

    public onNutritionModeChange(nextMode: string): void {
        this.nutritionModeChange.emit(nextMode);
    }

    public onCancel(): void {
        this.cancelRequested.emit();
    }
}
