import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import type { FavoriteRecipe } from '../../models/recipe.data';
import { FavoriteRecipeRowComponent } from './favorite-recipe-row';

const recipe: FavoriteRecipe = {
    id: 'f1',
    recipeId: 'r1',
    recipeName: 'Rice',
    createdAtUtc: '2026-01-01T00:00:00Z',
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
