import type { Recipe } from '../../../models/recipe.data';
import type { IngredientFormValues, StepFormValues } from './recipe-manage.types';
import {
    createRecipeIngredientValue,
    createRecipeStepValue,
    mapRecipeStepToFormValue,
    type RecipeIngredientMappingLabels,
} from './recipe-manage-form.mapper';

type RecipeStepsReader = () => readonly StepFormValues[];
type RecipeStepsWriter = (steps: StepFormValues[]) => void;

export type RecipeStepIngredientIndex = {
    stepIndex: number;
    ingredientIndex: number;
};

export class RecipeStepFormManager {
    public readonly expandedSteps = new Set<number>();

    public constructor(
        private readonly getSteps: RecipeStepsReader,
        private readonly setSteps: RecipeStepsWriter,
        private readonly resolveLabels: () => RecipeIngredientMappingLabels,
    ) {}

    public addStep(step?: StepFormValues): void {
        const steps = [...this.getSteps(), createRecipeStepValue(step)];
        this.setSteps(steps);
        this.expandedSteps.add(steps.length - 1);
    }

    public removeStep(index: number): void {
        this.setSteps(this.getSteps().filter((_step, currentIndex) => currentIndex !== index));
        const nextExpanded = new Set<number>();
        this.expandedSteps.forEach(stepIndex => {
            if (stepIndex === index) {
                return;
            }
            nextExpanded.add(stepIndex > index ? stepIndex - 1 : stepIndex);
        });
        this.expandedSteps.clear();
        nextExpanded.forEach(stepIndex => this.expandedSteps.add(stepIndex));
    }

    public addIngredientToStep(stepIndex: number): void {
        this.updateStep(stepIndex, step => ({
            ...step,
            ingredients: [...step.ingredients, createRecipeIngredientValue()],
        }));
    }

    public removeIngredientFromStep({ stepIndex, ingredientIndex }: RecipeStepIngredientIndex): void {
        this.updateStep(stepIndex, step => ({
            ...step,
            ingredients: step.ingredients.filter((_ingredient, currentIndex) => currentIndex !== ingredientIndex),
        }));
    }

    public patchIngredient({ stepIndex, ingredientIndex }: RecipeStepIngredientIndex, patch: Partial<IngredientFormValues>): void {
        this.updateStep(stepIndex, step => ({
            ...step,
            ingredients: step.ingredients.map((ingredient, currentIndex) =>
                currentIndex === ingredientIndex
                    ? {
                          ...ingredient,
                          ...patch,
                      }
                    : ingredient,
            ),
        }));
    }

    public toggleStepExpanded(index: number): void {
        if (this.expandedSteps.has(index)) {
            this.expandedSteps.delete(index);
            return;
        }

        this.expandedSteps.add(index);
    }

    public resetSteps(): void {
        this.setSteps([]);
        this.expandedSteps.clear();
    }

    public populateRecipeSteps(recipe: Recipe): void {
        if (recipe.steps.length === 0) {
            this.addStep();
            return;
        }

        recipe.steps.forEach(step => {
            this.addStep(mapRecipeStepToFormValue(step, this.resolveLabels()));
        });
    }

    private updateStep(index: number, update: (step: StepFormValues) => StepFormValues): void {
        this.setSteps(this.getSteps().map((step, currentIndex) => (currentIndex === index ? update(step) : step)));
    }
}
