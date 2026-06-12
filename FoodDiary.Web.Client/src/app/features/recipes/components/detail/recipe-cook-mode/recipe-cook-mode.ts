import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';

import type { Recipe, RecipeIngredient, RecipeStep } from '../../../models/recipe.data';

const PERCENT_SCALE = 100;

type CookModeIngredientView = {
    name: string;
    amount: number;
    unitKey: string;
};

@Component({
    selector: 'fd-recipe-cook-mode',
    templateUrl: './recipe-cook-mode.html',
    styleUrls: ['./recipe-cook-mode.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslatePipe, FdUiButtonComponent],
})
export class RecipeCookModeComponent {
    public readonly recipe = input.required<Recipe>();
    public readonly addToMeal = output();

    protected readonly currentStepIndex = signal(0);
    protected readonly steps = computed(() => [...this.recipe().steps].sort((a, b) => a.stepNumber - b.stepNumber));
    protected readonly stepCount = computed(() => this.steps().length);
    protected readonly currentStep = computed<RecipeStep | null>(() => this.steps()[this.currentStepIndex()] ?? null);
    protected readonly progressPercent = computed(() => {
        const count = this.stepCount();
        return count > 0 ? ((this.currentStepIndex() + 1) / count) * PERCENT_SCALE : 0;
    });
    protected readonly ingredients = computed<CookModeIngredientView[]>(() =>
        this.buildIngredientViews(this.currentStep()?.ingredients ?? []),
    );
    protected readonly canGoBack = computed(() => this.currentStepIndex() > 0);
    protected readonly canGoNext = computed(() => this.currentStepIndex() < this.stepCount() - 1);
    protected readonly isDone = computed(() => this.stepCount() > 0 && !this.canGoNext());

    protected previousStep(): void {
        if (this.canGoBack()) {
            this.currentStepIndex.update(index => index - 1);
        }
    }

    protected nextStep(): void {
        if (this.canGoNext()) {
            this.currentStepIndex.update(index => index + 1);
        }
    }

    protected addRecipeToMeal(): void {
        this.addToMeal.emit();
    }

    private buildIngredientViews(ingredients: readonly RecipeIngredient[]): CookModeIngredientView[] {
        return ingredients.map(ingredient => ({
            name: ingredient.productName ?? ingredient.nestedRecipeName ?? '',
            amount: ingredient.amount,
            unitKey:
                ingredient.productBaseUnit !== null && ingredient.productBaseUnit !== undefined && ingredient.productBaseUnit.length > 0
                    ? `GENERAL.UNITS.${ingredient.productBaseUnit}`
                    : '',
        }));
    }
}
