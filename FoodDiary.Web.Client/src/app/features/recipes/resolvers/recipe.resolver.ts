import { inject } from '@angular/core';
import { ResolveFn } from '@angular/router';
import { catchError, of } from 'rxjs';
import { NavigationService } from '../../../services/navigation.service';
import { RecipeService } from '../api/recipe.service';
import { Recipe } from '../models/recipe.data';

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
