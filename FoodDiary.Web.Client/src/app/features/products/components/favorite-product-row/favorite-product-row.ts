import { ChangeDetectionStrategy, Component, computed, ElementRef, input, output, signal, viewChild } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiHintDirective, FdUiIconComponent } from 'fd-ui-kit';

import { NutrientBadgesComponent } from '../../../../components/shared/nutrient-badges/nutrient-badges';
import { injectCurrentLanguage } from '../../../../shared/i18n/inject-current-language';
import { LocalizedNumberPipe } from '../../../../shared/i18n/localized-number.pipe';
import type { FavoriteProduct } from '../../models/product.data';

@Component({
    selector: 'fd-favorite-product-row',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiHintDirective, FdUiIconComponent, NutrientBadgesComponent, LocalizedNumberPipe],
    templateUrl: './favorite-product-row.html',
    styleUrl: './favorite-product-row.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FavoriteProductRowComponent {
    public readonly product = input.required<FavoriteProduct>();
    public readonly remove = output<FavoriteProduct>();
    public readonly removed = input(false);
    public readonly restoring = input(false);
    public readonly restoreFailed = input(false);
    public readonly restore = output<FavoriteProduct>();
    protected readonly undoMessageKey = computed(() =>
        this.restoreFailed() ? 'PRODUCT_FAVORITES.RESTORE_ERROR' : 'PRODUCT_FAVORITES.REMOVED',
    );
    public readonly action = viewChild<FdUiButtonComponent, ElementRef<HTMLElement>>('action', { read: ElementRef });

    public readonly removing = input(false);
    public readonly loading = input(false);
    public readonly disabled = input(false);
    public readonly add = output<FavoriteProduct>();
    protected readonly locale = injectCurrentLanguage();
    protected readonly name = computed(() => {
        const name = this.product().name?.trim();
        return name !== undefined && name.length > 0 ? name : this.product().productName;
    });
    protected readonly composition = computed(() => this.product().brand ?? this.product().barcode ?? '');
    private readonly failedImages = signal<ReadonlySet<string>>(new Set());
    protected readonly images = computed(() => {
        const cover = this.product().imageUrl?.trim();
        const failed = this.failedImages();
        if (cover !== undefined && cover.length > 0 && !failed.has(cover)) {
            return [cover];
        }
        return [];
    });
    protected imageFailed(url: string): void {
        this.failedImages.update(current => new Set([...current, url]));
    }
}
