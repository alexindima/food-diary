import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../testing/translate-testing.module';
import { RecipeCardComponent } from '../../../../../../components/shared/recipe-card/recipe-card';
import { AuthService } from '../../../../../../services/auth.service';
import type { FavoriteRecipe } from '../../../../models/recipe.data';
import { RecipeListFavoritesComponent } from './recipe-list-favorites';

describe('RecipeListFavoritesComponent', () => {
    it('derives has-more state from total count and visible favorites', () => {
        const { component } = setupComponent({ favoriteTotalCount: 2 });

        expect(component['hasMoreFavorites']()).toBe(true);
    });

    it('renders favorite as recipe card and emits card actions', () => {
        const favorite = createFavoriteRecipe();
        const { component, fixture } = setupComponent({ favorites: [favorite] });
        const opened: FavoriteRecipe[] = [];
        const addedToMeal: FavoriteRecipe[] = [];
        const removed: FavoriteRecipe[] = [];
        component['favoriteOpen'].subscribe(item => opened.push(item));
        component['favoriteAddToMeal'].subscribe(item => addedToMeal.push(item));
        component['favoriteRemove'].subscribe(item => removed.push(item));

        const recipeCard = fixture.debugElement.query(By.directive(RecipeCardComponent)).componentInstance as RecipeCardComponent;
        expect(recipeCard['recipe']()).toMatchObject({
            id: favorite.recipeId,
            name: favorite.name,
            isFavorite: true,
            favoriteRecipeId: favorite.id,
            totalCalories: favorite.totalCalories,
        });
        expect(recipeCard['showOwnership']()).toBe(false);

        recipeCard['open'].emit();
        recipeCard['addToMeal'].emit();
        recipeCard['favoriteToggle'].emit();

        expect(opened).toEqual([favorite]);
        expect(addedToMeal).toEqual([favorite]);
        expect(removed).toEqual([favorite]);
    });
});

function setupComponent(overrides: { favoriteTotalCount?: number; favorites?: FavoriteRecipe[] } = {}): {
    component: RecipeListFavoritesComponent;
    fixture: ComponentFixture<RecipeListFavoritesComponent>;
} {
    TestBed.configureTestingModule({
        imports: [RecipeListFavoritesComponent],
        providers: [
            provideTranslateTesting(),
            {
                provide: AuthService,
                useValue: {
                    isAuthenticated: signal(true),
                },
            },
        ],
    });

    const fixture = TestBed.createComponent(RecipeListFavoritesComponent);
    fixture.componentRef.setInput('favorites', overrides.favorites ?? [createFavoriteRecipe()]);
    fixture.componentRef.setInput('favoriteTotalCount', overrides.favoriteTotalCount ?? 1);
    fixture.componentRef.setInput('isFavoritesOpen', true);
    fixture.componentRef.setInput('isFavoritesLoadingMore', false);
    fixture.componentRef.setInput('favoriteLoadingIds', new Set<string>());
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
