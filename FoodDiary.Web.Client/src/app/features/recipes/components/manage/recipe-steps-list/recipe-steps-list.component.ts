import { CdkDragDrop, DragDropModule, moveItemInArray } from '@angular/cdk/drag-drop';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FormArray, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';

import { FormGroupControls } from '../../../../../shared/lib/common.data';
import { StepFormData, StepFormValues } from '../recipe-manage.types';
import { RecipeStepCardComponent } from '../recipe-step-card/recipe-step-card.component';

export interface StepIngredientEvent {
    stepIndex: number;
    ingredientIndex: number;
}

@Component({
    selector: 'fd-recipe-steps-list',
    standalone: true,
    imports: [ReactiveFormsModule, TranslatePipe, DragDropModule, FdUiButtonComponent, RecipeStepCardComponent],
    templateUrl: './recipe-steps-list.component.html',
    styleUrls: ['./recipe-steps-list.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecipeStepsListComponent {
    public readonly stepsFormArray = input.required<FormArray<FormGroup<FormGroupControls<StepFormValues>>>>();
    public readonly expandedSteps = input.required<Set<number>>();

    public readonly addStep = output<void>();
    public readonly removeStep = output<number>();
    public readonly addIngredient = output<number>();
    public readonly removeIngredient = output<StepIngredientEvent>();
    public readonly selectProduct = output<StepIngredientEvent>();

    public isStepExpanded(index: number): boolean {
        return this.expandedSteps().has(index);
    }

    public toggleStepExpanded(index: number): void {
        const expanded = this.expandedSteps();
        if (expanded.has(index)) {
            expanded.delete(index);
        } else {
            expanded.add(index);
        }
    }

    public onStepDrop(event: CdkDragDrop<FormGroup<StepFormData>[]>): void {
        if (event.previousIndex === event.currentIndex) {
            return;
        }

        const steps = this.stepsFormArray();
        moveItemInArray(steps.controls, event.previousIndex, event.currentIndex);
        steps.updateValueAndValidity();
        steps.markAsDirty();
    }

    public onRemoveStep(index: number): void {
        this.removeStep.emit(index);
    }

    public onAddStep(): void {
        this.addStep.emit();
    }

    public onAddIngredient(stepIndex: number): void {
        this.addIngredient.emit(stepIndex);
    }

    public onRemoveIngredient(stepIndex: number, ingredientIndex: number): void {
        this.removeIngredient.emit({ stepIndex, ingredientIndex });
    }

    public onSelectProduct(stepIndex: number, ingredientIndex: number): void {
        this.selectProduct.emit({ stepIndex, ingredientIndex });
    }
}
