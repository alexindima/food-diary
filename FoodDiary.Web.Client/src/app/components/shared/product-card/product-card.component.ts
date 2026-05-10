import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiImagePreviewDialogComponent } from 'fd-ui-kit/image-preview-dialog/fd-ui-image-preview-dialog.component';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { finalize, of, switchMap } from 'rxjs';

import { FavoriteProductService } from '../../../features/products/api/favorite-product.service';
import { type FavoriteProduct, type QualityGrade } from '../../../features/products/models/product.data';
import { AuthService } from '../../../services/auth.service';
import { EntityCardComponent } from '../entity-card/entity-card.component';

export interface ProductCardItem {
    id?: string;
    name: string;
    brand?: string | null;
    barcode?: string | null;
    isOwnedByCurrentUser: boolean;
    proteinsPerBase: number;
    fatsPerBase: number;
    carbsPerBase: number;
    fiberPerBase: number;
    alcoholPerBase: number;
    caloriesPerBase: number;
    qualityScore?: number | null;
    qualityGrade?: QualityGrade | null;
    isFavorite?: boolean;
    favoriteProductId?: string | null;
}

export interface ProductFavoriteChange {
    isFavorite: boolean;
    favoriteProductId: string | null;
}

@Component({
    selector: 'fd-product-card',
    standalone: true,
    imports: [TranslatePipe, EntityCardComponent],
    templateUrl: './product-card.component.html',
    styleUrl: './product-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductCardComponent {
    private readonly dialogService = inject(FdUiDialogService);
    private readonly translateService = inject(TranslateService);
    private readonly favoriteProductService = inject(FavoriteProductService);
    private readonly authService = inject(AuthService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly toastService = inject(FdUiToastService);

    public readonly product = input.required<ProductCardItem>();
    public readonly imageUrl = input.required<string | null | undefined>();
    public readonly open = output<void>();
    public readonly addToMeal = output<void>();
    public readonly favoriteChanged = output<ProductFavoriteChange>();
    public readonly isFavorite = signal(false);
    public readonly isFavoriteLoading = signal(false);
    public readonly isAuthenticated = this.authService.isAuthenticated;
    public readonly canToggleFavorite = computed(() => this.isAuthenticated() && Boolean(this.product().id));
    public readonly favoriteAriaLabelKey = computed(() =>
        this.isFavorite() ? 'PRODUCT_DETAIL.REMOVE_FAVORITE' : 'PRODUCT_DETAIL.ADD_FAVORITE',
    );
    public readonly ownershipIcon = computed(() => (this.product().isOwnedByCurrentUser ? 'person' : 'groups'));
    public readonly nutrition = computed(() => ({
        proteins: this.product().proteinsPerBase,
        fats: this.product().fatsPerBase,
        carbs: this.product().carbsPerBase,
        fiber: this.product().fiberPerBase,
        alcohol: this.product().alcoholPerBase,
    }));
    public readonly quality = computed(() => {
        const score = this.qualityScore();
        const grade = this.product().qualityGrade;
        return score === null || grade === null || grade === undefined ? null : { score, grade };
    });
    public readonly qualityScore = computed(() => {
        const score = this.product().qualityScore;
        if (score === null || score === undefined) {
            return null;
        }

        return Math.round(Math.min(100, Math.max(0, score)));
    });
    public readonly hasPreviewImage = computed(() => Boolean(this.imageUrl()?.trim()));
    private favoriteProductId: string | null = null;

    public constructor() {
        effect(() => {
            const product = this.product();
            this.isFavorite.set(Boolean(product.isFavorite));
            this.favoriteProductId = product.favoriteProductId ?? null;
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
                alt: this.translateService.instant('IMAGE_PREVIEW.ALT', { name: this.product().name }),
                title: this.product().name,
            },
        });
    }

    public toggleFavorite(): void {
        const productId = this.product().id;
        if (!productId || this.isFavoriteLoading()) {
            return;
        }

        this.isFavoriteLoading.set(true);

        if (this.isFavorite()) {
            this.removeFavorite(productId);
            return;
        }

        this.favoriteProductService
            .add(productId, this.product().name)
            .pipe(
                takeUntilDestroyed(this.destroyRef),
                finalize(() => {
                    this.isFavoriteLoading.set(false);
                }),
            )
            .subscribe({
                next: favorite => {
                    this.favoriteProductId = favorite.id;
                    this.isFavorite.set(true);
                    this.favoriteChanged.emit({ isFavorite: true, favoriteProductId: favorite.id });
                },
                error: () => {
                    this.showFavoriteError();
                },
            });
    }

    private removeFavorite(productId: string): void {
        if (this.favoriteProductId) {
            this.favoriteProductService
                .remove(this.favoriteProductId)
                .pipe(
                    takeUntilDestroyed(this.destroyRef),
                    finalize(() => {
                        this.isFavoriteLoading.set(false);
                    }),
                )
                .subscribe({
                    next: () => {
                        this.favoriteProductId = null;
                        this.isFavorite.set(false);
                        this.favoriteChanged.emit({ isFavorite: false, favoriteProductId: null });
                    },
                    error: () => {
                        this.showFavoriteError();
                    },
                });
            return;
        }

        this.favoriteProductService
            .getAll()
            .pipe(
                switchMap(favorites => {
                    const match = favorites.find((favorite: FavoriteProduct) => favorite.productId === productId);
                    if (!match) {
                        return of(null);
                    }

                    return this.favoriteProductService.remove(match.id);
                }),
                takeUntilDestroyed(this.destroyRef),
                finalize(() => {
                    this.isFavoriteLoading.set(false);
                }),
            )
            .subscribe({
                next: () => {
                    this.favoriteProductId = null;
                    this.isFavorite.set(false);
                    this.favoriteChanged.emit({ isFavorite: false, favoriteProductId: null });
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
