import { computed, inject, Injectable, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { catchError, finalize, map, type Observable, of, switchMap, tap } from 'rxjs';

import { NavigationService } from '../../../services/navigation.service';
import { PagedData } from '../../../shared/lib/paged-data.data';
import { QuickMealService } from '../../meals/lib/quick-meal.service';
import { FavoriteRecipeService } from '../api/favorite-recipe.service';
import { RecipeService } from '../api/recipe.service';
import type { RecipeDetailActionResult } from '../components/detail/recipe-detail-lib/recipe-detail.types';
import {
    RECIPE_LIST_OVERVIEW_FAVORITE_LIMIT,
    RECIPE_LIST_OVERVIEW_RECENT_LIMIT,
    RECIPE_LIST_PAGE_SIZE,
} from '../components/list/recipe-list.config';
import type { FavoriteRecipe, Recipe, RecipeFilters } from '../models/recipe.data';

@Injectable({ providedIn: 'root' })
export class RecipeListFacade {
    private readonly recipeService = inject(RecipeService);
    private readonly navigationService = inject(NavigationService);
    private readonly translateService = inject(TranslateService);
    private readonly toastService = inject(FdUiToastService);
    private readonly quickMealService = inject(QuickMealService);
    private readonly favoriteRecipeService = inject(FavoriteRecipeService);

    public readonly pageSize = RECIPE_LIST_PAGE_SIZE;
    public readonly recipeData = new PagedData<Recipe>();
    public readonly currentPageIndex = signal(0);
    public readonly recentRecipes = signal<Recipe[]>([]);
    public readonly favoriteRecipes = signal<FavoriteRecipe[]>([]);
    public readonly favoriteTotalCount = signal(0);
    public readonly errorKey = signal<string | null>(null);
    public readonly isDeleting = signal(false);
    public readonly isFavoritesLoadingMore = signal(false);
    public readonly favoriteLoadingIds = signal<ReadonlySet<string>>(new Set<string>());

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
            catchError((_error: unknown) => {
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

        return this.recipeService
            .queryOverview({
                page,
                limit,
                filters,
                includePublic,
                recentLimit: RECIPE_LIST_OVERVIEW_RECENT_LIMIT,
                favoriteLimit: RECIPE_LIST_OVERVIEW_FAVORITE_LIMIT,
            })
            .pipe(
                tap(data => {
                    this.recipeData.setData(data.allRecipes);
                    this.recentRecipes.set(data.recentItems);
                    this.favoriteRecipes.set(data.favoriteItems);
                    this.favoriteTotalCount.set(data.favoriteTotalCount);
                    this.currentPageIndex.set(data.allRecipes.page - 1);
                    this.errorKey.set(null);
                }),
                map(() => void 0),
                catchError((_error: unknown) => {
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

    public loadFavorites(): Observable<void> {
        this.isFavoritesLoadingMore.set(true);

        return this.favoriteRecipeService.getAll().pipe(
            tap(favorites => {
                this.favoriteRecipes.set(favorites);
                this.favoriteTotalCount.set(favorites.length);
            }),
            map(() => void 0),
            finalize(() => {
                this.isFavoritesLoadingMore.set(false);
            }),
        );
    }

    public toggleRecipeFavorite(recipe: Recipe): Observable<void> {
        if (this.favoriteLoadingIds().has(recipe.id)) {
            return of(void 0);
        }

        this.setFavoriteLoading(recipe.id, true);

        if (recipe.isFavorite === true) {
            return this.removeRecipeFavorite(recipe);
        }

        return this.favoriteRecipeService.add(recipe.id, recipe.name).pipe(
            tap(favorite => {
                this.syncRecipeFavoriteState(recipe.id, true, favorite.id);
            }),
            switchMap(() => this.loadFavorites()),
            finalize(() => {
                this.setFavoriteLoading(recipe.id, false);
            }),
        );
    }

    public getFavoriteRecipe(favorite: FavoriteRecipe): Observable<Recipe | null> {
        return this.recipeService.getById(favorite.recipeId);
    }

    public removeFavorite(favorite: FavoriteRecipe): Observable<void> {
        return this.favoriteRecipeService.remove(favorite.id).pipe(
            tap(() => {
                this.favoriteRecipes.update(favorites => favorites.filter(item => item.id !== favorite.id));
                this.favoriteTotalCount.update(count => Math.max(0, count - 1));
                this.syncRecipeFavoriteState(favorite.recipeId, false, null);
            }),
            map(() => void 0),
        );
    }

    public hasActiveFilters(onlyMine: boolean): boolean {
        return onlyMine;
    }

    public hasSearch(search: string | null): boolean {
        return this.hasSearchValue(search);
    }

    private syncRecipeFavoriteState(recipeId: string, isFavorite: boolean, favoriteRecipeId: string | null): void {
        this.recipeData.items.update(items =>
            items.map(recipe => (recipe.id === recipeId ? { ...recipe, isFavorite, favoriteRecipeId } : recipe)),
        );
        this.recentRecipes.update(recipes =>
            recipes.map(recipe => (recipe.id === recipeId ? { ...recipe, isFavorite, favoriteRecipeId } : recipe)),
        );
    }

    private removeRecipeFavorite(recipe: Recipe): Observable<void> {
        const favoriteId = recipe.favoriteRecipeId;
        const request$ =
            favoriteId !== null && favoriteId !== undefined && favoriteId.length > 0
                ? this.favoriteRecipeService.remove(favoriteId)
                : this.favoriteRecipeService.getAll().pipe(
                      switchMap(favorites => {
                          const match = favorites.find(favorite => favorite.recipeId === recipe.id);
                          return match === undefined ? of(null) : this.favoriteRecipeService.remove(match.id);
                      }),
                  );

        return request$.pipe(
            tap(() => {
                this.syncRecipeFavoriteState(recipe.id, false, null);
            }),
            switchMap(() => this.loadFavorites()),
            finalize(() => {
                this.setFavoriteLoading(recipe.id, false);
            }),
        );
    }

    private setFavoriteLoading(recipeId: string, isLoading: boolean): void {
        this.favoriteLoadingIds.update(current => {
            const next = new Set(current);
            if (isLoading) {
                next.add(recipeId);
            } else {
                next.delete(recipeId);
            }

            return next;
        });
    }

    private hasSearchValue(value: string | null): boolean {
        return value !== null && value.trim().length > 0;
    }
}
