import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { publicRecipeFixture } from '../../lib/public-recipe.test-helper';
import { PublicStepsComponent } from './public-steps';

describe('public cooking mode', () => {
    beforeEach(() => TestBed.configureTestingModule({ imports: [PublicStepsComponent], providers: [provideTranslateTesting()] }));
    it('switches between all instructions and one step without losing the recipe', () => {
        const recipe = publicRecipeFixture();
        recipe.steps[0].ingredients = [
            { name: 'Water', amount: 200, unit: 'ml', amountText: null, recipeId: null, isAvailable: true },
            { name: null, amount: null, unit: null, amountText: null, recipeId: null, isAvailable: false },
        ];
        recipe.steps.push({ stepNumber: 2, title: null, instruction: 'Serve', images: [], ingredients: [] });
        const fixture = TestBed.createComponent(PublicStepsComponent);
        fixture.componentRef.setInput('recipe', recipe);
        fixture.detectChanges();
        const element: unknown = fixture.nativeElement;
        if (!(element instanceof HTMLElement)) {
            throw new Error('Expected component element');
        }
        expect(element.querySelectorAll('article')).toHaveLength(2);
        expect(element.querySelector('.step-ingredients')?.textContent).toContain('Water');
        expect(element.querySelector('.step-ingredients')?.textContent).not.toContain('PUBLIC_RECIPES.PRIVATE_INGREDIENT');
        element.querySelector<HTMLButtonElement>('button')?.click();
        fixture.detectChanges();
        expect(element.querySelectorAll('article')).toHaveLength(1);
        expect(element.querySelector('article')?.textContent).toContain('Boil');
        element.querySelectorAll<HTMLButtonElement>('.step-navigation button')[1].click();
        fixture.detectChanges();
        expect(element.querySelector('article')?.textContent).toContain('Serve');
        expect(element.querySelectorAll<HTMLButtonElement>('.step-navigation button')[1].disabled).toBe(true);
    });
});
