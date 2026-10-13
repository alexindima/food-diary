import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import type { FavoriteRecipe } from '../../../../shared/models/recipe.data';
import { utcInstant } from '../../../../shared/models/semantics/date-value';
import { entityId } from '../../../../shared/models/semantics/entity-id';
import { FavoriteRecipeRowComponent } from './favorite-recipe-row';

const STORED_RECIPE_CALORIES = 2525;

const recipe: FavoriteRecipe = {
    id: entityId<'favorite-recipe'>('f1'),
    recipeId: entityId<'recipe'>('r1'),
    recipeName: 'Rice',
    createdAtUtc: utcInstant('2026-01-01T00:00:00Z'),
    servings: 2,
    ingredientCount: 1,
    ingredientNames: ['Rice'],
    totalCalories: 200,
};

function setup(overrides: Partial<FavoriteRecipe> = {}): ComponentFixture<FavoriteRecipeRowComponent> {
    TestBed.configureTestingModule({ imports: [FavoriteRecipeRowComponent], providers: [provideTranslateTesting()] });
    const fixture = TestBed.createComponent(FavoriteRecipeRowComponent);
    fixture.componentRef.setInput('recipe', { ...recipe, ...overrides });
    fixture.detectChanges();
    return fixture;
}

describe('FavoriteRecipeRowComponent', () => {
    it('shows per-serving calories and macros without changing the favorite passed to meal actions', () => {
        const fixture = setup({
            servings: 5,
            totalCalories: STORED_RECIPE_CALORIES,
            totalProteins: 180,
            totalFats: 235,
            totalCarbs: 25,
            totalFiber: 5,
        });
        const text = (fixture.nativeElement as HTMLElement).querySelector('.favorite-row__calories')?.textContent;
        expect(text).toContain('505');
        expect(text).toContain('GENERAL.UNITS.KCAL_PER_SERVING');
        expect(fixture.componentInstance['nutrition']()).toEqual({ proteins: 36, fats: 47, carbs: 5, fiber: 1 });
        const add = vi.fn();
        fixture.componentInstance.add.subscribe(add);
        [...(fixture.nativeElement as HTMLElement).querySelectorAll<HTMLButtonElement>('button')]
            .find(button => button.textContent.includes('RECIPE_FAVORITES.ADD'))
            ?.click();
        expect(add).toHaveBeenCalledExactlyOnceWith(fixture.componentInstance.recipe());
        expect(fixture.componentInstance.recipe().totalCalories).toBe(STORED_RECIPE_CALORIES);
    });

    it.each([null, undefined, '', '   '])('uses recipe name when favorite name is empty: %s', name => {
        const fixture = setup({ name });
        expect((fixture.nativeElement as HTMLElement).querySelector('strong')?.textContent).toBe('Rice');
    });

    it('trims a custom favorite name and exposes ingredient composition', () => {
        const fixture = setup({ name: '  Lunch  ', ingredientNames: ['Rice', 'Carrots'] });
        expect((fixture.nativeElement as HTMLElement).querySelector('strong')?.textContent).toBe('Lunch');
        expect((fixture.nativeElement as HTMLElement).querySelector<HTMLElement>('.favorite-row__composition')?.title).toBe(
            'Rice, Carrots',
        );
    });

    it('replaces a failed photo with a placeholder and permits a new photo', () => {
        const fixture = setup({ imageUrl: '/broken.jpg' });
        (fixture.nativeElement as HTMLElement).querySelector('img')?.dispatchEvent(new Event('error'));
        fixture.detectChanges();
        expect((fixture.nativeElement as HTMLElement).querySelector('img')).toBeNull();
        expect((fixture.nativeElement as HTMLElement).querySelector('.favorite-row__media fd-ui-icon')).not.toBeNull();
        fixture.componentRef.setInput('recipe', { ...recipe, imageUrl: '/new.jpg' });
        fixture.detectChanges();
        expect((fixture.nativeElement as HTMLElement).querySelector('img')?.getAttribute('src')).toBe('/new.jpg');
    });

    it('emits add and remove only for enabled buttons', () => {
        const fixture = setup();
        const add = vi.fn();
        const remove = vi.fn();
        fixture.componentInstance.add.subscribe(add);
        fixture.componentInstance.remove.subscribe(remove);
        const buttons: HTMLButtonElement[] = [...(fixture.nativeElement as HTMLElement).querySelectorAll('button')];
        buttons.forEach(button => {
            button.click();
        });
        expect(add).toHaveBeenCalledExactlyOnceWith(recipe);
        expect(remove).toHaveBeenCalledExactlyOnceWith(recipe);
        fixture.componentRef.setInput('disabled', true);
        fixture.detectChanges();
        buttons.forEach(button => {
            button.click();
        });
        expect(add).toHaveBeenCalledTimes(1);
        expect(remove).toHaveBeenCalledTimes(1);
    });

    it('offers accessible retry after restoration fails', () => {
        const fixture = setup();
        fixture.componentRef.setInput('removed', true);
        fixture.componentRef.setInput('restoreFailed', true);
        fixture.detectChanges();
        expect((fixture.nativeElement as HTMLElement).querySelector('[role="status"]')?.textContent).toContain(
            'RECIPE_FAVORITES.RESTORE_ERROR',
        );
        const restore = vi.fn();
        fixture.componentInstance.restore.subscribe(restore);
        (fixture.nativeElement as HTMLElement).querySelector('button')?.click();
        expect(restore).toHaveBeenCalledExactlyOnceWith(recipe);
    });
});
