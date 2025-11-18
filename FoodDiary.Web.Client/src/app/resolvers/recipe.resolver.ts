import { ResolveFn } from '@angular/router';
import { Recipe } from '../types/recipe.data';
import { inject } from '@angular/core';
import { RecipeService } from '../services/recipe.service';
import { NavigationService } from '../services/navigation.service';
import { catchError, of } from 'rxjs';

export const recipeResolver: ResolveFn<Recipe | null> = route => {
    const recipeService = inject(RecipeService);
    const navigationService = inject(NavigationService);

    const recipeId = route.paramMap.get('id');
    if (!recipeId) {
        navigationService.navigateToRecipeList();
        return of(null);
    }

    return recipeService.getById(recipeId, false).pipe(
        catchError(() => {
            navigationService.navigateToRecipeList();
            return of(null);
        }),
    );
};
