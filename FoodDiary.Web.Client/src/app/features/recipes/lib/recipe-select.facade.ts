import { inject, Service } from '@angular/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import type { Observable } from 'rxjs';

import type { PageOf } from '../../../shared/models/page-of.data';
import { RecipeService } from '../api/recipe.service';
import { RecipeListFiltersDialogComponent } from '../components/list/recipe-list-filters-dialog/recipe-list-filters-dialog';
import type { RecipeListFiltersDialogResult } from '../components/list/recipe-list-filters-dialog/recipe-list-filters-dialog.types';
import type { Recipe, RecipeFilters } from '../models/recipe.data';

@Service()
export class RecipeSelectFacade {
    private readonly recipeService = inject(RecipeService);
    private readonly dialogService = inject(FdUiDialogService);

    public query(page: number, limit: number, filters?: RecipeFilters, includePublic = true): Observable<PageOf<Recipe>> {
        return this.recipeService.query(page, limit, filters, includePublic);
    }

    public openFilters(filters: RecipeSelectFilterValues): Observable<RecipeListFiltersDialogResult | null | undefined> {
        return this.dialogService
            .open<RecipeListFiltersDialogComponent, RecipeSelectFilterValues, RecipeListFiltersDialogResult | null>(
                RecipeListFiltersDialogComponent,
                { preset: 'form', data: filters },
            )
            .afterClosed();
    }
}

export type RecipeSelectFilterValues = {
    onlyMine: boolean;
    category: string | null;
    maxTotalTime: number | null;
    caloriesFrom: number | null;
    caloriesTo: number | null;
    hasImage: boolean | null;
};
