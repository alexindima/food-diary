import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiImagePreviewDialogComponent } from 'fd-ui-kit/image-preview-dialog/fd-ui-image-preview-dialog.component';

import { AuthService } from '../../../services/auth.service';
import type { QualityGrade } from '../../../shared/models/quality-grade.data';
import { EntityCardComponent } from '../entity-card/entity-card.component';

const QUALITY_SCORE_MAX = 100;

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
    standalone: true,
    imports: [TranslatePipe, EntityCardComponent],
    templateUrl: './recipe-card.component.html',
    styleUrl: './recipe-card.component.scss',
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
    public readonly isFavorite = computed(() => Boolean(this.recipe().isFavorite));
    public readonly isAuthenticated = this.authService.isAuthenticated;
    public readonly canToggleFavorite = computed(() => this.isAuthenticated() && this.hasRecipeId());
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

        return Math.round(Math.min(QUALITY_SCORE_MAX, Math.max(0, score)));
    });
    public readonly hasPreviewImage = computed(() => (this.imageUrl()?.trim().length ?? 0) > 0);
    public readonly totalTime = computed(() => {
        const recipe = this.recipe();
        const prep = recipe.prepTime ?? 0;
        const cook = recipe.cookTime ?? 0;
        const total = prep + cook;
        return total > 0 ? total : null;
    });
    public readonly ingredientCount = computed(() => {
        const recipe = this.recipe();
        if (recipe.steps === null || recipe.steps === undefined || recipe.steps.length === 0) {
            return 0;
        }

        return recipe.steps.reduce((total, step) => total + (step.ingredients?.length ?? 0), 0);
    });
    public readonly description = computed(() => {
        const ingredients = `${this.translateService.instant('RECIPE_LIST.INGREDIENTS_COUNT')}: ${this.ingredientCount()}`;
        const totalTime = this.totalTime();
        if (totalTime === null || totalTime <= 0) {
            return ingredients;
        }

        return `${ingredients} - ${totalTime} ${this.translateService.instant('RECIPE_DETAIL.MIN')}`;
    });
    public handleOpen(): void {
        this.open.emit();
    }

    public handleAdd(): void {
        this.addToMeal.emit();
    }

    public handlePreview(): void {
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

    public toggleFavorite(): void {
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
