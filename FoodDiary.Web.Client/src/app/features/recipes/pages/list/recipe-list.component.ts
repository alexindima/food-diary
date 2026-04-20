import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ChangeDetectionStrategy, Component, DestroyRef, computed, effect, ElementRef, inject, signal, viewChild } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon.component';
import { ErrorStateComponent } from '../../../../components/shared/error-state/error-state.component';
import { FavoritesSectionComponent } from '../../../../components/shared/favorites-section/favorites-section.component';
import { SkeletonCardComponent } from '../../../../components/shared/skeleton-card/skeleton-card.component';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination.component';
import { debounceTime, distinctUntilChanged, finalize, switchMap } from 'rxjs';
import { FdPageContainerDirective } from '../../../../directives/layout/page-container.directive';
import { FormGroupControls } from '../../../../shared/lib/common.data';
import { resolveRecipeImageUrl } from '../../lib/recipe-image.util';
import { PageBodyComponent } from '../../../../components/shared/page-body/page-body.component';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header.component';
import { RecipeCardComponent } from '../../../../components/shared/recipe-card/recipe-card.component';
import { RecipeDetailActionResult, RecipeDetailComponent } from '../../components/detail/recipe-detail.component';
import { FavoriteRecipeService } from '../../api/favorite-recipe.service';
import { RecipeService } from '../../api/recipe.service';
import {
    RecipeListFiltersDialogComponent,
    RecipeListFiltersDialogResult,
} from '../../components/list/recipe-list-filters-dialog.component';
import { FavoriteRecipe, Recipe, RecipeVisibility } from '../../models/recipe.data';
import { RecipeListFacade } from '../../lib/recipe-list.facade';
import { ViewportService } from '../../../../services/viewport.service';

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
        FavoritesSectionComponent,
        FdUiIconComponent,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        RecipeCardComponent,
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
    public readonly recentRecipeItems = computed(() => this.recentRecipes());
    public readonly allRecipesSectionItems = computed(() => this.recipeListFacade.allRecipesSectionItems());
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
                debounceTime(300),
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

    public async onAddRecipeClick(): Promise<void> {
        await this.recipeListFacade.navigateToAddRecipe();
    }

    public onRecipeClick(recipe: Recipe): void {
        this.fdDialogService
            .open<RecipeDetailComponent, Recipe, RecipeDetailActionResult>(RecipeDetailComponent, {
                size: 'lg',
                data: recipe,
                panelClass: 'fd-ui-dialog-panel--detail',
                backdropClass: 'fd-ui-dialog-backdrop--detail',
            })
            .afterClosed()
            .subscribe(result => {
                this.loadFavorites();
                this.reloadCurrentPage();

                if (!result) {
                    return;
                }
                void this.recipeListFacade.handleDetailAction(
                    result,
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

    public async onEditRecipe(recipe: Recipe): Promise<void> {
        await this.recipeListFacade.navigateToEditRecipe(recipe.id);
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
                    size: 'sm',
                    data: { onlyMine: currentOnlyMine },
                },
            )
            .afterClosed()
            .subscribe(result => {
                if (!result || result.onlyMine === currentOnlyMine) {
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
                finalize(() => this.recipeListFacade.isFavoritesLoadingMore.set(false)),
            )
            .subscribe(favorites => {
                this.favorites.set(favorites);
                this.favoriteTotalCount.set(favorites.length);
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
                if (recipe) {
                    this.onRecipeClick(recipe);
                }
            });
    }

    public addFavoriteRecipeToMeal(favorite: FavoriteRecipe): void {
        this.recipeService
            .getById(favorite.recipeId)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(recipe => {
                if (recipe) {
                    this.onAddToMeal(recipe);
                }
            });
    }

    public removeFavorite(favorite: FavoriteRecipe): void {
        this.favoriteRecipeService.remove(favorite.id).subscribe({
            next: () => {
                this.loadFavorites();
                this.reloadCurrentPage();
                this.favoriteTotalCount.update(count => Math.max(0, count - 1));
                this.recentRecipes.set(
                    this.recentRecipes().map(recipe =>
                        recipe.id === favorite.recipeId ? { ...recipe, isFavorite: false, favoriteRecipeId: null } : recipe,
                    ),
                );
            },
        });
    }

    public isPrivateVisibility(visibility: RecipeVisibility | string | null | undefined): boolean {
        return visibility?.toString().toUpperCase() === 'PRIVATE';
    }

    private scrollToTop(): void {
        this.container()?.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
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
}

interface RecipeSearchFormValues {
    search: string | null;
    onlyMine: boolean;
}

type RecipeSearchFormGroup = FormGroupControls<RecipeSearchFormValues>;
