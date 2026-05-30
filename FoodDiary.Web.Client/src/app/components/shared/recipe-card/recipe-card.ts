import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiImagePreviewDialogComponent } from 'fd-ui-kit/image-preview-dialog/fd-ui-image-preview-dialog';

import { AuthService } from '../../../services/auth.service';
import { normalizeQualityScore } from '../../../shared/lib/quality-score.utils';
import type { QualityGrade } from '../../../shared/models/quality-grade.data';
import { EntityCardComponent } from '../entity-card/entity-card';

export type RecipeCardStep = {
    ingredients?: unknown[] | null;
};

export type RecipeCardItem = {
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
};

@Component({
    selector: 'fd-recipe-card',
    imports: [TranslatePipe, EntityCardComponent],
    templateUrl: './recipe-card.html',
    styleUrl: './recipe-card.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecipeCardComponent {
    private readonly dialogService = inject(FdUiDialogService);
    private readonly translateService = inject(TranslateService);
    private readonly authService = inject(AuthService);

    public readonly recipe = input.required<RecipeCardItem>();
    public readonly imageUrl = input.required<string | null | undefined>();
    public readonly favoriteLoading = input(false);
    public readonly open = output();
    public readonly addToMeal = output();
    public readonly favoriteToggle = output();
    protected readonly isFavorite = computed(() => Boolean(this.recipe().isFavorite));
    protected readonly isAuthenticated = this.authService.isAuthenticated;
    protected readonly canToggleFavorite = computed(() => this.isAuthenticated() && this.hasRecipeId());
    protected readonly favoriteAriaLabelKey = computed(() =>
        this.isFavorite() ? 'RECIPE_DETAIL.REMOVE_FAVORITE' : 'RECIPE_DETAIL.ADD_FAVORITE',
    );
    protected readonly ownershipIcon = computed(() => (this.recipe().isOwnedByCurrentUser ? 'person' : 'groups'));
    protected readonly nutrition = computed(() => ({
        proteins: this.recipe().totalProteins ?? 0,
        fats: this.recipe().totalFats ?? 0,
        carbs: this.recipe().totalCarbs ?? 0,
        fiber: this.recipe().totalFiber ?? 0,
        alcohol: this.recipe().totalAlcohol ?? 0,
    }));
    protected readonly quality = computed(() => {
        const score = this.qualityScore();
        const grade = this.recipe().qualityGrade;
        return score === null || grade === null || grade === undefined ? null : { score, grade };
    });
    protected readonly qualityScore = computed(() => {
        const score = this.recipe().qualityScore;
        if (score === null || score === undefined) {
            return null;
        }

        return normalizeQualityScore(score);
    });
    protected readonly hasPreviewImage = computed(() => (this.imageUrl()?.trim().length ?? 0) > 0);
    protected readonly totalTime = computed(() => {
        const recipe = this.recipe();
        const prep = recipe.prepTime ?? 0;
        const cook = recipe.cookTime ?? 0;
        const total = prep + cook;
        return total > 0 ? total : null;
    });
    protected readonly ingredientCount = computed(() => {
        const recipe = this.recipe();
        if (recipe.steps === null || recipe.steps === undefined || recipe.steps.length === 0) {
            return 0;
        }

        return recipe.steps.reduce((total, step) => total + (step.ingredients?.length ?? 0), 0);
    });
    protected readonly description = computed(() => {
        const ingredients = `${this.translateService.instant('RECIPE_LIST.INGREDIENTS_COUNT')}: ${this.ingredientCount()}`;
        const totalTime = this.totalTime();
        if (totalTime === null || totalTime <= 0) {
            return ingredients;
        }

        return `${ingredients} - ${totalTime} ${this.translateService.instant('RECIPE_DETAIL.MIN')}`;
    });
    protected openCard(): void {
        this.open.emit();
    }

    protected addToMealFromCard(): void {
        this.addToMeal.emit();
    }

    protected previewCardImage(): void {
        const imageUrl = this.imageUrl()?.trim();
        if (imageUrl === undefined || imageUrl.length === 0) {
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

    protected toggleFavorite(): void {
        const recipeId = this.recipe().id;
        if (recipeId === undefined || recipeId.length === 0 || this.favoriteLoading()) {
            return;
        }

        this.favoriteToggle.emit();
    }

    private hasRecipeId(): boolean {
        const id = this.recipe().id;
        return id !== undefined && id.length > 0;
    }
}
