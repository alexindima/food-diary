import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { publicRecipeFixture } from '../../lib/public-recipe.test-helper';
import { PublicIngredientsComponent } from './public-ingredients';

describe('public ingredient quantities', () => {
    beforeEach(() =>
        TestBed.configureTestingModule({
            imports: [PublicIngredientsComponent],
            providers: [provideRouter([]), provideTranslateTesting()],
        }),
    );
    it('scales numbers, preserves free text and does not link hidden ingredients', () => {
        const recipe = publicRecipeFixture();
        recipe.steps[0].ingredients = [
            { name: 'Rice', amount: 100, unit: 'g', amountText: null, recipeId: null, isAvailable: true },
            { name: 'Salt', amount: null, unit: null, amountText: 'to taste', recipeId: null, isAvailable: true },
            { name: null, amount: null, unit: null, amountText: null, recipeId: null, isAvailable: false },
        ];
        const fixture = TestBed.createComponent(PublicIngredientsComponent);
        fixture.componentRef.setInput('recipe', recipe);
        fixture.componentRef.setInput('servings', recipe.servings);
        fixture.detectChanges();
        const element: unknown = fixture.nativeElement;
        if (!(element instanceof HTMLElement)) {
            throw new Error('Expected component element');
        }
        const decrease = element.querySelector<HTMLButtonElement>('[aria-label="PUBLIC_RECIPES.FEWER"]');
        const increase = element.querySelector<HTMLButtonElement>('[aria-label="PUBLIC_RECIPES.MORE"]');
        increase?.click();
        fixture.detectChanges();
        expect(element.textContent).toContain('150');
        expect(element.textContent).toContain('to taste');
        expect(element.textContent).toContain('PUBLIC_RECIPES.PRIVATE_INGREDIENT');
        expect(element.querySelectorAll('li a')).toHaveLength(0);
        decrease?.click();
        decrease?.click();
        fixture.detectChanges();
        expect(decrease?.disabled).toBe(true);
    });

    it('hides the portions hint when the recipe has no ingredients', () => {
        const recipe = publicRecipeFixture();
        const fixture = TestBed.createComponent(PublicIngredientsComponent);
        fixture.componentRef.setInput('recipe', recipe);
        fixture.componentRef.setInput('servings', recipe.servings);
        fixture.detectChanges();
        const element = fixture.nativeElement as HTMLElement;
        expect(element.querySelector('[aria-label="PUBLIC_RECIPES.PORTIONS_HINT"]')).toBeNull();
        expect(element.textContent).toContain('PUBLIC_RECIPES.NO_INGREDIENTS');
    });
});
