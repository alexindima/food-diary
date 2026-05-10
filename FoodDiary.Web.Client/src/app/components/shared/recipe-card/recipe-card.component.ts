import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiImagePreviewDialogComponent } from 'fd-ui-kit/image-preview-dialog/fd-ui-image-preview-dialog.component';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { finalize, of, switchMap } from 'rxjs';

import type { QualityGrade } from '../../../features/products/models/product.data';
import { FavoriteRecipeService } from '../../../features/recipes/api/favorite-recipe.service';
import type { FavoriteRecipe } from '../../../features/recipes/models/recipe.data';
import { AuthService } from '../../../services/auth.service';
import { EntityCardComponent } from '../entity-card/entity-card.component';

export interface RecipeCardStep {
    ingredients?: Array<unknown> | null;
}

export interface RecipeCardItem {
    id?: string;
    name: string;
    imageUrl?: string | null;
    isOwnedByCurrentUser: boolean;
    prepTime?: number | null;
    cookTime?: number | null;
    totalProteins?: number | null;
    totalFats?: number | null;
    totalCarbs?: number | null;
    totalFiber?: number | null;
    totalAlcohol?: number | null;
    totalCalories?: number | null;
    qualityScore?: number | null;
    qualityGrade?: QualityGrade | null;
    steps?: RecipeCardStep[] | null;
    isFavorite?: boolean;
    favoriteRecipeId?: string | null;
}

export interface RecipeFavoriteChange {
    isFavorite: boolean;
    favoriteRecipeId: string | null;
}

@Component({
    selector: 'fd-recipe-card',
    standalone: true,
    imports: [TranslatePipe, EntityCardComponent],
    templateUrl: './recipe-card.component.html',
    styleUrl: './recipe-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecipeCardComponent {
    private readonly dialogService = inject(FdUiDialogService);
    private readonly translateService = inject(TranslateService);
    private readonly favoriteRecipeService = inject(FavoriteRecipeService);
    private readonly authService = inject(AuthService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly toastService = inject(FdUiToastService);

    public readonly recipe = input.required<RecipeCardItem>();
    public readonly imageUrl = input.required<string | null | undefined>();
    public readonly open = output<void>();
    public readonly addToMeal = output<void>();
    public readonly favoriteChanged = output<RecipeFavoriteChange>();
    public readonly isFavorite = signal(false);
    public readonly isFavoriteLoading = signal(false);
    public readonly isAuthenticated = this.authService.isAuthenticated;
    public readonly canToggleFavorite = computed(() => this.isAuthenticated() && Boolean(this.recipe().id));
    public readonly favoriteAriaLabelKey = computed(() =>
        this.isFavorite() ? 'RECIPE_DETAIL.REMOVE_FAVORITE' : 'RECIPE_DETAIL.ADD_FAVORITE',
    );
    public readonly ownershipIcon = computed(() => (this.recipe().isOwnedByCurrentUser ? 'person' : 'groups'));
    public readonly nutrition = computed(() => ({
        proteins: this.recipe().totalProteins ?? 0,
        fats: this.recipe().totalFats ?? 0,
        carbs: this.recipe().totalCarbs ?? 0,
        fiber: this.recipe().totalFiber ?? 0,
        alcohol: this.recipe().totalAlcohol ?? 0,
    }));
    public readonly quality = computed(() => {
        const score = this.qualityScore();
        const grade = this.recipe().qualityGrade;
        return score === null || grade === null || grade === undefined ? null : { score, grade };
    });
    public readonly qualityScore = computed(() => {
        const score = this.recipe().qualityScore;
        if (score === null || score === undefined) {
            return null;
        }

        return Math.round(Math.min(100, Math.max(0, score)));
    });
    public readonly hasPreviewImage = computed(() => Boolean(this.imageUrl()?.trim()));
    public readonly totalTime = computed(() => {
        const recipe = this.recipe();
        const prep = recipe.prepTime ?? 0;
        const cook = recipe.cookTime ?? 0;
        const total = prep + cook;
        return total > 0 ? total : null;
    });
    public readonly ingredientCount = computed(() => {
        const recipe = this.recipe();
        if (!recipe.steps?.length) {
            return 0;
        }

        return recipe.steps.reduce((total, step) => total + (step.ingredients?.length ?? 0), 0);
    });
    public readonly description = computed(() => {
        const ingredients = `${this.translateService.instant('RECIPE_LIST.INGREDIENTS_COUNT')}: ${this.ingredientCount()}`;
        const totalTime = this.totalTime();
        if (!totalTime) {
            return ingredients;
        }

        return `${ingredients} - ${totalTime} ${this.translateService.instant('RECIPE_DETAIL.MIN')}`;
    });
    private favoriteRecipeId: string | null = null;

    public constructor() {
        effect(() => {
            const recipe = this.recipe();
            this.isFavorite.set(Boolean(recipe.isFavorite));
            this.favoriteRecipeId = recipe.favoriteRecipeId ?? null;
        });
    }

    public handleOpen(): void {
        this.open.emit();
    }

    public handleAdd(): void {
        this.addToMeal.emit();
    }

    public handlePreview(): void {
        const imageUrl = this.imageUrl()?.trim();
        if (!imageUrl) {
            return;
        }

        this.dialogService.open(FdUiImagePreviewDialogComponent, {
            size: 'lg',
            width: 'var(--fd-size-dialog-media-width)',
            maxWidth: 'var(--fd-size-dialog-media-max-width)',
            data: {
                imageUrl,
                alt: this.translateService.instant('IMAGE_PREVIEW.ALT', { name: this.recipe().name }),
                title: this.recipe().name,
            },
        });
    }

    public toggleFavorite(): void {
        const recipeId = this.recipe().id;
        if (!recipeId || this.isFavoriteLoading()) {
            return;
        }

        this.isFavoriteLoading.set(true);

        if (this.isFavorite()) {
            this.removeFavorite(recipeId);
            return;
        }

        this.favoriteRecipeService
            .add(recipeId, this.recipe().name)
            .pipe(
                takeUntilDestroyed(this.destroyRef),
                finalize(() => {
                    this.isFavoriteLoading.set(false);
                }),
            )
            .subscribe({
                next: favorite => {
                    this.favoriteRecipeId = favorite.id;
                    this.isFavorite.set(true);
                    this.favoriteChanged.emit({ isFavorite: true, favoriteRecipeId: favorite.id });
                },
                error: () => {
                    this.showFavoriteError();
                },
            });
    }

    private removeFavorite(recipeId: string): void {
        if (this.favoriteRecipeId) {
            this.favoriteRecipeService
                .remove(this.favoriteRecipeId)
                .pipe(
                    takeUntilDestroyed(this.destroyRef),
                    finalize(() => {
                        this.isFavoriteLoading.set(false);
                    }),
                )
                .subscribe({
                    next: () => {
                        this.favoriteRecipeId = null;
                        this.isFavorite.set(false);
                        this.favoriteChanged.emit({ isFavorite: false, favoriteRecipeId: null });
                    },
                    error: () => {
                        this.showFavoriteError();
                    },
                });
            return;
        }

        this.favoriteRecipeService
            .getAll()
            .pipe(
                switchMap(favorites => {
                    const match = favorites.find((favorite: FavoriteRecipe) => favorite.recipeId === recipeId);
                    if (!match) {
                        return of(null);
                    }

                    return this.favoriteRecipeService.remove(match.id);
                }),
                takeUntilDestroyed(this.destroyRef),
                finalize(() => {
                    this.isFavoriteLoading.set(false);
                }),
            )
            .subscribe({
                next: () => {
                    this.favoriteRecipeId = null;
                    this.isFavorite.set(false);
                    this.favoriteChanged.emit({ isFavorite: false, favoriteRecipeId: null });
                },
                error: () => {
                    this.showFavoriteError();
                },
            });
    }

    private showFavoriteError(): void {
        this.toastService.error(this.translateService.instant('ERRORS.FAVORITE_UPDATE_FAILED'));
    }
}
