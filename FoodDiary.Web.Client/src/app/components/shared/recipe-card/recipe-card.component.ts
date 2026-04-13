import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { finalize, of, switchMap } from 'rxjs';
import { FdUiIconModule } from 'fd-ui-kit/material';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { NutrientBadgesComponent } from '../nutrient-badges/nutrient-badges.component';
import { MediaCardComponent } from '../media-card/media-card.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiImagePreviewDialogComponent } from 'fd-ui-kit/image-preview-dialog/fd-ui-image-preview-dialog.component';
import { FavoriteRecipeService } from '../../../features/recipes/api/favorite-recipe.service';
import { AuthService } from '../../../services/auth.service';

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
    steps?: RecipeCardStep[] | null;
}

@Component({
    selector: 'fd-recipe-card',
    standalone: true,
    imports: [CommonModule, TranslatePipe, FdUiIconModule, FdUiButtonComponent, NutrientBadgesComponent, MediaCardComponent],
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

    public readonly recipe = input.required<RecipeCardItem>();
    public readonly imageUrl = input<string>();
    public readonly open = output<void>();
    public readonly addToMeal = output<void>();
    public readonly favoriteChanged = output<boolean>();
    public readonly isFavorite = signal(false);
    public readonly isFavoriteLoading = signal(false);
    public readonly isAuthenticated = this.authService.isAuthenticated;
    public readonly canToggleFavorite = computed(() => this.isAuthenticated() && Boolean(this.recipe().id));
    private favoriteRecipeId: string | null = null;

    public ngOnInit(): void {
        const recipeId = this.recipe().id;
        if (!recipeId || !this.isAuthenticated()) {
            return;
        }

        this.favoriteRecipeService
            .isFavorite(recipeId)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(isFav => this.isFavorite.set(isFav));
    }

    public handleOpen(): void {
        this.open.emit();
    }

    public handleAdd(event: Event): void {
        event.stopPropagation();
        this.addToMeal.emit();
    }

    public hasPreviewImage(): boolean {
        return Boolean(this.imageUrl()?.trim());
    }

    public handlePreview(event: Event): void {
        event.stopPropagation();

        const imageUrl = this.imageUrl()?.trim();
        if (!imageUrl) {
            return;
        }

        this.dialogService.open(FdUiImagePreviewDialogComponent, {
            size: 'lg',
            width: 'min(calc(100vw - 3rem), 1200px)',
            maxWidth: '1200px',
            data: {
                imageUrl,
                alt: this.translateService.instant('IMAGE_PREVIEW.ALT', { name: this.recipe().name }),
                title: this.recipe().name,
            },
        });
    }

    public getTotalTime(): number | null {
        const r = this.recipe();
        const prep = r?.prepTime ?? 0;
        const cook = r?.cookTime ?? 0;
        const total = prep + cook;
        return total > 0 ? total : null;
    }

    public getIngredientCount(): number {
        const r = this.recipe();
        if (!r?.steps?.length) {
            return 0;
        }

        return r.steps.reduce((total, step) => total + (step.ingredients?.length ?? 0), 0);
    }

    public toggleFavorite(event: Event): void {
        event.stopPropagation();

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
                finalize(() => this.isFavoriteLoading.set(false)),
            )
            .subscribe({
                next: favorite => {
                    this.favoriteRecipeId = favorite.id;
                    this.isFavorite.set(true);
                    this.favoriteChanged.emit(true);
                },
            });
    }

    private removeFavorite(recipeId: string): void {
        if (this.favoriteRecipeId) {
            this.favoriteRecipeService
                .remove(this.favoriteRecipeId)
                .pipe(
                    takeUntilDestroyed(this.destroyRef),
                    finalize(() => this.isFavoriteLoading.set(false)),
                )
                .subscribe({
                    next: () => {
                        this.favoriteRecipeId = null;
                        this.isFavorite.set(false);
                        this.favoriteChanged.emit(false);
                    },
                });
            return;
        }

        this.favoriteRecipeService
            .getAll()
            .pipe(
                switchMap(favorites => {
                    const match = favorites.find(favorite => favorite.recipeId === recipeId);
                    if (!match) {
                        return of(null);
                    }

                    return this.favoriteRecipeService.remove(match.id);
                }),
                takeUntilDestroyed(this.destroyRef),
                finalize(() => this.isFavoriteLoading.set(false)),
            )
            .subscribe({
                next: () => {
                    this.favoriteRecipeId = null;
                    this.isFavorite.set(false);
                    this.favoriteChanged.emit(false);
                },
            });
    }
}
