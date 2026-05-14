import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { RecipeVisibility } from '../../models/recipe.data';
import { RecipeEditComponent } from './recipe-edit.component';

describe('RecipeEditComponent', () => {
    it('accepts resolved recipe input', () => {
        TestBed.configureTestingModule({
            imports: [RecipeEditComponent],
        });
        TestBed.overrideComponent(RecipeEditComponent, {
            set: { template: '' },
        });

        const fixture = TestBed.createComponent(RecipeEditComponent);
        const recipe = {
            id: 'recipe-1',
            name: 'Recipe',
            servings: 2,
            visibility: RecipeVisibility.Private,
            usageCount: 0,
            createdAt: '2026-01-01T00:00:00Z',
            isOwnedByCurrentUser: true,
            isNutritionAutoCalculated: true,
            steps: [],
        };
        fixture.componentRef.setInput('recipe', recipe);
        fixture.detectChanges();

        expect(fixture.componentInstance.recipe()).toEqual(recipe);
    });
});
