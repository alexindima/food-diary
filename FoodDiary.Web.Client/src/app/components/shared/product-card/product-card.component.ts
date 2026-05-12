import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiImagePreviewDialogComponent } from 'fd-ui-kit/image-preview-dialog/fd-ui-image-preview-dialog.component';

import { AuthService } from '../../../services/auth.service';
import type { QualityGrade } from '../../../shared/models/quality-grade.data';
import { EntityCardComponent } from '../entity-card/entity-card.component';

const QUALITY_SCORE_MAX = 100;

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
    private readonly authService = inject(AuthService);

    public readonly product = input.required<ProductCardItem>();
    public readonly imageUrl = input.required<string | null | undefined>();
    public readonly favoriteLoading = input(false);
    public readonly open = output();
    public readonly addToMeal = output();
    public readonly favoriteToggle = output();
    public readonly isFavorite = computed(() => Boolean(this.product().isFavorite));
    public readonly isAuthenticated = this.authService.isAuthenticated;
    public readonly canToggleFavorite = computed(() => this.isAuthenticated() && this.hasProductId());
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

        return Math.round(Math.min(QUALITY_SCORE_MAX, Math.max(0, score)));
    });
    public readonly hasPreviewImage = computed(() => (this.imageUrl()?.trim().length ?? 0) > 0);
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
                alt: this.translateService.instant('IMAGE_PREVIEW.ALT', { name: this.product().name }),
                title: this.product().name,
            },
        });
    }

    public toggleFavorite(): void {
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
