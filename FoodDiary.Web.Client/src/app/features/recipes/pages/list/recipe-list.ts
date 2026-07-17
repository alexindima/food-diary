import {
    ChangeDetectionStrategy,
    Component,
    computed,
    DestroyRef,
    effect,
    type ElementRef,
    inject,
    signal,
    viewChild,
} from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { form, FormField, FormRoot } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { FdTourService } from 'fd-tour';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination';
import { debounceTime, distinctUntilChanged, EMPTY, skip, switchMap } from 'rxjs';

import { ErrorStateComponent } from '../../../../components/shared/error-state/error-state';
import { PageBodyComponent } from '../../../../components/shared/page-body/page-body';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header';
import { SkeletonCardComponent } from '../../../../components/shared/skeleton-card/skeleton-card';
import { APP_SEARCH_DEBOUNCE_MS } from '../../../../config/runtime-ui.tokens';
import { ViewportService } from '../../../../shared/platform/viewport.service';
import { LocalizedTourDefinitionService } from '../../../../shared/tours/localized-tour-definition.service';
import { FdPageContainerDirective } from '../../../../shared/ui/layout/page-container.directive';
import { RecipeDetailActionResult } from '../../components/detail/recipe-detail-lib/recipe-detail.types';
import type { RecipeListFiltersDialogResult } from '../../components/list/recipe-list-filters-dialog/recipe-list-filters-dialog.types';
import { RecipeListFavoritesComponent } from '../../components/list/recipe-list-sections/recipe-list-favorites/recipe-list-favorites';
import {
    type RecipeListEmptyState,
    RecipeListResultsComponent,
} from '../../components/list/recipe-list-sections/recipe-list-results/recipe-list-results';
import { resolveRecipeImageUrl } from '../../lib/recipe-image.util';
import { RecipeListFacade } from '../../lib/recipe-list.facade';
import type { FavoriteRecipe, Recipe, RecipeFilters } from '../../models/recipe.data';
import type { RecipeCardViewModel } from './recipe-list.types';
import { RECIPE_LIST_TOUR } from './recipe-list-tour';

@Component({
    selector: 'fd-recipe-list',
    templateUrl: './recipe-list.html',
    styleUrls: ['./recipe-list.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        FormField,
        FormRoot,
        TranslatePipe,
        FdUiHintDirective,
        FdUiInputComponent,
        FdUiButtonComponent,
        FdUiPaginationComponent,
        SkeletonCardComponent,
        ErrorStateComponent,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        RecipeListFavoritesComponent,
        RecipeListResultsComponent,
    ],
    providers: [RecipeListFacade],
})
export class RecipeListComponent {
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly viewportService = inject(ViewportService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly recipeListFacade = inject(RecipeListFacade);
    private readonly tourService = inject(FdTourService);
    private readonly localizedTour = inject(LocalizedTourDefinitionService);
    private readonly searchDebounceMs = inject(APP_SEARCH_DEBOUNCE_MS);

    private readonly container = viewChild.required<ElementRef<HTMLElement>>('container');

    protected readonly pageSize = this.recipeListFacade.pageSize;
    protected recipeData = this.recipeListFacade.recipeData;
    protected currentPageIndex = this.recipeListFacade.currentPageIndex;
    protected recentRecipes = this.recipeListFacade.recentRecipes;
    protected readonly favorites = this.recipeListFacade.favoriteRecipes;
    protected readonly favoriteTotalCount = this.recipeListFacade.favoriteTotalCount;
    protected readonly isFavoritesOpen = signal(false);
    protected readonly isFavoritesLoadingMore = this.recipeListFacade.isFavoritesLoadingMore;
    protected readonly errorKey = this.recipeListFacade.errorKey;
    protected readonly isMobileView = this.viewportService.isMobile;
    protected readonly recentRecipeItems = computed<RecipeCardViewModel[]>(() =>
        this.recentRecipes().map(recipe => ({
            recipe,
            imageUrl: this.resolveImage(recipe),
        })),
    );
    protected readonly allRecipesSectionItems = computed(() => this.recipeListFacade.allRecipesSectionItems());
    protected readonly allRecipeItems = computed<RecipeCardViewModel[]>(() =>
        this.allRecipesSectionItems().map(recipe => ({
            recipe,
            imageUrl: this.resolveImage(recipe),
        })),
    );
    protected readonly hasVisibleRecipes = computed(() => this.recipeListFacade.hasVisibleRecipes());
    protected readonly activeFilterKeys = computed(() => {
        const filters = this.buildRecipeFilters();
        const keys: string[] = [];
        if (this.searchModel().onlyMine) {
            keys.push('RECIPE_LIST.FILTER_MY_RECIPES');
        }
        if (this.hasFilterText(filters.category)) {
            keys.push('RECIPE_LIST.FILTER_CATEGORY_ACTIVE');
        }
        if (filters.maxTotalTime !== null && filters.maxTotalTime !== undefined) {
            keys.push('RECIPE_LIST.FILTER_MAX_TOTAL_TIME_ACTIVE');
        }
        if (filters.caloriesFrom !== null || filters.caloriesTo !== null) {
            keys.push('RECIPE_LIST.FILTER_CALORIES_ACTIVE');
        }
        if (filters.hasImage === true) {
            keys.push('RECIPE_LIST.FILTER_IMAGE_WITH');
        }
        if (filters.hasImage === false) {
            keys.push('RECIPE_LIST.FILTER_IMAGE_WITHOUT');
        }

        return keys;
    });
    protected readonly hasActiveFilters = computed(() =>
        this.recipeListFacade.hasActiveFilters(this.searchModel().onlyMine, this.buildRecipeFilters()),
    );
    protected readonly isEmptyState = computed(
        () => !this.hasVisibleRecipes() && !this.recipeListFacade.hasSearch(this.searchModel().search) && !this.hasActiveFilters(),
    );
    protected readonly allRecipesSectionLabelKey = computed(() => this.recipeListFacade.allRecipesSectionLabelKey());
    protected readonly emptyState = computed<RecipeListEmptyState>(() => (this.isEmptyState() ? 'empty' : 'no-results'));
    protected readonly isMobileSearchVisible = computed(
        () => this.isMobileSearchOpen() || this.recipeListFacade.hasSearch(this.searchModel().search),
    );
    protected readonly pageIndex = computed(() => this.currentPageIndex());
    private readonly isMobileSearchOpen = signal(false);
    protected readonly searchModel = signal<RecipeSearchFormValues>({
        search: null,
        onlyMine: false,
        category: null,
        maxTotalTime: null,
        caloriesFrom: null,
        caloriesTo: null,
        hasImage: null,
    });
    protected readonly searchForm = form(this.searchModel);
    protected readonly isDeleting = this.recipeListFacade.isDeleting;
    protected readonly favoriteLoadingIds = this.recipeListFacade.favoriteLoadingIds;

    public constructor() {
        effect(() => {
            if (!this.isMobileView()) {
                this.isMobileSearchOpen.set(false);
            }
        });

        this.recipeListFacade.loadInitialOverview(1, this.pageSize, this.buildRecipeFilters(), this.searchModel().onlyMine).subscribe();

        toObservable(computed(() => this.searchModel().search))
            .pipe(
                skip(1),
                debounceTime(this.searchDebounceMs),
                switchMap(() =>
                    this.recipeListFacade.loadRecipes(1, this.pageSize, this.buildRecipeFilters(), this.searchModel().onlyMine),
                ),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe();

        toObservable(computed(() => this.searchModel().onlyMine))
            .pipe(
                skip(1),
                distinctUntilChanged(),
                switchMap(() =>
                    this.recipeListFacade.loadRecipes(1, this.pageSize, this.buildRecipeFilters(), this.searchModel().onlyMine),
                ),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe();
    }

    protected resolveImage(recipe: Recipe): string | undefined {
        return resolveRecipeImageUrl(recipe.imageUrl ?? undefined);
    }

    protected retryLoad(): void {
        this.recipeListFacade.loadInitialOverview(1, this.pageSize, this.buildRecipeFilters(), this.searchModel().onlyMine).subscribe();
    }

    protected async onAddRecipeClickAsync(): Promise<void> {
        await this.recipeListFacade.navigateToAddRecipeAsync();
    }

    protected onRecipeClick(recipe: Recipe): void {
        void this.openRecipeDetailAsync(recipe);
    }

    private async openRecipeDetailAsync(recipe: Recipe): Promise<void> {
        const { RecipeDetailComponent } = await import('../../components/detail/recipe-detail/recipe-detail');
        this.fdDialogService
            .open(RecipeDetailComponent, {
                preset: 'detail',
                data: recipe,
            })
            .afterClosed()
            .subscribe(result => {
                if (!(result instanceof RecipeDetailActionResult)) {
                    return;
                }

                const actionResult = result;
                if (actionResult.action === 'FavoriteChanged') {
                    this.loadFavorites();
                    this.reloadCurrentPage();
                    return;
                }

                void this.recipeListFacade.handleDetailActionAsync(
                    actionResult,
                    recipe,
                    this.searchModel().search,
                    this.searchModel().onlyMine,
                );
            });
    }

    protected onPageChange(pageIndex: number): void {
        this.scrollToTop();
        this.currentPageIndex.set(pageIndex);
        this.recipeListFacade
            .loadRecipes(this.currentPageIndex() + 1, this.pageSize, this.buildRecipeFilters(), this.searchModel().onlyMine)
            .subscribe();
    }

    protected toggleMobileSearch(): void {
        this.isMobileSearchOpen.update(value => !value);
    }

    protected openFilters(): void {
        const currentOnlyMine = this.searchModel().onlyMine;
        const currentFilters = this.buildRecipeFilters();
        this.recipeListFacade
            .openFilters({
                onlyMine: currentOnlyMine,
                category: currentFilters.category ?? null,
                maxTotalTime: currentFilters.maxTotalTime ?? null,
                caloriesFrom: currentFilters.caloriesFrom ?? null,
                caloriesTo: currentFilters.caloriesTo ?? null,
                hasImage: currentFilters.hasImage ?? null,
            })
            .pipe(
                switchMap(result => {
                    if (result === null || result === undefined || this.hasSameFilters(currentOnlyMine, currentFilters, result)) {
                        return EMPTY;
                    }

                    this.searchModel.update(value => ({
                        ...value,
                        onlyMine: result.onlyMine,
                        category: result.category,
                        maxTotalTime: result.maxTotalTime,
                        caloriesFrom: result.caloriesFrom,
                        caloriesTo: result.caloriesTo,
                        hasImage: result.hasImage,
                    }));
                    return this.recipeListFacade.loadRecipes(1, this.pageSize, this.buildRecipeFilters(), result.onlyMine);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe();
    }

    protected startRecipeListTour(force = true): void {
        this.tourService.start(this.localizedTour.build(RECIPE_LIST_TOUR), { force });
    }

    protected clearSearch(): void {
        this.searchForm.search().value.set('');
    }

    protected onAddToMeal(recipe: Recipe): void {
        this.recipeListFacade.addToMeal(recipe);
    }

    protected loadFavorites(): void {
        this.recipeListFacade.loadFavorites().pipe(takeUntilDestroyed(this.destroyRef)).subscribe();
    }

    protected onRecipeFavoriteToggle(recipe: Recipe): void {
        this.recipeListFacade.toggleRecipeFavorite(recipe).pipe(takeUntilDestroyed(this.destroyRef)).subscribe();
    }

    protected toggleFavorites(): void {
        this.isFavoritesOpen.update(value => !value);
    }

    protected openFavoriteRecipe(favorite: FavoriteRecipe): void {
        this.recipeListFacade
            .getFavoriteRecipe(favorite)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(recipe => {
                if (recipe !== null) {
                    this.onRecipeClick(recipe);
                }
            });
    }

    protected addFavoriteRecipeToMeal(favorite: FavoriteRecipe): void {
        this.recipeListFacade
            .getFavoriteRecipe(favorite)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(recipe => {
                if (recipe !== null) {
                    this.onAddToMeal(recipe);
                }
            });
    }

    protected removeFavorite(favorite: FavoriteRecipe): void {
        this.recipeListFacade.removeFavorite(favorite).pipe(takeUntilDestroyed(this.destroyRef)).subscribe();
    }

    private scrollToTop(): void {
        this.container().nativeElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }

    private reloadCurrentPage(): void {
        this.recipeListFacade
            .loadRecipes(this.currentPageIndex() + 1, this.pageSize, this.buildRecipeFilters(), this.searchModel().onlyMine)
            .subscribe();
    }

    private buildRecipeFilters(): RecipeFilters {
        const model = this.searchModel();
        return {
            search: model.search,
            category: model.category,
            maxTotalTime: model.maxTotalTime,
            caloriesFrom: model.caloriesFrom,
            caloriesTo: model.caloriesTo,
            hasImage: model.hasImage,
        };
    }

    private hasSameFilters(currentOnlyMine: boolean, currentFilters: RecipeFilters, next: RecipeListFiltersDialogResult): boolean {
        return currentOnlyMine === next.onlyMine && this.hasSameRecipeFilterValues(currentFilters, next);
    }

    private hasSameRecipeFilterValues(currentFilters: RecipeFilters, next: RecipeListFiltersDialogResult): boolean {
        return (
            (currentFilters.category ?? null) === next.category &&
            (currentFilters.maxTotalTime ?? null) === next.maxTotalTime &&
            (currentFilters.caloriesFrom ?? null) === next.caloriesFrom &&
            (currentFilters.caloriesTo ?? null) === next.caloriesTo &&
            (currentFilters.hasImage ?? null) === next.hasImage
        );
    }

    private hasFilterText(value: string | null | undefined): boolean {
        return value !== null && value !== undefined && value.trim().length > 0;
    }
}

type RecipeSearchFormValues = {
    search: string | null;
    onlyMine: boolean;
    category: string | null;
    maxTotalTime: number | null;
    caloriesFrom: number | null;
    caloriesTo: number | null;
    hasImage: boolean | null;
};
