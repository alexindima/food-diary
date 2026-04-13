import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ChangeDetectionStrategy, Component, DestroyRef, LOCALE_ID, computed, inject, input, output, signal } from '@angular/core';
import { CommonModule, formatDate } from '@angular/common';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { finalize, of, switchMap } from 'rxjs';
import { resolveMealImageUrl } from '../../../shared/lib/meal-image.util';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiImagePreviewDialogComponent } from 'fd-ui-kit/image-preview-dialog/fd-ui-image-preview-dialog.component';
import { FavoriteMealService } from '../../../features/meals/api/favorite-meal.service';
import { AuthService } from '../../../services/auth.service';
import { QualityGrade } from '../../../features/products/models/product.data';
import { EntityCardComponent } from '../entity-card/entity-card.component';

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
    items?: Array<unknown> | null;
    aiSessions?: Array<{ items?: Array<unknown> | null } | null> | null;
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
    private readonly locale = inject(LOCALE_ID);

    public readonly meal = input.required<MealCardItem>();
    public readonly open = output<void>();
    public readonly favoriteChanged = output<boolean>();
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
        const image = this.meal().imageUrl?.trim();
        const resolved = resolveMealImageUrl(image ?? undefined, this.meal().mealType ?? undefined) ?? image;
        return resolved ?? this.fallbackMealImage;
    });

    public readonly itemCount = computed(() => {
        const meal = this.meal();
        const manualCount = meal.items?.length ?? 0;
        const aiCount = meal.aiSessions?.reduce((total, session) => total + (session?.items?.length ?? 0), 0) ?? 0;
        return manualCount + aiCount;
    });
    public readonly mealTime = computed(() => formatDate(this.meal().date, 'HH:mm', this.locale));
    public readonly mealTitle = computed(() =>
        this.meal().mealType
            ? this.translateService.instant(`MEAL_CARD.MEAL_TYPES.${this.meal().mealType}`)
            : this.translateService.instant('MEAL_CARD.MEAL_TYPES.OTHER'),
    );

    public ngOnInit(): void {
        if (!this.isAuthenticated()) {
            return;
        }

        this.favoriteMealService
            .isFavorite(this.meal().id)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(isFav => this.isFavorite.set(isFav));
    }

    public handleOpen(): void {
        this.open.emit();
    }

    public hasPreviewImage(): boolean {
        return Boolean(this.meal().imageUrl?.trim());
    }

    public handlePreview(): void {
        const imageUrl = this.meal().imageUrl?.trim();
        if (!imageUrl) {
            return;
        }

        this.dialogService.open(FdUiImagePreviewDialogComponent, {
            size: 'lg',
            width: 'min(calc(100vw - 3rem), 1200px)',
            maxWidth: '1200px',
            data: {
                imageUrl,
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
                finalize(() => this.isFavoriteLoading.set(false)),
            )
            .subscribe({
                next: favorite => {
                    this.favoriteMealId = favorite.id;
                    this.isFavorite.set(true);
                    this.favoriteChanged.emit(true);
                },
            });
    }

    private removeFavorite(): void {
        if (this.favoriteMealId) {
            this.favoriteMealService
                .remove(this.favoriteMealId)
                .pipe(
                    takeUntilDestroyed(this.destroyRef),
                    finalize(() => this.isFavoriteLoading.set(false)),
                )
                .subscribe({
                    next: () => {
                        this.favoriteMealId = null;
                        this.isFavorite.set(false);
                        this.favoriteChanged.emit(false);
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
                finalize(() => this.isFavoriteLoading.set(false)),
            )
            .subscribe({
                next: () => {
                    this.favoriteMealId = null;
                    this.isFavorite.set(false);
                    this.favoriteChanged.emit(false);
                },
            });
    }
}
