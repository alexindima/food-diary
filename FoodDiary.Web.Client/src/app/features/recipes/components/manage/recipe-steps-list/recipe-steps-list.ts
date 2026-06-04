import { type CdkDragDrop, DragDropModule } from '@angular/cdk/drag-drop';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';

import { RecipeStepCardComponent, type RecipeStepCardForm } from '../recipe-step-card/recipe-step-card';

export type StepIngredientEvent = {
    stepIndex: number;
    ingredientIndex: number;
};

export type StepDropEvent = {
    previousIndex: number;
    currentIndex: number;
};

export type RecipeStepListItem = {
    form: RecipeStepCardForm;
};

@Component({
    selector: 'fd-recipe-steps-list',
    imports: [TranslatePipe, DragDropModule, FdUiButtonComponent, RecipeStepCardComponent],
    templateUrl: './recipe-steps-list.html',
    styleUrls: ['./recipe-steps-list.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecipeStepsListComponent {
    public readonly steps = input.required<readonly RecipeStepListItem[]>();
    public readonly expandedSteps = input.required<ReadonlySet<number>>();
    public readonly stepsError = input<string | null>(null);

    public readonly addStep = output();
    public readonly removeStep = output<number>();
    public readonly stepDrop = output<StepDropEvent>();
    public readonly stepExpandedToggle = output<number>();
    public readonly addIngredient = output<number>();
    public readonly removeIngredient = output<StepIngredientEvent>();
    public readonly selectProduct = output<StepIngredientEvent>();

    protected isStepExpanded(index: number): boolean {
        return this.expandedSteps().has(index);
    }

    protected onToggleStepExpanded(index: number): void {
        this.stepExpandedToggle.emit(index);
    }

    protected onStepDrop(event: Pick<CdkDragDrop<readonly RecipeStepListItem[]>, 'previousIndex' | 'currentIndex'>): void {
        if (event.previousIndex === event.currentIndex) {
            return;
        }

        this.stepDrop.emit({
            previousIndex: event.previousIndex,
            currentIndex: event.currentIndex,
        });
    }

    protected onRemoveStep(index: number): void {
        this.removeStep.emit(index);
    }

    protected onAddStep(): void {
        this.addStep.emit();
    }

    protected onAddIngredient(stepIndex: number): void {
        this.addIngredient.emit(stepIndex);
    }

    protected onRemoveIngredient(stepIndex: number, ingredientIndex: number): void {
        this.removeIngredient.emit({ stepIndex, ingredientIndex });
    }

    protected onSelectProduct(stepIndex: number, ingredientIndex: number): void {
        this.selectProduct.emit({ stepIndex, ingredientIndex });
    }
}
