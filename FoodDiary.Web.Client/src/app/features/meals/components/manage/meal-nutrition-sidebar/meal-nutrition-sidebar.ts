import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import type { FieldTree } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { FdUiFormErrorComponent } from 'fd-ui-kit/form-error/fd-ui-form-error';
import { FdUiSegmentedToggleComponent } from 'fd-ui-kit/segmented-toggle/fd-ui-segmented-toggle';

import {
    NutritionEditorComponent,
    type NutritionEditorSignalForm,
    type NutritionEditorWarning,
} from '../../../../../components/shared/nutrition-editor/nutrition-editor';
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

    public readonly consumptionForm = input.required<FieldTree<ConsumptionFormValues>>();
    public readonly macroBarState = input.required<MacroBarState>();
    public readonly nutritionMode = input.required<NutritionMode>();
    public readonly nutritionWarning = input.required<CalorieMismatchWarning | null>();
    public readonly caloriesError = input.required<string | null>();
    public readonly macrosError = input.required<string | null>();
    public readonly isEditMode = input.required<boolean>();
    public readonly globalError = input.required<string | null>();

    public readonly nutritionModeChange = output<string>();
    public readonly cancelRequested = output();
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
}
