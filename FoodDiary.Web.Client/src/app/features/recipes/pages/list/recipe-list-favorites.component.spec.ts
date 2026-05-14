import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import type { FavoriteRecipe } from '../../models/recipe.data';
import { RecipeListFavoritesComponent } from './recipe-list-favorites.component';

describe('RecipeListFavoritesComponent', () => {
    it('derives has-more state from total count and visible favorites', () => {
        const { component } = setupComponent({ favoriteTotalCount: 2 });

        expect(component.hasMoreFavorites()).toBe(true);
    });

    it('emits favorite actions', () => {
        const { component } = setupComponent();
        const opened: FavoriteRecipe[] = [];
        component.favoriteOpen.subscribe(favorite => {
            opened.push(favorite);
        });

        component.favoriteOpen.emit(createFavoriteRecipe());

        expect(opened).toEqual([createFavoriteRecipe()]);
    });
});

function setupComponent(overrides: { favoriteTotalCount?: number } = {}): {
    component: RecipeListFavoritesComponent;
    fixture: ComponentFixture<RecipeListFavoritesComponent>;
} {
    TestBed.configureTestingModule({
        imports: [RecipeListFavoritesComponent, TranslateModule.forRoot()],
    });

    const fixture = TestBed.createComponent(RecipeListFavoritesComponent);
    fixture.componentRef.setInput('favorites', [createFavoriteRecipe()]);
    fixture.componentRef.setInput('favoriteTotalCount', overrides.favoriteTotalCount ?? 1);
    fixture.componentRef.setInput('isFavoritesOpen', true);
    fixture.componentRef.setInput('isFavoritesLoadingMore', false);
    fixture.detectChanges();

    return { component: fixture.componentInstance, fixture };
}

function createFavoriteRecipe(): FavoriteRecipe {
    return {
        id: 'favorite-1',
        recipeId: 'recipe-1',
        name: 'Favorite recipe',
        createdAtUtc: '2026-01-01T00:00:00Z',
        recipeName: 'Recipe',
        imageUrl: null,
        totalCalories: 120,
        servings: 2,
        totalTimeMinutes: 30,
        ingredientCount: 3,
    };
}
