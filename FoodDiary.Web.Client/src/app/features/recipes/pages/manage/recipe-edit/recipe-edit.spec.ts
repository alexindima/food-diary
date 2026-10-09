import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { RecipeVisibility } from '../../../../../shared/models/recipe.data';
import { utcInstant } from '../../../../../shared/models/semantics/date-value';
import { entityId } from '../../../../../shared/models/semantics/entity-id';
import { RecipeEditComponent } from './recipe-edit';

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
            id: entityId<'recipe'>('recipe-1'),
            name: 'Recipe',
            servings: 2,
            visibility: RecipeVisibility.Private,
            usageCount: 0,
            createdAt: utcInstant('2026-01-01T00:00:00Z'),
            isOwnedByCurrentUser: true,
            isNutritionAutoCalculated: true,
            steps: [],
        };
        fixture.componentRef.setInput('recipe', recipe);
        fixture.detectChanges();

        expect(fixture.componentInstance['recipe']()).toEqual(recipe);
    });
});
