import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

type NutritionMismatchWarning = {
    kind: 'caloriesMismatch';
    expectedCalories: number;
    actualCalories: number;
};

type NutritionTextWarning = {
    kind: 'text';
    messageKey: string;
    params?: Record<string, number | string>;
};

type NutritionEditorWarning = NutritionMismatchWarning | NutritionTextWarning;
type NutritionEditorField = 'calories' | 'proteins' | 'fats' | 'carbs' | 'fiber' | 'alcohol';
type NutritionEditorFieldErrors = Partial<Record<NutritionEditorField, string | null>>;

type NutritionEditorFieldError = {
    labelKey: string;
    message: string;
};

type NutritionEditorWarningViewModel = {
    messageKey: string;
    params?: Record<string, number | string>;
};

const nutritionEditorFieldOrder: NutritionEditorField[] = ['calories', 'proteins', 'fats', 'carbs', 'fiber', 'alcohol'];
const nutritionEditorFieldLabelKeys: Record<NutritionEditorField, string> = {
    calories: 'NUTRITION_EDITOR.FIELD_LABELS.CALORIES',
    proteins: 'NUTRITION_EDITOR.FIELD_LABELS.PROTEINS',
    fats: 'NUTRITION_EDITOR.FIELD_LABELS.FATS',
    carbs: 'NUTRITION_EDITOR.FIELD_LABELS.CARBS',
    fiber: 'NUTRITION_EDITOR.FIELD_LABELS.FIBER',
    alcohol: 'NUTRITION_EDITOR.FIELD_LABELS.ALCOHOL',
};

@Component({
    selector: 'fd-nutrition-editor-messages',
    imports: [TranslatePipe],
    templateUrl: './nutrition-editor-messages.html',
    styleUrls: ['./nutrition-editor-messages.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NutritionEditorMessagesComponent {
    public readonly fieldErrors = input<NutritionEditorFieldErrors>({});
    public readonly macrosError = input<string | null>(null);
    public readonly showManualHint = input(false);
    public readonly manualHintKey = input('');
    public readonly warning = input<NutritionEditorWarning | null>(null);

    protected readonly hasMacrosError = computed(() => this.hasText(this.macrosError()));
    protected readonly hasManualHint = computed(() => this.showManualHint() && this.manualHintKey().trim().length > 0);
    protected readonly warningMessage = computed<NutritionEditorWarningViewModel | null>(() => {
        const warning = this.warning();

        if (warning === null) {
            return null;
        }

        if (warning.kind === 'text') {
            return {
                messageKey: warning.messageKey,
                params: warning.params,
            };
        }

        return {
            messageKey: 'NUTRITION_EDITOR.CALORIES_MISMATCH_WARNING',
            params: {
                expected: warning.expectedCalories,
                actual: warning.actualCalories,
            },
        };
    });
    protected readonly fieldErrorsList = computed<NutritionEditorFieldError[]>(() => {
        const fieldErrors = this.fieldErrors();

        return nutritionEditorFieldOrder.flatMap(field => {
            const message = fieldErrors[field]?.trim() ?? '';

            return this.hasText(message)
                ? [
                      {
                          labelKey: nutritionEditorFieldLabelKeys[field],
                          message,
                      },
                  ]
                : [];
        });
    });

    private hasText(value: string | null): boolean {
        return value !== null && value.trim().length > 0;
    }
}
