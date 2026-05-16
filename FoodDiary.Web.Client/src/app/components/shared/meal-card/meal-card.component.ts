import { CommonModule, formatDate } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, LOCALE_ID, output } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiImagePreviewDialogComponent } from 'fd-ui-kit/image-preview-dialog/fd-ui-image-preview-dialog.component';

import { AuthService } from '../../../services/auth.service';
import { resolveMealImageUrl } from '../../../shared/lib/meal-image.util';
import { normalizeQualityScore } from '../../../shared/lib/quality-score.utils';
import type { QualityGrade } from '../../../shared/models/quality-grade.data';
import { EntityCardComponent } from '../entity-card/entity-card.component';
import type { EntityCardCollageImage } from '../entity-card/entity-card.types';

export type MealCardItem = {
    id: string;
    date: string | Date;
    mealType?: string | null;
    imageUrl?: string | null;
    totalCalories: number;
    totalProteins: number;
    totalFats: number;
    totalCarbs: number;
    totalFiber: number;
    totalAlcohol: number;
    qualityScore?: number | null;
    qualityGrade?: QualityGrade | null;
    isFavorite?: boolean;
    favoriteMealId?: string | null;
    items?: Array<{
        product?: { imageUrl?: string | null; name?: string | null } | null;
        recipe?: { imageUrl?: string | null; name?: string | null } | null;
    } | null> | null;
    aiSessions?: Array<{ imageUrl?: string | null; notes?: string | null; items?: unknown[] | null } | null> | null;
};

const COLLAGE_IMAGE_LIMIT = 4;

@Component({
    selector: 'fd-meal-card',
    imports: [CommonModule, TranslatePipe, EntityCardComponent],
    templateUrl: './meal-card.component.html',
    styleUrls: ['./meal-card.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealCardComponent {
    private readonly dialogService = inject(FdUiDialogService);
    private readonly translateService = inject(TranslateService);
    private readonly authService = inject(AuthService);
    private readonly locale = inject(LOCALE_ID);

    public readonly meal = input.required<MealCardItem>();
    public readonly favoriteLoading = input(false);
    public readonly open = output();
    public readonly favoriteToggle = output();
    public readonly isFavorite = computed(() => Boolean(this.meal().isFavorite));
    public readonly isAuthenticated = this.authService.isAuthenticated;
    public readonly canToggleFavorite = computed(() => this.isAuthenticated() && this.meal().id.length > 0);
    public readonly favoriteAriaLabelKey = computed(() =>
        this.isFavorite() ? 'CONSUMPTION_DETAIL.REMOVE_FAVORITE' : 'CONSUMPTION_DETAIL.ADD_FAVORITE',
    );
    public readonly nutrition = computed(() => ({
        proteins: this.meal().totalProteins,
        fats: this.meal().totalFats,
        carbs: this.meal().totalCarbs,
        fiber: this.meal().totalFiber,
        alcohol: this.meal().totalAlcohol,
    }));
    public readonly quality = computed(() => {
        const score = this.qualityScore();
        const grade = this.meal().qualityGrade;
        return score === null || grade === null || grade === undefined ? null : { score, grade };
    });
    public readonly qualityScore = computed(() => {
        const score = this.meal().qualityScore;
        if (score === null || score === undefined) {
            return null;
        }

        return normalizeQualityScore(score);
    });
    private readonly fallbackMealImage = 'assets/images/stubs/meals/other.svg';

    public readonly coverImage = computed(() => {
        const image = this.resolvePreviewImage();
        const resolved = resolveMealImageUrl(image ?? undefined, this.meal().mealType ?? undefined) ?? image;
        const itemImages = image !== undefined ? [] : this.resolveItemImages();

        if (image === undefined && itemImages.length === 1) {
            return itemImages[0].url;
        }

        if (image === undefined && itemImages.length > 1) {
            return null;
        }

        return resolved ?? this.fallbackMealImage;
    });
    public readonly collageImages = computed<readonly EntityCardCollageImage[]>(() => {
        if (this.resolvePreviewImage() !== undefined) {
            return [];
        }

        const itemImages = this.resolveItemImages();
        return itemImages.length > 1 ? itemImages : [];
    });
    public readonly hasPreviewImage = computed(() => this.resolvePreviewImage() !== undefined || this.collageImages().length > 0);

    public readonly itemCount = computed(() => {
        const meal = this.meal();
        const manualCount = meal.items?.length ?? 0;
        const aiCount = meal.aiSessions?.reduce((total, session) => total + (session?.items?.length ?? 0), 0) ?? 0;
        return manualCount + aiCount;
    });
    public readonly description = computed(() => `${this.translateService.instant('MEAL_CARD.ITEM_COUNT')}: ${this.itemCount()}`);
    public readonly mealTime = computed(() => formatDate(this.meal().date, 'HH:mm', this.locale));
    public readonly mealTitle = computed(() => {
        const mealType = this.meal().mealType?.trim();
        const normalizedMealType = mealType !== undefined && mealType.length > 0 ? mealType.toUpperCase() : 'OTHER';
        return this.translateService.instant(`MEAL_CARD.MEAL_TYPES.${normalizedMealType}`);
    });
    public handleOpen(): void {
        this.open.emit();
    }

    public handlePreview(): void {
        const imageUrl = this.resolvePreviewImage();
        const collageImages = imageUrl !== undefined ? [] : this.collageImages();
        if (imageUrl === undefined && collageImages.length === 0) {
            return;
        }

        this.dialogService.open(FdUiImagePreviewDialogComponent, {
            size: 'lg',
            width: 'var(--fd-size-dialog-media-width)',
            maxWidth: 'var(--fd-size-dialog-media-max-width)',
            data: {
                imageUrl,
                collageImages,
                alt: this.translateService.instant('IMAGE_PREVIEW.ALT', {
                    name: this.mealTitle(),
                }),
                title: this.mealTitle(),
            },
        });
    }

    public toggleFavorite(): void {
        if (this.favoriteLoading()) {
            return;
        }

        this.favoriteToggle.emit();
    }

    private resolvePreviewImage(): string | undefined {
        const explicitImage = this.resolveExplicitPreviewImage();
        if (explicitImage !== undefined) {
            return explicitImage;
        }

        const itemImages = this.resolveItemImages();
        return itemImages.length === 1 ? itemImages[0].url : undefined;
    }

    private resolveExplicitPreviewImage(): string | undefined {
        const mealImage = this.meal().imageUrl?.trim();
        if (mealImage !== undefined && mealImage.length > 0) {
            return mealImage;
        }

        return undefined;
    }

    private resolveItemImages(): EntityCardCollageImage[] {
        const seen = new Set<string>();
        const result: EntityCardCollageImage[] = [];

        this.appendItemImages(result, seen);
        this.appendAiSessionImages(result, seen);

        return result;
    }

    private appendItemImages(result: EntityCardCollageImage[], seen: Set<string>): void {
        for (const item of this.meal().items ?? []) {
            const image = this.resolveMealItemCollageImage(item);
            if (!this.appendCollageImage(result, seen, image.url, image.alt)) {
                break;
            }
        }
    }

    private appendAiSessionImages(result: EntityCardCollageImage[], seen: Set<string>): void {
        if (result.length === COLLAGE_IMAGE_LIMIT) {
            return;
        }

        for (const session of this.meal().aiSessions ?? []) {
            if (!this.appendCollageImage(result, seen, session?.imageUrl?.trim(), session?.notes?.trim())) {
                break;
            }
        }
    }

    private appendCollageImage(
        result: EntityCardCollageImage[],
        seen: Set<string>,
        imageUrl: string | undefined,
        alt: string | undefined,
    ): boolean {
        if (imageUrl === undefined || imageUrl.length === 0 || seen.has(imageUrl)) {
            return true;
        }

        seen.add(imageUrl);
        result.push({
            url: imageUrl,
            alt: alt ?? this.mealTitle(),
        });

        return result.length < COLLAGE_IMAGE_LIMIT;
    }

    private resolveMealItemCollageImage(item: NonNullable<NonNullable<MealCardItem['items']>[number]> | null): {
        url: string | undefined;
        alt: string | undefined;
    } {
        return {
            url: this.resolveMealItemProductImage(item) ?? this.resolveMealItemRecipeImage(item),
            alt: this.resolveMealItemProductName(item) ?? this.resolveMealItemRecipeName(item),
        };
    }

    private resolveMealItemProductImage(item: NonNullable<NonNullable<MealCardItem['items']>[number]> | null): string | undefined {
        return item?.product?.imageUrl?.trim();
    }

    private resolveMealItemRecipeImage(item: NonNullable<NonNullable<MealCardItem['items']>[number]> | null): string | undefined {
        return item?.recipe?.imageUrl?.trim();
    }

    private resolveMealItemProductName(item: NonNullable<NonNullable<MealCardItem['items']>[number]> | null): string | undefined {
        return item?.product?.name?.trim();
    }

    private resolveMealItemRecipeName(item: NonNullable<NonNullable<MealCardItem['items']>[number]> | null): string | undefined {
        return item?.recipe?.name?.trim();
    }
}
