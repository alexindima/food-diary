import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import type { FieldTree } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import {
    FD_VALIDATION_ERRORS,
    FdUiFormErrorComponent,
    type FdValidationErrors,
    resolveSignalFormFieldError,
} from 'fd-ui-kit/form-error/fd-ui-form-error';
import { FdUiSegmentedToggleComponent } from 'fd-ui-kit/segmented-toggle/fd-ui-segmented-toggle';

import {
    NutritionEditorComponent,
    type NutritionEditorFieldErrors,
    type NutritionEditorSignalForm,
    type NutritionEditorWarning,
} from '../../../../../components/shared/nutrition-editor/nutrition-editor';
import { MANUAL_NUTRITION_MAX_CALORIES, MANUAL_NUTRITION_MAX_NUTRIENT } from '../../../../../shared/lib/nutrition.constants';
import type { CalorieMismatchWarning, ConsumptionFormValues, MacroBarState, NutritionMode } from '../meal-manage-lib/meal-manage.types';
import { buildMealNutritionModeOptions } from '../meal-manage-lib/meal-manage-options.mapper';

@Component({
    selector: 'fd-meal-nutrition-sidebar',
    templateUrl: './meal-nutrition-sidebar.html',
    styleUrls: ['../meal-manage-form.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
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
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly validationErrors = inject<FdValidationErrors>(FD_VALIDATION_ERRORS, { optional: true });

    public readonly consumptionForm = input.required<FieldTree<ConsumptionFormValues>>();
    public readonly macroBarState = input.required<MacroBarState>();
    public readonly nutritionMode = input.required<NutritionMode>();
    public readonly nutritionWarning = input.required<CalorieMismatchWarning | null>();
    public readonly caloriesError = input.required<string | null>();
    public readonly macrosError = input.required<string | null>();
    public readonly isEditMode = input.required<boolean>();
    public readonly isSubmitting = input(false);
    public readonly globalError = input.required<string | null>();

    public readonly nutritionModeChange = output<string>();
    public readonly cancelRequested = output();
    protected readonly maxCalories = MANUAL_NUTRITION_MAX_CALORIES;
    protected readonly maxNutrient = MANUAL_NUTRITION_MAX_NUTRIENT;
    private readonly activeLang = signal(this.translateService.getCurrentLang());
    protected readonly nutritionForm = computed<NutritionEditorSignalForm>(() => {
        const form = this.consumptionForm();
        return {
            calories: form.manualCalories,
            proteins: form.manualProteins,
            fats: form.manualFats,
            carbs: form.manualCarbs,
            fiber: form.manualFiber,
            alcohol: form.manualAlcohol,
        };
    });
    protected readonly nutritionModeOptions = computed(() => {
        this.activeLang();
        return buildMealNutritionModeOptions(this.translateService);
    });
    protected readonly nutritionEditorWarning = computed<NutritionEditorWarning | null>(() => {
        const warning = this.nutritionWarning();
        return warning === null ? null : { kind: 'caloriesMismatch', ...warning };
    });
    protected readonly nutritionFieldErrors = computed<NutritionEditorFieldErrors>(() => {
        if (this.nutritionMode() === 'auto') {
            return {};
        }

        const form = this.consumptionForm();
        return {
            calories: this.getFieldError(form.manualCalories),
            proteins: this.getFieldError(form.manualProteins),
            fats: this.getFieldError(form.manualFats),
            carbs: this.getFieldError(form.manualCarbs),
            fiber: this.getFieldError(form.manualFiber),
            alcohol: this.getFieldError(form.manualAlcohol),
        };
    });
    protected readonly isNutritionReadonly = computed(() => this.nutritionMode() === 'auto');
    protected readonly showManualNutritionHint = computed(() => !this.isNutritionReadonly());

    public constructor() {
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(event => {
            this.activeLang.set(event.lang);
        });
    }

    protected onNutritionModeChange(nextMode: string): void {
        this.nutritionModeChange.emit(nextMode);
    }

    protected onCancel(): void {
        this.cancelRequested.emit();
    }

    private getFieldError(field: NutritionEditorSignalForm[keyof NutritionEditorSignalForm]): string | null {
        if (field().errors()[0]?.kind === 'required') {
            return null;
        }

        return resolveSignalFormFieldError(field, this.validationErrors, this.translateService);
    }
}
