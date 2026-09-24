import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import type { StepFormValues } from '../recipe-manage-lib/recipe-manage.types';
import { createRecipeStepValue } from '../recipe-manage-lib/recipe-manage-form.mapper';
import { RecipeStepCardComponent } from '../recipe-step-card/recipe-step-card';
import { type RecipeStepListItem, RecipeStepsListComponent } from './recipe-steps-list';

describe('RecipeStepsListComponent', () => {
    it('checks expanded state from readonly expanded steps input', () => {
        const { component } = setupComponent(new Set([0]));

        expect(component['isStepExpanded'](0)).toBe(true);
        expect(component['isStepExpanded'](1)).toBe(false);
    });

    it('emits step toggle instead of mutating expanded steps input', () => {
        const expandedSteps = new Set([0]);
        const { component } = setupComponent(expandedSteps);
        const toggled: number[] = [];
        component['stepExpandedToggle'].subscribe(index => {
            toggled.push(index);
        });

        component['onToggleStepExpanded'](0);

        expect(toggled).toEqual([0]);
        expect(expandedSteps.has(0)).toBe(true);
    });

    it('emits ingredient events with step and ingredient indexes', () => {
        const { component } = setupComponent(new Set([0]));
        const removed: Array<{ stepIndex: number; ingredientIndex: number }> = [];
        component['removeIngredient'].subscribe(event => {
            removed.push(event);
        });

        component['onRemoveIngredient'](1, 2);

        expect(removed).toEqual([{ stepIndex: 1, ingredientIndex: 2 }]);
    });

    it('emits ingredient selection events with requested item type', () => {
        const { component } = setupComponent(new Set([0]));
        const selected: Array<{ stepIndex: number; ingredientIndex: number; itemType: string }> = [];
        component['selectProduct'].subscribe(event => {
            selected.push(event);
        });

        component['onSelectProduct'](1, { ingredientIndex: 2, itemType: 'Recipe' });

        expect(selected).toEqual([{ stepIndex: 1, ingredientIndex: 2, itemType: 'Recipe' }]);
    });

    it('emits step drop event without mutating supplied steps', () => {
        const { component } = setupComponent(new Set([0]));
        const handler = vi.fn();
        component['stepDrop'].subscribe(handler);

        const dropEvent = {
            previousIndex: 0,
            currentIndex: 1,
        };

        component['onStepDrop'](dropEvent);

        expect(handler).toHaveBeenCalledWith({ previousIndex: 0, currentIndex: 1 });
    });
});

function setupComponent(expandedSteps: ReadonlySet<number>): {
    component: RecipeStepsListComponent;
    fixture: ComponentFixture<RecipeStepsListComponent>;
} {
    TestBed.configureTestingModule({
        imports: [RecipeStepsListComponent],
        providers: [provideTranslateTesting()],
    });

    const fixture = TestBed.createComponent(RecipeStepsListComponent);
    fixture.componentRef.setInput('steps', [createRecipeStepListItem(), createRecipeStepListItem()]);
    fixture.componentRef.setInput('expandedSteps', expandedSteps);
    fixture.componentRef.setInput('stepsError', null);
    fixture.detectChanges();

    return { component: fixture.componentInstance, fixture };
}

function createRecipeStepListItem(step: StepFormValues = createRecipeStepValue()): RecipeStepListItem {
    return {
        state: {
            title: { value: step.title, error: null },
            imageUrl: { value: step.imageUrl, error: null },
            description: { value: step.description, error: null },
            ingredients: step.ingredients.map(ingredient => ({
                amount: { value: ingredient.amount, error: null },
                food: ingredient.food,
                foodName: { value: ingredient.foodName, error: null },
                nestedRecipeId: ingredient.nestedRecipeId,
            })),
        },
    };
}

describe('RecipeStepsListComponent child event wiring', () => {
    it('routes edited fields from the second step with its actual index', () => {
        const { component, fixture } = setupComponent(new Set([0, 1]));
        const child: RecipeStepCardComponent = fixture.debugElement
            .queryAll(By.directive(RecipeStepCardComponent))[1]
            .injector.get(RecipeStepCardComponent);
        const title = vi.fn();
        const image = vi.fn();
        const description = vi.fn();
        const amount = vi.fn();
        const added = vi.fn();
        const removed = vi.fn();
        component.stepTitleChange.subscribe(title);
        component.stepImageChange.subscribe(image);
        component.stepDescriptionChange.subscribe(description);
        component.ingredientAmountChange.subscribe(amount);
        component.addIngredient.subscribe(added);
        component.removeStep.subscribe(removed);
        child.stepTitleChange.emit('Bake');
        child.stepImageChange.emit(null);
        child.stepDescriptionChange.emit('Bake until ready');
        child.ingredientAmountChange.emit({ ingredientIndex: 0, amount: 25 });
        child.addIngredient.emit();
        child.removeStep.emit();
        expect(title).toHaveBeenCalledExactlyOnceWith({ stepIndex: 1, value: 'Bake' });
        expect(image).toHaveBeenCalledExactlyOnceWith({ stepIndex: 1, value: null });
        expect(description).toHaveBeenCalledExactlyOnceWith({ stepIndex: 1, value: 'Bake until ready' });
        expect(amount).toHaveBeenCalledExactlyOnceWith({ stepIndex: 1, ingredientIndex: 0, amount: 25 });
        expect(added).toHaveBeenCalledExactlyOnceWith(1);
        expect(removed).toHaveBeenCalledExactlyOnceWith(1);
    });

    it('ignores a drop that leaves the order unchanged', () => {
        const { component } = setupComponent(new Set([0]));
        const dropped = vi.fn();
        component.stepDrop.subscribe(dropped);
        component['onStepDrop']({ previousIndex: 0, currentIndex: 0 });
        expect(dropped).not.toHaveBeenCalled();
    });
});
