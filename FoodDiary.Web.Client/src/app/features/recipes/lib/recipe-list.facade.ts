import type { HttpErrorResponse } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { catchError, finalize, map, type Observable, of, switchMap, tap } from 'rxjs';

import { NavigationService } from '../../../services/navigation.service';
import { PagedData } from '../../../shared/lib/paged-data.data';
import { QuickMealService } from '../../meals/lib/quick-meal.service';
import { RecipeService } from '../api/recipe.service';
import type { RecipeDetailActionResult } from '../components/detail/recipe-detail.component';
import type { FavoriteRecipe, Recipe, RecipeFilters } from '../models/recipe.data';

@Injectable({ providedIn: 'root' })
export class RecipeListFacade {
    private readonly recipeService = inject(RecipeService);
    private readonly navigationService = inject(NavigationService);
    private readonly translateService = inject(TranslateService);
    private readonly toastService = inject(FdUiToastService);
    private readonly quickMealService = inject(QuickMealService);

    public readonly pageSize = 10;
    public readonly recipeData = new PagedData<Recipe>();
    public readonly currentPageIndex = signal(0);
    public readonly recentRecipes = signal<Recipe[]>([]);
    public readonly favoriteRecipes = signal<FavoriteRecipe[]>([]);
    public readonly favoriteTotalCount = signal(0);
    public readonly errorKey = signal<string | null>(null);
    public readonly isDeleting = signal(false);
    public readonly isFavoritesLoadingMore = signal(false);

    public readonly showRecentSection = computed(() => !this.hasSearchValue(this.searchValue()) && this.recentRecipes().length > 0);
    public readonly allRecipesSectionItems = computed(() => {
        const recipes = this.recipeData.items();
        if (recipes.length === 0) {
            return [];
        }

        if (!this.showRecentSection()) {
            return recipes;
        }

        const recentIds = new Set(this.recentRecipes().map(recipe => recipe.id));
        return recipes.filter(recipe => !recentIds.has(recipe.id));
    });
    public readonly hasVisibleRecipes = computed(() => this.showRecentSection() || this.allRecipesSectionItems().length > 0);
    public readonly allRecipesSectionLabelKey = computed(() =>
        this.hasSearchValue(this.searchValue()) ? 'RECIPE_LIST.SEARCH_RESULTS' : 'RECIPE_LIST.ALL_RECIPES',
    );

    private readonly searchValue = signal<string | null>(null);

    public loadRecipes(page: number, limit: number, search: string | null, onlyMine: boolean): Observable<void> {
        this.recipeData.setLoading(true);
        this.searchValue.set(search);
        const filters: RecipeFilters = { search };
        const includePublic = !onlyMine;

        return this.recipeService.query(page, limit, filters, includePublic).pipe(
            tap(data => {
                this.recipeData.setData(data);
                this.recentRecipes.set([]);
                this.currentPageIndex.set(data.page - 1);
                this.errorKey.set(null);
            }),
            map(() => void 0),
            catchError((_error: HttpErrorResponse) => {
                this.recipeData.clearData();
                this.recentRecipes.set([]);
                this.errorKey.set('ERRORS.LOAD_FAILED_TITLE');
                return of(void 0);
            }),
            finalize(() => {
                this.recipeData.setLoading(false);
            }),
        );
    }

    public loadInitialOverview(page: number, limit: number, search: string | null, onlyMine: boolean): Observable<void> {
        this.recipeData.setLoading(true);
        this.searchValue.set(search);
        const filters: RecipeFilters = { search };
        const includePublic = !onlyMine;

        return this.recipeService.queryOverview({ page, limit, filters, includePublic, recentLimit: 10, favoriteLimit: 10 }).pipe(
            tap(data => {
                this.recipeData.setData(data.allRecipes);
                this.recentRecipes.set(data.recentItems);
                this.favoriteRecipes.set(data.favoriteItems);
                this.favoriteTotalCount.set(data.favoriteTotalCount);
                this.currentPageIndex.set(data.allRecipes.page - 1);
                this.errorKey.set(null);
            }),
            map(() => void 0),
            catchError((_error: HttpErrorResponse) => {
                this.recipeData.clearData();
                this.recentRecipes.set([]);
                this.favoriteRecipes.set([]);
                this.favoriteTotalCount.set(0);
                this.errorKey.set('ERRORS.LOAD_FAILED_TITLE');
                return of(void 0);
            }),
            finalize(() => {
                this.recipeData.setLoading(false);
            }),
        );
    }

    public async navigateToAddRecipeAsync(): Promise<void> {
        await this.navigationService.navigateToRecipeAddAsync();
    }

    public async navigateToEditRecipeAsync(recipeId: string): Promise<void> {
        await this.navigationService.navigateToRecipeEditAsync(recipeId);
    }

    public async handleDetailActionAsync(
        result: RecipeDetailActionResult,
        recipe: Recipe,
        search: string | null,
        onlyMine: boolean,
    ): Promise<void> {
        if (result.action === 'Edit') {
            await this.navigateToEditRecipeAsync(recipe.id);
            return;
        }

        if (result.action === 'Duplicate') {
            await this.navigateToEditRecipeAsync(result.id);
            return;
        }

        if (result.action === 'Delete') {
            this.deleteRecipe(recipe, search, onlyMine).subscribe();
        }
    }

    public deleteRecipe(recipe: Recipe, search: string | null, onlyMine: boolean): Observable<void> {
        if (!recipe.isOwnedByCurrentUser || recipe.usageCount > 0 || this.isDeleting()) {
            return of(void 0);
        }

        this.isDeleting.set(true);
        this.recipeData.setLoading(true);

        return this.recipeService.deleteById(recipe.id).pipe(
            switchMap(() => this.loadRecipes(1, this.pageSize, search, onlyMine)),
            catchError(() => {
                this.toastService.error(this.translateService.instant('RECIPE_LIST.DELETE_ERROR'));
                return of(void 0);
            }),
            finalize(() => {
                this.isDeleting.set(false);
                this.recipeData.setLoading(false);
            }),
        );
    }

    public addToMeal(recipe: Recipe): void {
        this.quickMealService.addRecipe(recipe);
    }

    public hasActiveFilters(onlyMine: boolean): boolean {
        return onlyMine;
    }

    public hasSearch(search: string | null): boolean {
        return this.hasSearchValue(search);
    }

    private hasSearchValue(value: string | null): boolean {
        return !!value?.trim();
    }
}
