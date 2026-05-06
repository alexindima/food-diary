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
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination.component';
import { debounceTime, distinctUntilChanged, finalize, switchMap } from 'rxjs';

import { ErrorStateComponent } from '../../../../components/shared/error-state/error-state.component';
import { FavoritesSectionComponent } from '../../../../components/shared/favorites-section/favorites-section.component';
import { PageBodyComponent } from '../../../../components/shared/page-body/page-body.component';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header.component';
import { RecipeCardComponent, type RecipeFavoriteChange } from '../../../../components/shared/recipe-card/recipe-card.component';
import { SkeletonCardComponent } from '../../../../components/shared/skeleton-card/skeleton-card.component';
import { FdPageContainerDirective } from '../../../../directives/layout/page-container.directive';
import { ViewportService } from '../../../../services/viewport.service';
import { type FormGroupControls } from '../../../../shared/lib/common.data';
import { FavoriteRecipeService } from '../../api/favorite-recipe.service';
import { RecipeService } from '../../api/recipe.service';
import type { RecipeDetailActionResult } from '../../components/detail/recipe-detail.component';
import {
    RecipeListFiltersDialogComponent,
    type RecipeListFiltersDialogResult,
} from '../../components/list/recipe-list-filters-dialog.component';
import { resolveRecipeImageUrl } from '../../lib/recipe-image.util';
import { RecipeListFacade } from '../../lib/recipe-list.facade';
import { type FavoriteRecipe, type Recipe, type RecipeVisibility } from '../../models/recipe.data';

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
        void this.openRecipeDetail(recipe);
    }

    private async openRecipeDetail(recipe: Recipe): Promise<void> {
        const { RecipeDetailComponent } = await import('../../components/detail/recipe-detail.component');
        this.fdDialogService
            .open(RecipeDetailComponent, {
                preset: 'detail',
                data: recipe,
            })
            .afterClosed()
            .subscribe(result => {
                const actionResult = result as RecipeDetailActionResult | undefined;

                if (!actionResult) {
                    return;
                }

                if (actionResult.action === 'FavoriteChanged') {
                    this.loadFavorites();
                    this.reloadCurrentPage();
                    return;
                }

                void this.recipeListFacade.handleDetailAction(
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
                    preset: 'form',
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
                finalize(() => {
                    this.recipeListFacade.isFavoritesLoadingMore.set(false);
                }),
            )
            .subscribe(favorites => {
                this.favorites.set(favorites);
                this.favoriteTotalCount.set(favorites.length);
            });
    }

    public onRecipeFavoriteChanged(recipe: Recipe, change: RecipeFavoriteChange): void {
        this.syncRecipeFavoriteState(recipe.id, change.isFavorite, change.favoriteRecipeId);
        this.loadFavorites();
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
}

interface RecipeSearchFormValues {
    search: string | null;
    onlyMine: boolean;
}

type RecipeSearchFormGroup = FormGroupControls<RecipeSearchFormValues>;
