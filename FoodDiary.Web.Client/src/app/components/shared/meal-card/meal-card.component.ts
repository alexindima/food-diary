import { CommonModule, formatDate } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, input, LOCALE_ID, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiImagePreviewDialogComponent } from 'fd-ui-kit/image-preview-dialog/fd-ui-image-preview-dialog.component';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { finalize, of, switchMap } from 'rxjs';

import { FavoriteMealService } from '../../../features/meals/api/favorite-meal.service';
import { type QualityGrade } from '../../../features/products/models/product.data';
import { AuthService } from '../../../services/auth.service';
import { resolveMealImageUrl } from '../../../shared/lib/meal-image.util';
import { type EntityCardCollageImage, EntityCardComponent } from '../entity-card/entity-card.component';

export interface MealCardItem {
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
    aiSessions?: Array<{ imageUrl?: string | null; notes?: string | null; items?: Array<unknown> | null } | null> | null;
}

export interface MealFavoriteChange {
    isFavorite: boolean;
    favoriteMealId: string | null;
}

@Component({
    selector: 'fd-meal-card',
    standalone: true,
    imports: [CommonModule, TranslatePipe, EntityCardComponent],
    templateUrl: './meal-card.component.html',
    styleUrls: ['./meal-card.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealCardComponent {
    private readonly dialogService = inject(FdUiDialogService);
    private readonly translateService = inject(TranslateService);
    private readonly favoriteMealService = inject(FavoriteMealService);
    private readonly authService = inject(AuthService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly toastService = inject(FdUiToastService);
    private readonly locale = inject(LOCALE_ID);

    public readonly meal = input.required<MealCardItem>();
    public readonly open = output<void>();
    public readonly favoriteChanged = output<MealFavoriteChange>();
    public readonly isFavorite = signal(false);
    public readonly isFavoriteLoading = signal(false);
    public readonly isAuthenticated = this.authService.isAuthenticated;
    public readonly canToggleFavorite = computed(() => this.isAuthenticated() && Boolean(this.meal().id));
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

        return Math.round(Math.min(100, Math.max(0, score)));
    });
    private readonly fallbackMealImage = 'assets/images/stubs/meals/other.svg';
    private favoriteMealId: string | null = null;

    public readonly coverImage = computed(() => {
        const image = this.resolvePreviewImage();
        const resolved = resolveMealImageUrl(image ?? undefined, this.meal().mealType ?? undefined) ?? image;
        const itemImages = image ? [] : this.resolveItemImages();

        if (!image && itemImages.length === 1) {
            return itemImages[0].url;
        }

        if (!image && itemImages.length > 1) {
            return null;
        }

        return resolved ?? this.fallbackMealImage;
    });
    public readonly collageImages = computed<ReadonlyArray<EntityCardCollageImage>>(() => {
        if (this.resolvePreviewImage()) {
            return [];
        }

        const itemImages = this.resolveItemImages();
        return itemImages.length > 1 ? itemImages : [];
    });
    public readonly hasPreviewImage = computed(() => Boolean(this.resolvePreviewImage() || this.collageImages().length > 0));

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
        const normalizedMealType = mealType ? mealType.toUpperCase() : 'OTHER';
        return this.translateService.instant(`MEAL_CARD.MEAL_TYPES.${normalizedMealType}`);
    });
    private readonly favoriteStateEffect = effect(() => {
        const meal = this.meal();
        this.isFavorite.set(meal.isFavorite ?? false);
        this.favoriteMealId = meal.favoriteMealId ?? null;
    });

    public handleOpen(): void {
        this.open.emit();
    }

    public handlePreview(): void {
        const imageUrl = this.resolvePreviewImage();
        const collageImages = imageUrl ? [] : this.collageImages();
        if (!imageUrl && collageImages.length === 0) {
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
        if (this.isFavoriteLoading()) {
            return;
        }

        this.isFavoriteLoading.set(true);

        if (this.isFavorite()) {
            this.removeFavorite();
            return;
        }

        this.favoriteMealService
            .add(this.meal().id)
            .pipe(
                takeUntilDestroyed(this.destroyRef),
                finalize(() => {
                    this.isFavoriteLoading.set(false);
                }),
            )
            .subscribe({
                next: favorite => {
                    this.favoriteMealId = favorite.id;
                    this.isFavorite.set(true);
                    this.favoriteChanged.emit({ isFavorite: true, favoriteMealId: favorite.id });
                },
                error: () => {
                    this.showFavoriteError();
                },
            });
    }

    private removeFavorite(): void {
        if (this.favoriteMealId) {
            this.favoriteMealService
                .remove(this.favoriteMealId)
                .pipe(
                    takeUntilDestroyed(this.destroyRef),
                    finalize(() => {
                        this.isFavoriteLoading.set(false);
                    }),
                )
                .subscribe({
                    next: () => {
                        this.favoriteMealId = null;
                        this.isFavorite.set(false);
                        this.favoriteChanged.emit({ isFavorite: false, favoriteMealId: null });
                    },
                    error: () => {
                        this.showFavoriteError();
                    },
                });
            return;
        }

        this.favoriteMealService
            .getAll()
            .pipe(
                switchMap(favorites => {
                    const match = favorites.find(favorite => favorite.mealId === this.meal().id);
                    if (!match) {
                        return of(null);
                    }

                    return this.favoriteMealService.remove(match.id);
                }),
                takeUntilDestroyed(this.destroyRef),
                finalize(() => {
                    this.isFavoriteLoading.set(false);
                }),
            )
            .subscribe({
                next: () => {
                    this.favoriteMealId = null;
                    this.isFavorite.set(false);
                    this.favoriteChanged.emit({ isFavorite: false, favoriteMealId: null });
                },
                error: () => {
                    this.showFavoriteError();
                },
            });
    }

    private showFavoriteError(): void {
        this.toastService.error(this.translateService.instant('ERRORS.FAVORITE_UPDATE_FAILED'));
    }

    private resolvePreviewImage(): string | undefined {
        const explicitImage = this.resolveExplicitPreviewImage();
        if (explicitImage) {
            return explicitImage;
        }

        const itemImages = this.resolveItemImages();
        return itemImages.length === 1 ? itemImages[0].url : undefined;
    }

    private resolveExplicitPreviewImage(): string | undefined {
        const mealImage = this.meal().imageUrl?.trim();
        if (mealImage) {
            return mealImage;
        }

        return undefined;
    }

    private resolveItemImages(): EntityCardCollageImage[] {
        const seen = new Set<string>();
        const result: EntityCardCollageImage[] = [];

        for (const item of this.meal().items ?? []) {
            const imageUrl = item?.product?.imageUrl?.trim() || item?.recipe?.imageUrl?.trim();
            if (!imageUrl || seen.has(imageUrl)) {
                continue;
            }

            seen.add(imageUrl);
            result.push({
                url: imageUrl,
                alt: item?.product?.name?.trim() || item?.recipe?.name?.trim() || this.mealTitle(),
            });

            if (result.length === 4) {
                break;
            }
        }

        if (result.length < 4) {
            for (const session of this.meal().aiSessions ?? []) {
                const imageUrl = session?.imageUrl?.trim();
                if (!imageUrl || seen.has(imageUrl)) {
                    continue;
                }

                seen.add(imageUrl);
                result.push({
                    url: imageUrl,
                    alt: session?.notes?.trim() || this.mealTitle(),
                });

                if (result.length === 4) {
                    break;
                }
            }
        }

        return result;
    }
}
