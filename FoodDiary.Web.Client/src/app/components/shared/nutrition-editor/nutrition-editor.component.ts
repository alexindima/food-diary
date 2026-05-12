import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { type FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiNutrientInputComponent } from 'fd-ui-kit/nutrient-input/fd-ui-nutrient-input.component';

export type NutritionControlNames = {
    calories: string;
    proteins: string;
    fats: string;
    carbs: string;
    fiber: string;
    alcohol: string;
};

export type NutritionMacroSegment = {
    key: 'proteins' | 'fats' | 'carbs';
    percent: number;
};

export type NutritionMacroState = {
    isEmpty: boolean;
    segments: NutritionMacroSegment[];
};

export type NutritionMismatchWarning = {
    expectedCalories: number;
    actualCalories: number;
};

@Component({
    selector: 'fd-nutrition-editor',
    standalone: true,
    imports: [CommonModule, ReactiveFormsModule, TranslatePipe, FdUiNutrientInputComponent],
    templateUrl: './nutrition-editor.component.html',
    styleUrls: ['./nutrition-editor.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NutritionEditorComponent {
    public readonly formGroup = input.required<FormGroup>();
    public readonly controlNames = input.required<NutritionControlNames>();
    public readonly macroState = input.required<NutritionMacroState>();
    public readonly readonly = input(false);
    public readonly caloriesError = input<string | null>(null);
    public readonly macrosError = input<string | null>(null);
    public readonly showManualHint = input(false);
    public readonly manualHintKey = input('');
    public readonly warning = input<NutritionMismatchWarning | null>(null);

    public readonly nutrientFillColors = {
        calories: 'var(--fd-color-nutrition-calories-fill)',
        proteins: 'var(--fd-color-nutrition-proteins-fill)',
        fats: 'var(--fd-color-nutrition-fats-fill)',
        carbs: 'var(--fd-color-nutrition-carbs-fill)',
        fiber: 'var(--fd-color-nutrition-fiber-fill)',
        alcohol: 'var(--fd-color-nutrition-alcohol-fill)',
    };
    public readonly nutrientTextColors = {
        calories: 'var(--fd-color-nutrition-calories)',
        proteins: 'var(--fd-color-nutrition-proteins)',
        fats: 'var(--fd-color-nutrition-fats)',
        carbs: 'var(--fd-color-nutrition-carbs)',
        fiber: 'var(--fd-color-nutrition-fiber)',
    };
    public readonly macroBarState = computed<NutritionMacroBarViewModel>(() => {
        const state = this.macroState();

        return {
            isEmpty: state.isEmpty,
            segments: state.segments.map(segment => ({
                ...segment,
                color: this.nutrientTextColors[segment.key],
            })),
        };
    });
}

type NutritionMacroBarViewModel = {
    isEmpty: boolean;
    segments: NutritionMacroSegmentViewModel[];
};

type NutritionMacroSegmentViewModel = {
    color: string;
} & NutritionMacroSegment;
