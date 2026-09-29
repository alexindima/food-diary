import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';

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

    it('sends scaled quantities and free text, with no shopping action for private ingredients', () => {
        const recipe = publicRecipeFixture();
        recipe.steps[0].ingredients = [
            { name: 'Rice', productId: 'public-rice', amount: 100, unit: 'Gram', amountText: null, recipeId: null, isAvailable: true },
            { name: 'Salt', amount: null, unit: null, amountText: 'to taste', recipeId: null, isAvailable: true },
            { name: null, amount: null, unit: null, amountText: null, recipeId: null, isAvailable: false },
        ];
        const fixture = TestBed.createComponent(PublicIngredientsComponent);
        fixture.componentRef.setInput('recipe', recipe);
        fixture.componentRef.setInput('servings', recipe.servings * 2);
        const added = vi.fn();
        fixture.componentInstance.shoppingAdd.subscribe(added);
        fixture.detectChanges();
        const element = fixture.nativeElement as HTMLElement;
        const buttons = element.querySelectorAll<HTMLButtonElement>('[aria-label="PUBLIC_RECIPES.SHOPPING_ADD"]');
        expect(buttons).toHaveLength(2);
        buttons[0].click();
        buttons[1].click();
        expect(added).toHaveBeenNthCalledWith(1, {
            index: 0,
            item: { name: 'Rice', productId: 'public-rice', amount: 200, unit: 'G', note: null, isChecked: false },
        });
        expect(added).toHaveBeenNthCalledWith(2, {
            index: 1,
            item: { name: 'Salt', productId: null, amount: null, unit: null, note: 'to taste', isChecked: false },
        });
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

const DEFAULT_LIMIT = 50;
const LARGE_RECIPE_SERVINGS = 80;
it.each([2, LARGE_RECIPE_SERVINGS])('limits servings while preserving larger original recipes (%s)', original => {
    TestBed.configureTestingModule({
        imports: [PublicIngredientsComponent],
        providers: [provideRouter([]), provideTranslateTesting()],
    });
    const recipe = publicRecipeFixture();
    recipe.servings = original;
    const limit = Math.max(DEFAULT_LIMIT, original);
    const fixture = TestBed.createComponent(PublicIngredientsComponent);
    fixture.componentRef.setInput('recipe', recipe);
    fixture.componentRef.setInput('servings', limit - 1);
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;
    const increase = element.querySelector<HTMLButtonElement>('[aria-label="PUBLIC_RECIPES.MORE"]');
    increase?.click();
    fixture.detectChanges();
    expect(fixture.componentInstance.servings()).toBe(limit);
    expect(increase?.disabled).toBe(true);
    increase?.click();
    expect(fixture.componentInstance.servings()).toBe(limit);
});

it('hides shopping actions when all ingredients are unavailable', () => {
    TestBed.configureTestingModule({ imports: [PublicIngredientsComponent], providers: [provideRouter([]), provideTranslateTesting()] });
    const recipe = publicRecipeFixture();
    recipe.steps[0].ingredients = [{ name: null, amount: null, unit: null, amountText: null, recipeId: null, isAvailable: false }];
    const fixture = TestBed.createComponent(PublicIngredientsComponent);
    fixture.componentRef.setInput('recipe', recipe);
    fixture.componentRef.setInput('servings', recipe.servings);
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector<HTMLElement>('.shopping-toolbar')?.style.display).toBe('none');
});
