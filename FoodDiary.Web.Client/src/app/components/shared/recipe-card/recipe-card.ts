import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiImagePreviewDialogComponent } from 'fd-ui-kit/image-preview-dialog/fd-ui-image-preview-dialog';

import { AuthService } from '../../../services/auth.service';
import { injectCurrentLanguage } from '../../../shared/i18n/inject-current-language';
import { injectImagePreviews } from '../../../shared/lib/image-owner-preview';
import { normalizeQualityScore } from '../../../shared/lib/quality-score.utils';
import { distinctRecipePhotoSources, recipePhotoSources, recipeUsesOwnerImagePreviews } from '../../../shared/lib/recipe-image.util';
import { recipeNutrientPerServing } from '../../../shared/lib/recipe-nutrition-display.utils';
import { capitalizeFirstLetter } from '../../../shared/lib/text-display.utils';
import type { QualityGrade } from '../../../shared/models/quality-grade.data';
import type { RecipePublicationStatus, RecipeVisibility } from '../../../shared/models/recipe.data';
import { EntityCardComponent } from '../entity-card/entity-card';

export type RecipeCardStep = {
    images?: Array<{ imageUrl: string; imageAssetId?: string | null }>;
    ingredients?: Array<{ textName?: string | null; productName?: string | null; nestedRecipeName?: string | null }> | null;
    imageUrl?: string | null;
    imageAssetId?: string | null;
};

export type RecipeCardItem = {
    id?: string;
    name: string;
    comment?: string | null;
    imageUrl?: string | null;
    imageAssetId?: string | null;
    images?: Array<{ imageUrl: string; imageAssetId?: string | null }>;
    isOwnedByCurrentUser: boolean;
    visibility?: RecipeVisibility;
    publicationStatus?: RecipePublicationStatus | null;
    prepTime?: number | null;
    cookTime?: number | null;
    servings: number;
    totalProteins?: number | null;
    totalFats?: number | null;
    totalCarbs?: number | null;
    totalFiber?: number | null;
    totalAlcohol?: number | null;
    totalCalories?: number | null;
    missingIngredientCount?: number;
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
    public readonly showOwnership = input(true);
    public readonly catalogLayout = input(false);
    public readonly open = output();
    public readonly addToMeal = output();
    public readonly favoriteToggle = output();
    protected readonly isFavorite = computed(() => Boolean(this.recipe().isFavorite));
    protected readonly isAuthenticated = this.authService.isAuthenticated;
    protected readonly canToggleFavorite = computed(() => this.isAuthenticated() && this.hasRecipeId());
    protected readonly favoriteAriaLabelKey = computed(() =>
        this.isFavorite() ? 'RECIPE_DETAIL.REMOVE_FAVORITE' : 'RECIPE_DETAIL.ADD_FAVORITE',
    );
    protected readonly ownershipIcon = computed(() => {
        if (!this.showOwnership()) {
            return null;
        }

        return this.recipe().isOwnedByCurrentUser ? 'person' : 'groups';
    });
    protected readonly calories = computed(() => recipeNutrientPerServing(this.recipe().totalCalories, this.recipe().servings));
    protected readonly nutrition = computed(() => {
        const recipe = this.recipe();
        return {
            proteins: recipeNutrientPerServing(recipe.totalProteins, recipe.servings) ?? 0,
            fats: recipeNutrientPerServing(recipe.totalFats, recipe.servings) ?? 0,
            carbs: recipeNutrientPerServing(recipe.totalCarbs, recipe.servings) ?? 0,
            fiber: recipeNutrientPerServing(recipe.totalFiber, recipe.servings) ?? 0,
            alcohol: recipeNutrientPerServing(recipe.totalAlcohol, recipe.servings) ?? 0,
        };
    });
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
    private readonly useOwnerPreviews = computed(() => recipeUsesOwnerImagePreviews(this.recipe()));
    private readonly gallerySources = computed(() => {
        const recipe = this.recipe();
        const requiresOwner = this.useOwnerPreviews();
        return distinctRecipePhotoSources([
            ...recipePhotoSources(recipe, requiresOwner),
            ...(recipe.steps ?? []).flatMap(step => recipePhotoSources(step, requiresOwner)),
        ]);
    });
    private readonly coverPreview = injectImagePreviews(computed(() => this.gallerySources().slice(0, 1)));
    private readonly privateGalleryRequested = signal(false);
    private readonly privateGalleryPreviews = injectImagePreviews(
        computed(() => (this.privateGalleryRequested() ? this.gallerySources().slice(1) : [])),
    );
    protected readonly displayImage = computed(() =>
        this.gallerySources().length === 0 ? this.imageUrl() : (this.coverPreview.urls()[0] ?? null),
    );
    protected readonly hasPreviewImage = computed(() => {
        const cover = this.displayImage();
        return this.gallerySources().length > 0 && cover !== null && cover !== undefined;
    });
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
    private readonly language = injectCurrentLanguage();
    protected readonly description = computed(() => {
        const count = this.ingredientCount();
        const category = new Intl.PluralRules(this.language()).select(count).toUpperCase();
        const ingredients = this.translateService.instant(`RECIPE_CARD.INGREDIENTS_${category}`, { count });
        const timeAndIngredients =
            this.totalTime() === null
                ? ingredients
                : `${ingredients} · ${this.totalTime()} ${this.translateService.instant('RECIPE_DETAIL.MIN')}`;
        const publicationStatus = this.recipe().publicationStatus;
        const metadata =
            publicationStatus !== null && publicationStatus !== undefined
                ? `${this.translateService.instant(`RECIPE_PUBLICATION.${publicationStatus}`)} · ${timeAndIngredients}`
                : timeAndIngredients;
        if (this.catalogLayout()) {
            return metadata;
        }

        const names = (this.recipe().steps ?? [])
            .flatMap(step => step.ingredients ?? [])
            .map(item => capitalizeFirstLetter(item.textName ?? item.productName ?? item.nestedRecipeName))
            .filter(Boolean);
        return names.length > 0 ? `${metadata} · ${[...new Set(names)].join(', ')}` : metadata;
    });
    public constructor() {
        effect(() => {
            if (!this.privateGalleryRequested() || this.privateGalleryPreviews.loading()) {
                return;
            }
            this.openImageGallery([this.coverPreview.urls()[0], ...this.privateGalleryPreviews.urls()]);
            this.privateGalleryRequested.set(false);
        });
    }

    protected openCard(): void {
        this.open.emit();
    }

    protected addToMealFromCard(): void {
        this.addToMeal.emit();
    }

    protected previewCardImage(): void {
        if (this.useOwnerPreviews()) {
            this.privateGalleryRequested.set(true);
            return;
        }
        this.openImageGallery(this.gallerySources().map(image => image.url));
    }

    private openImageGallery(urls: ReadonlyArray<string | null | undefined>): void {
        const images = urls.filter((url): url is string => typeof url === 'string' && url.length > 0);
        if (images.length === 0) {
            return;
        }

        this.dialogService.open(FdUiImagePreviewDialogComponent, {
            size: 'lg',
            width: 'var(--fd-dialog-panel-width-lg)',
            maxWidth: 'var(--fd-size-dialog-media-max-width)',
            data: {
                collageImages: images.map(url => ({ url, alt: this.recipe().name })),
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
