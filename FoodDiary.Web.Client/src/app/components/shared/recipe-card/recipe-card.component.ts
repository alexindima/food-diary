import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, input, output, signal } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { finalize, of, switchMap } from 'rxjs';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiImagePreviewDialogComponent } from 'fd-ui-kit/image-preview-dialog/fd-ui-image-preview-dialog.component';
import { FavoriteRecipeService } from '../../../features/recipes/api/favorite-recipe.service';
import { AuthService } from '../../../services/auth.service';
import { QualityGrade } from '../../../features/products/models/product.data';
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

    public readonly recipe = input.required<RecipeCardItem>();
    public readonly imageUrl = input<string>();
    public readonly open = output<void>();
    public readonly addToMeal = output<void>();
    public readonly favoriteChanged = output<boolean>();
    public readonly isFavorite = signal(false);
    public readonly isFavoriteLoading = signal(false);
    public readonly isAuthenticated = this.authService.isAuthenticated;
    public readonly canToggleFavorite = computed(() => this.isAuthenticated() && Boolean(this.recipe().id));
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

    public handleAdd(): void {
        this.addToMeal.emit();
    }

    public hasPreviewImage(): boolean {
        return Boolean(this.imageUrl()?.trim());
    }

    public handlePreview(): void {
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
        const recipe = this.recipe();
        const prep = recipe.prepTime ?? 0;
        const cook = recipe.cookTime ?? 0;
        const total = prep + cook;
        return total > 0 ? total : null;
    }

    public getIngredientCount(): number {
        const recipe = this.recipe();
        if (!recipe.steps?.length) {
            return 0;
        }

        return recipe.steps.reduce((total, step) => total + (step.ingredients?.length ?? 0), 0);
    }

    public recipeTimeSuffix(): string {
        const minutes = this.getTotalTime();
        if (!minutes) {
            return '';
        }

        return ` - ${minutes} ${this.translateService.instant('RECIPE_DETAIL.MIN')}`;
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
