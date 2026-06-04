import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import type { StepFormValues } from '../recipe-manage-lib/recipe-manage.types';
import { createRecipeStepValue } from '../recipe-manage-lib/recipe-manage-form.mapper';
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
        imports: [RecipeStepsListComponent, TranslateModule.forRoot()],
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
