import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { FormField } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiNutrientInputComponent } from 'fd-ui-kit/nutrient-input/fd-ui-nutrient-input';

import type {
    NutritionEditorSignalForm,
    NutritionMacroSegment,
    NutritionMacroState,
    NutritionMismatchWarning,
} from './nutrition-editor.types';
import { NutritionEditorMessagesComponent } from './nutrition-editor-messages';

export type {
    NutritionEditorSignalForm,
    NutritionFormModel,
    NutritionMacroSegment,
    NutritionMacroState,
    NutritionMismatchWarning,
} from './nutrition-editor.types';

@Component({
    selector: 'fd-nutrition-editor',
    imports: [CommonModule, FormField, TranslatePipe, FdUiNutrientInputComponent, NutritionEditorMessagesComponent],
    templateUrl: './nutrition-editor.html',
    styleUrls: ['./nutrition-editor.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NutritionEditorComponent {
    public readonly form = input<NutritionEditorSignalForm | null>(null);
    public readonly macroState = input.required<NutritionMacroState>();
    public readonly readonly = input(false);
    public readonly caloriesError = input<string | null>(null);
    public readonly macrosError = input<string | null>(null);
    public readonly showManualHint = input(false);
    public readonly manualHintKey = input('');
    public readonly warning = input<NutritionMismatchWarning | null>(null);
    protected readonly hasCaloriesError = computed(() => this.hasText(this.caloriesError()));

    protected readonly nutrientFillColors = {
        calories: 'var(--fd-color-nutrition-calories-fill)',
        proteins: 'var(--fd-color-nutrition-proteins-fill)',
        fats: 'var(--fd-color-nutrition-fats-fill)',
        carbs: 'var(--fd-color-nutrition-carbs-fill)',
        fiber: 'var(--fd-color-nutrition-fiber-fill)',
        alcohol: 'var(--fd-color-nutrition-alcohol-fill)',
    };
    protected readonly nutrientTextColors = {
        calories: 'var(--fd-color-nutrition-calories)',
        proteins: 'var(--fd-color-nutrition-proteins)',
        fats: 'var(--fd-color-nutrition-fats)',
        carbs: 'var(--fd-color-nutrition-carbs)',
        fiber: 'var(--fd-color-nutrition-fiber)',
    };
    protected readonly macroBarState = computed<NutritionMacroBarViewModel>(() => {
        const state = this.macroState();

        return {
            isEmpty: state.isEmpty,
            segments: state.segments.map(segment => ({
                ...segment,
                color: this.nutrientTextColors[segment.key],
            })),
        };
    });

    private hasText(value: string | null): boolean {
        return value !== null && value.trim().length > 0;
    }
}

type NutritionMacroBarViewModel = {
    isEmpty: boolean;
    segments: NutritionMacroSegmentViewModel[];
};

type NutritionMacroSegmentViewModel = {
    color: string;
} & NutritionMacroSegment;
