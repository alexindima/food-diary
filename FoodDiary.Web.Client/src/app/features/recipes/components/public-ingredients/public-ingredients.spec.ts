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
        fixture.detectChanges();
        const element: unknown = fixture.nativeElement;
        if (!(element instanceof HTMLElement)) {
            throw new Error('Expected component element');
        }
        const buttons = element.querySelectorAll<HTMLButtonElement>('button');
        buttons[1].click();
        fixture.detectChanges();
        expect(element.textContent).toContain('150');
        expect(element.textContent).toContain('to taste');
        expect(element.textContent).toContain('PUBLIC_RECIPES.PRIVATE_INGREDIENT');
        expect(element.querySelectorAll('li a')).toHaveLength(0);
        buttons[0].click();
        buttons[0].click();
        fixture.detectChanges();
        expect(buttons[0].disabled).toBe(true);
    });
});
