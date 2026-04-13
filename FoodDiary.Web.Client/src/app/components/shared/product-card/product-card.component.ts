import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { inject } from '@angular/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiIconModule } from 'fd-ui-kit/material';
import { NutrientBadgesComponent } from '../nutrient-badges/nutrient-badges.component';
import { QualityGrade } from '../../../features/products/models/product.data';
import { MediaCardComponent } from '../media-card/media-card.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiImagePreviewDialogComponent } from 'fd-ui-kit/image-preview-dialog/fd-ui-image-preview-dialog.component';

export interface ProductCardItem {
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
    qualityGrade?: QualityGrade | null;
}

@Component({
    selector: 'fd-product-card',
    standalone: true,
    imports: [CommonModule, TranslatePipe, FdUiButtonComponent, FdUiIconModule, NutrientBadgesComponent, MediaCardComponent],
    templateUrl: './product-card.component.html',
    styleUrl: './product-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductCardComponent {
    private readonly dialogService = inject(FdUiDialogService);
    private readonly translateService = inject(TranslateService);

    public readonly product = input.required<ProductCardItem>();
    public readonly imageUrl = input<string>();
    public readonly open = output<void>();
    public readonly addToMeal = output<void>();

    public handleOpen(): void {
        this.open.emit();
    }

    public handleAdd(event: Event): void {
        event.stopPropagation();
        this.addToMeal.emit();
    }

    public hasPreviewImage(): boolean {
        return Boolean(this.imageUrl()?.trim());
    }

    public handlePreview(event: Event): void {
        event.stopPropagation();

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
                alt: this.translateService.instant('IMAGE_PREVIEW.ALT', { name: this.product().name }),
                title: this.product().name,
            },
        });
    }
}
