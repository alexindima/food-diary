import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import { type Recipe, RecipeVisibility } from '../../../models/recipe.data';
import { RecipeSelectDialogContentComponent } from './recipe-select-dialog-content.component';

describe('RecipeSelectDialogContentComponent', () => {
    it('renders recipe rows and emits selected recipe', () => {
        const recipe = createRecipe();
        const { component, fixture } = setupComponent([{ recipe, imageUrl: undefined }]);
        const selected: Recipe[] = [];
        component.recipeSelected.subscribe(value => {
            selected.push(value);
        });

        const button = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('.recipe-select__item');
        button?.click();

        expect(getText(fixture)).toContain('Recipe');
        expect(selected).toEqual([recipe]);
    });

    it('renders no-results state when loaded list is empty', () => {
        const { fixture } = setupComponent([]);

        expect(getText(fixture)).toContain('RECIPE_SELECT_DIALOG.NO_RECIPES_FOUND');
    });

    it('renders loader while loading', () => {
        const { fixture } = setupComponent([], true);

        expect((fixture.nativeElement as HTMLElement).querySelector('.recipe-select__loader')).not.toBeNull();
    });
});

function setupComponent(
    items: ReadonlyArray<{ recipe: Recipe; imageUrl: string | undefined }>,
    isLoading = false,
): { component: RecipeSelectDialogContentComponent; fixture: ComponentFixture<RecipeSelectDialogContentComponent> } {
    TestBed.configureTestingModule({
        imports: [RecipeSelectDialogContentComponent, TranslateModule.forRoot()],
    });

    const fixture = TestBed.createComponent(RecipeSelectDialogContentComponent);
    fixture.componentRef.setInput('items', items);
    fixture.componentRef.setInput('isLoading', isLoading);
    fixture.detectChanges();

    return { component: fixture.componentInstance, fixture };
}

function getText(fixture: ComponentFixture<RecipeSelectDialogContentComponent>): string {
    return (fixture.nativeElement as HTMLElement).textContent;
}

function createRecipe(): Recipe {
    return {
        id: 'recipe-1',
        name: 'Recipe',
        category: 'Dinner',
        totalCalories: 240,
        servings: 2,
        visibility: RecipeVisibility.Private,
        usageCount: 0,
        createdAt: '2026-01-01T00:00:00Z',
        isOwnedByCurrentUser: true,
        isNutritionAutoCalculated: true,
        steps: [],
    };
}
