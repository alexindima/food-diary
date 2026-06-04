import type { Recipe } from '../../../models/recipe.data';
import type { StepFormValues } from './recipe-manage.types';
import {
    createRecipeIngredientGroup,
    createRecipeStepGroup,
    mapRecipeStepToFormValue,
    type RecipeIngredientMappingLabels,
} from './recipe-manage-form.mapper';

type RecipeStepGroup = ReturnType<typeof createRecipeStepGroup>;
type RecipeIngredientGroup = ReturnType<typeof createRecipeIngredientGroup>;
type RecipeStepsArray = {
    length: number;
    at: (index: number) => RecipeStepGroup;
    push: (step: RecipeStepGroup) => void;
    removeAt: (index: number) => void;
};

export type RecipeStepIngredientIndex = {
    stepIndex: number;
    ingredientIndex: number;
};

export class RecipeStepFormManager {
    public readonly expandedSteps = new Set<number>();

    public constructor(
        private readonly steps: RecipeStepsArray,
        private readonly resolveLabels: () => RecipeIngredientMappingLabels,
    ) {}

    public addStep(step?: StepFormValues): void {
        this.steps.push(createRecipeStepGroup(step));
        this.expandedSteps.add(this.steps.length - 1);
    }

    public removeStep(index: number): void {
        this.steps.removeAt(index);
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
        this.steps.at(stepIndex).controls.ingredients.push(createRecipeIngredientGroup());
    }

    public removeIngredientFromStep({ stepIndex, ingredientIndex }: RecipeStepIngredientIndex): void {
        this.steps.at(stepIndex).controls.ingredients.removeAt(ingredientIndex);
    }

    public getIngredientGroup({ stepIndex, ingredientIndex }: RecipeStepIngredientIndex): RecipeIngredientGroup {
        return this.steps.at(stepIndex).controls.ingredients.at(ingredientIndex);
    }

    public toggleStepExpanded(index: number): void {
        if (this.expandedSteps.has(index)) {
            this.expandedSteps.delete(index);
            return;
        }

        this.expandedSteps.add(index);
    }

    public resetSteps(): void {
        while (this.steps.length > 0) {
            this.steps.removeAt(0);
        }
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
}
