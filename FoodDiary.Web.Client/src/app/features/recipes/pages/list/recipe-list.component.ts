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
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination.component';
import { debounceTime, distinctUntilChanged, finalize, of, switchMap } from 'rxjs';

import { ErrorStateComponent } from '../../../../components/shared/error-state/error-state.component';
import { PageBodyComponent } from '../../../../components/shared/page-body/page-body.component';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header.component';
import { SkeletonCardComponent } from '../../../../components/shared/skeleton-card/skeleton-card.component';
import { APP_SEARCH_DEBOUNCE_MS } from '../../../../config/runtime-ui.tokens';
import { FdPageContainerDirective } from '../../../../directives/layout/page-container.directive';
import { ViewportService } from '../../../../services/viewport.service';
import type { FormGroupControls } from '../../../../shared/lib/common.data';
import { FavoriteRecipeService } from '../../api/favorite-recipe.service';
import { RecipeService } from '../../api/recipe.service';
import { RecipeDetailActionResult } from '../../components/detail/recipe-detail.types';
import {
    RecipeListFiltersDialogComponent,
    type RecipeListFiltersDialogResult,
} from '../../components/list/recipe-list-filters-dialog.component';
import { resolveRecipeImageUrl } from '../../lib/recipe-image.util';
import { RecipeListFacade } from '../../lib/recipe-list.facade';
import type { FavoriteRecipe, Recipe, RecipeVisibility } from '../../models/recipe.data';
import type { RecipeCardViewModel } from './recipe-list.types';
import { RecipeListFavoritesComponent } from './recipe-list-favorites.component';
import { RecipeListResultsComponent } from './recipe-list-results.component';

@Component({
    selector: 'fd-recipe-list',
    templateUrl: './recipe-list.component.html',
    styleUrls: ['./recipe-list.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        ReactiveFormsModule,
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
    private readonly translateService = inject(TranslateService);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly viewportService = inject(ViewportService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly recipeListFacade = inject(RecipeListFacade);
    private readonly favoriteRecipeService = inject(FavoriteRecipeService);
    private readonly recipeService = inject(RecipeService);
    private readonly searchDebounceMs = inject(APP_SEARCH_DEBOUNCE_MS);

    private readonly container = viewChild.required<ElementRef<HTMLElement>>('container');

    public readonly pageSize = this.recipeListFacade.pageSize;
    public recipeData = this.recipeListFacade.recipeData;
    public currentPageIndex = this.recipeListFacade.currentPageIndex;
    public recentRecipes = this.recipeListFacade.recentRecipes;
    public readonly favorites = this.recipeListFacade.favoriteRecipes;
    public readonly favoriteTotalCount = this.recipeListFacade.favoriteTotalCount;
    public readonly isFavoritesOpen = signal(false);
    public readonly isFavoritesLoadingMore = this.recipeListFacade.isFavoritesLoadingMore;
    public readonly errorKey = this.recipeListFacade.errorKey;
    public readonly isMobileView = this.viewportService.isMobile;
    public readonly showRecentSection = computed(() => this.recipeListFacade.showRecentSection());
    public readonly recentRecipeItems = computed<RecipeCardViewModel[]>(() =>
        this.recentRecipes().map(recipe => ({
            recipe,
            imageUrl: this.resolveImage(recipe),
        })),
    );
    public readonly allRecipesSectionItems = computed(() => this.recipeListFacade.allRecipesSectionItems());
    public readonly allRecipeItems = computed<RecipeCardViewModel[]>(() =>
        this.allRecipesSectionItems().map(recipe => ({
            recipe,
            imageUrl: this.resolveImage(recipe),
        })),
    );
    public readonly hasVisibleRecipes = computed(() => this.recipeListFacade.hasVisibleRecipes());
    public readonly hasActiveFilters = computed(() => this.recipeListFacade.hasActiveFilters(this.searchForm.controls.onlyMine.value));
    public readonly isEmptyState = computed(
        () =>
            !this.hasVisibleRecipes() &&
            !this.recipeListFacade.hasSearch(this.searchForm.controls.search.value) &&
            !this.hasActiveFilters(),
    );
    public readonly isNoResultsState = computed(() => !this.hasVisibleRecipes() && !this.isEmptyState());
    public readonly allRecipesSectionLabelKey = computed(() => this.recipeListFacade.allRecipesSectionLabelKey());
    public readonly isMobileSearchVisible = computed(
        () => this.isMobileSearchOpen() || this.recipeListFacade.hasSearch(this.searchForm.controls.search.value),
    );
    public readonly pageIndex = computed(() => this.currentPageIndex());
    public readonly hasMoreFavorites = computed(() => this.favoriteTotalCount() > this.favorites().length);
    private readonly isMobileSearchOpen = signal(false);
    public searchForm: FormGroup<RecipeSearchFormGroup>;
    public readonly isDeleting = this.recipeListFacade.isDeleting;
    public readonly favoriteLoadingIds = signal<ReadonlySet<string>>(new Set<string>());
    protected readonly fallbackRecipeImage = 'assets/images/stubs/receipt.png';

    public constructor() {
        this.searchForm = new FormGroup<RecipeSearchFormGroup>({
            search: new FormControl<string | null>(null),
            onlyMine: new FormControl<boolean>(false, { nonNullable: true }),
        });

        effect(() => {
            if (!this.isMobileView()) {
                this.isMobileSearchOpen.set(false);
            }
        });

        this.recipeListFacade
            .loadInitialOverview(1, this.pageSize, this.searchForm.controls.search.value, this.searchForm.controls.onlyMine.value)
            .subscribe();

        this.searchForm.controls.search.valueChanges
            .pipe(
                debounceTime(this.searchDebounceMs),
                switchMap(value => this.recipeListFacade.loadRecipes(1, this.pageSize, value, this.searchForm.controls.onlyMine.value)),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe();

        this.searchForm.controls.onlyMine.valueChanges
            .pipe(
                distinctUntilChanged(),
                switchMap(() =>
                    this.recipeListFacade.loadRecipes(
                        1,
                        this.pageSize,
                        this.searchForm.controls.search.value,
                        this.searchForm.controls.onlyMine.value,
                    ),
                ),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe();
    }

    public resolveImage(recipe: Recipe): string | undefined {
        return resolveRecipeImageUrl(recipe.imageUrl ?? undefined);
    }

    public retryLoad(): void {
        this.recipeListFacade
            .loadInitialOverview(1, this.pageSize, this.searchForm.controls.search.value, this.searchForm.controls.onlyMine.value)
            .subscribe();
    }

    public async onAddRecipeClickAsync(): Promise<void> {
        await this.recipeListFacade.navigateToAddRecipeAsync();
    }

    public onRecipeClick(recipe: Recipe): void {
        void this.openRecipeDetailAsync(recipe);
    }

    private async openRecipeDetailAsync(recipe: Recipe): Promise<void> {
        const { RecipeDetailComponent } = await import('../../components/detail/recipe-detail.component');
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
                    this.searchForm.controls.search.value,
                    this.searchForm.controls.onlyMine.value,
                );
            });
    }

    public onPageChange(pageIndex: number): void {
        this.scrollToTop();
        this.currentPageIndex.set(pageIndex);
        this.recipeListFacade
            .loadRecipes(
                this.currentPageIndex() + 1,
                this.pageSize,
                this.searchForm.controls.search.value,
                this.searchForm.controls.onlyMine.value,
            )
            .subscribe();
    }

    public async onEditRecipeAsync(recipe: Recipe): Promise<void> {
        await this.recipeListFacade.navigateToEditRecipeAsync(recipe.id);
    }

    public onDeleteRecipe(recipe: Recipe): void {
        this.recipeListFacade
            .deleteRecipe(recipe, this.searchForm.controls.search.value, this.searchForm.controls.onlyMine.value)
            .subscribe();
    }

    public trackByRecipeId(_index: number, recipe: Recipe): string {
        return recipe.id;
    }

    public getVisibilityTranslation(visibility: RecipeVisibility): string {
        return this.translateService.instant(`RECIPE_VISIBILITY.${visibility}`);
    }

    public toggleMobileSearch(): void {
        this.isMobileSearchOpen.update(value => !value);
    }

    public openFilters(): void {
        const currentOnlyMine = this.searchForm.controls.onlyMine.value;
        this.fdDialogService
            .open<RecipeListFiltersDialogComponent, { onlyMine: boolean }, RecipeListFiltersDialogResult | null>(
                RecipeListFiltersDialogComponent,
                {
                    preset: 'form',
                    data: { onlyMine: currentOnlyMine },
                },
            )
            .afterClosed()
            .subscribe(result => {
                if (result === null || result === undefined || result.onlyMine === currentOnlyMine) {
                    return;
                }

                this.searchForm.controls.onlyMine.setValue(result.onlyMine);
            });
    }

    public clearSearch(): void {
        this.searchForm.controls.search.setValue('');
    }

    public onAddToMeal(recipe: Recipe): void {
        this.recipeListFacade.addToMeal(recipe);
    }

    public loadFavorites(): void {
        this.recipeListFacade.isFavoritesLoadingMore.set(true);
        this.favoriteRecipeService
            .getAll()
            .pipe(
                takeUntilDestroyed(this.destroyRef),
                finalize(() => {
                    this.recipeListFacade.isFavoritesLoadingMore.set(false);
                }),
            )
            .subscribe(favorites => {
                this.favorites.set(favorites);
                this.favoriteTotalCount.set(favorites.length);
            });
    }

    public onRecipeFavoriteToggle(recipe: Recipe): void {
        if (this.favoriteLoadingIds().has(recipe.id)) {
            return;
        }

        this.setFavoriteLoading(recipe.id, true);

        if (recipe.isFavorite === true) {
            this.removeRecipeFavorite(recipe);
            return;
        }

        this.favoriteRecipeService
            .add(recipe.id, recipe.name)
            .pipe(
                takeUntilDestroyed(this.destroyRef),
                finalize(() => {
                    this.setFavoriteLoading(recipe.id, false);
                }),
            )
            .subscribe(favorite => {
                this.syncRecipeFavoriteState(recipe.id, true, favorite.id);
                this.loadFavorites();
            });
    }

    public toggleFavorites(): void {
        this.isFavoritesOpen.update(value => !value);
    }

    public openFavoriteRecipe(favorite: FavoriteRecipe): void {
        this.recipeService
            .getById(favorite.recipeId)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(recipe => {
                if (recipe !== null) {
                    this.onRecipeClick(recipe);
                }
            });
    }

    public addFavoriteRecipeToMeal(favorite: FavoriteRecipe): void {
        this.recipeService
            .getById(favorite.recipeId)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(recipe => {
                if (recipe !== null) {
                    this.onAddToMeal(recipe);
                }
            });
    }

    public removeFavorite(favorite: FavoriteRecipe): void {
        this.favoriteRecipeService.remove(favorite.id).subscribe({
            next: () => {
                this.favorites.update(favorites => favorites.filter(item => item.id !== favorite.id));
                this.favoriteTotalCount.update(count => Math.max(0, count - 1));
                this.syncRecipeFavoriteState(favorite.recipeId, false, null);
            },
        });
    }

    public isPrivateVisibility(visibility: RecipeVisibility | string | null | undefined): boolean {
        return visibility?.toString().toUpperCase() === 'PRIVATE';
    }

    private scrollToTop(): void {
        this.container().nativeElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }

    private reloadCurrentPage(): void {
        this.recipeListFacade
            .loadRecipes(
                this.currentPageIndex() + 1,
                this.pageSize,
                this.searchForm.controls.search.value,
                this.searchForm.controls.onlyMine.value,
            )
            .subscribe();
    }

    private syncRecipeFavoriteState(recipeId: string, isFavorite: boolean, favoriteRecipeId: string | null): void {
        this.recipeData.items.update(items =>
            items.map(recipe => (recipe.id === recipeId ? { ...recipe, isFavorite, favoriteRecipeId } : recipe)),
        );
        this.recentRecipes.update(recipes =>
            recipes.map(recipe => (recipe.id === recipeId ? { ...recipe, isFavorite, favoriteRecipeId } : recipe)),
        );
    }

    private removeRecipeFavorite(recipe: Recipe): void {
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

        request$
            .pipe(
                takeUntilDestroyed(this.destroyRef),
                finalize(() => {
                    this.setFavoriteLoading(recipe.id, false);
                }),
            )
            .subscribe(() => {
                this.syncRecipeFavoriteState(recipe.id, false, null);
                this.loadFavorites();
            });
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
}

type RecipeSearchFormValues = {
    search: string | null;
    onlyMine: boolean;
};

type RecipeSearchFormGroup = FormGroupControls<RecipeSearchFormValues>;
