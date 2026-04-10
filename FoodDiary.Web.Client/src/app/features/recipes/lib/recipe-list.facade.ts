import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, finalize, map, of, switchMap, tap } from 'rxjs';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { TranslateService } from '@ngx-translate/core';
import { NavigationService } from '../../../services/navigation.service';
import { PagedData } from '../../../shared/lib/paged-data.data';
import { QuickMealService } from '../../meals/lib/quick-meal.service';
import { RecipeService } from '../api/recipe.service';
import { Recipe, RecipeFilters } from '../models/recipe.data';
import { RecipeDetailActionResult } from '../components/detail/recipe-detail.component';

@Injectable()
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
    public readonly errorKey = signal<string | null>(null);
    public readonly isDeleting = signal(false);

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

        return this.recipeService.queryWithRecent(page, limit, filters, includePublic, 10).pipe(
            tap(data => {
                this.recipeData.setData(data.allRecipes);
                this.recentRecipes.set(data.recentItems);
                this.currentPageIndex.set(data.allRecipes.page - 1);
                this.errorKey.set(null);
            }),
            map(() => void 0),
            catchError((_error: HttpErrorResponse) => {
                this.recipeData.clearData();
                this.recentRecipes.set([]);
                this.errorKey.set('ERRORS.LOAD_FAILED_TITLE');
                return of(void 0);
            }),
            finalize(() => this.recipeData.setLoading(false)),
        );
    }

    public async navigateToAddRecipe(): Promise<void> {
        await this.navigationService.navigateToRecipeAdd();
    }

    public async navigateToEditRecipe(recipeId: string): Promise<void> {
        await this.navigationService.navigateToRecipeEdit(recipeId);
    }

    public async handleDetailAction(
        result: RecipeDetailActionResult,
        recipe: Recipe,
        search: string | null,
        onlyMine: boolean,
    ): Promise<void> {
        if (result.action === 'Edit') {
            await this.navigateToEditRecipe(recipe.id);
            return;
        }

        if (result.action === 'Duplicate') {
            await this.navigateToEditRecipe(result.id);
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
