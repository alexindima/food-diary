import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiImagePreviewDialogComponent } from 'fd-ui-kit/image-preview-dialog/fd-ui-image-preview-dialog';

import { AuthService } from '../../../services/auth.service';
import { normalizeQualityScore } from '../../../shared/lib/quality-score.utils';
import type { QualityGrade } from '../../../shared/models/quality-grade.data';
import { EntityCardComponent } from '../entity-card/entity-card';

export type ProductCardItem = {
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
};

@Component({
    selector: 'fd-product-card',
    imports: [TranslatePipe, EntityCardComponent],
    templateUrl: './product-card.html',
    styleUrl: './product-card.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductCardComponent {
    private readonly dialogService = inject(FdUiDialogService);
    private readonly translateService = inject(TranslateService);
    private readonly authService = inject(AuthService);

    public readonly product = input.required<ProductCardItem>();
    public readonly imageUrl = input.required<string | null | undefined>();
    public readonly favoriteLoading = input(false);
    public readonly open = output();
    public readonly addToMeal = output();
    public readonly favoriteToggle = output();
    protected readonly isFavorite = computed(() => Boolean(this.product().isFavorite));
    protected readonly isAuthenticated = this.authService.isAuthenticated;
    protected readonly canToggleFavorite = computed(() => this.isAuthenticated() && this.hasProductId());
    protected readonly favoriteAriaLabelKey = computed(() =>
        this.isFavorite() ? 'PRODUCT_DETAIL.REMOVE_FAVORITE' : 'PRODUCT_DETAIL.ADD_FAVORITE',
    );
    protected readonly ownershipIcon = computed(() => (this.product().isOwnedByCurrentUser ? 'person' : 'groups'));
    protected readonly nutrition = computed(() => ({
        proteins: this.product().proteinsPerBase,
        fats: this.product().fatsPerBase,
        carbs: this.product().carbsPerBase,
        fiber: this.product().fiberPerBase,
        alcohol: this.product().alcoholPerBase,
    }));
    protected readonly quality = computed(() => {
        const score = this.qualityScore();
        const grade = this.product().qualityGrade;
        return score === null || grade === null || grade === undefined ? null : { score, grade };
    });
    protected readonly qualityScore = computed(() => {
        const score = this.product().qualityScore;
        if (score === null || score === undefined) {
            return null;
        }

        return normalizeQualityScore(score);
    });
    protected readonly hasPreviewImage = computed(() => (this.imageUrl()?.trim().length ?? 0) > 0);
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
                alt: this.translateService.instant('IMAGE_PREVIEW.ALT', { name: this.product().name }),
                title: this.product().name,
            },
        });
    }

    protected toggleFavorite(): void {
        const productId = this.product().id;
        if (productId === undefined || productId.length === 0 || this.favoriteLoading()) {
            return;
        }

        this.favoriteToggle.emit();
    }

    private hasProductId(): boolean {
        const id = this.product().id;
        return id !== undefined && id.length > 0;
    }
}
