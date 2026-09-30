import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiDialogComponent, FdUiLoaderComponent } from 'fd-ui-kit';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { catchError, of } from 'rxjs';

import { injectCurrentLanguage } from '../../../../shared/i18n/inject-current-language';
import { LocalizedNumberPipe } from '../../../../shared/i18n/localized-number.pipe';
import { PublicProductGalleryComponent } from '../../components/public-product-gallery/public-product-gallery';
import { PublicProductDetailsFacade } from '../../lib/public-product-details.facade';

const MAX_PRODUCT_PHOTOS = 5;

@Component({
    selector: 'fd-public-product-preview',
    templateUrl: './public-product-preview.html',
    styleUrl: './public-product-preview.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslatePipe, LocalizedNumberPipe, FdUiDialogComponent, FdUiLoaderComponent, PublicProductGalleryComponent],
})
export class PublicProductPreviewComponent {
    private readonly id = inject<string>(FD_UI_DIALOG_DATA);
    private readonly service = inject(PublicProductDetailsFacade);
    protected readonly ref = inject(FdUiDialogRef);
    protected readonly language = injectCurrentLanguage();
    protected readonly product = toSignal(this.service.getById(this.id).pipe(catchError(() => of(null))));
    protected readonly images = computed(() => {
        const item = this.product();
        return [
            ...new Set(
                [item?.imageUrl, ...(item?.images ?? [])].filter((url): url is string => typeof url === 'string' && url.trim().length > 0),
            ),
        ].slice(0, MAX_PRODUCT_PHOTOS);
    });
    protected readonly nutrients = [
        { key: 'calories', label: 'CALORIES', unit: 'KCAL' },
        { key: 'proteins', label: 'PROTEINS', unit: 'GRAMS' },
        { key: 'fats', label: 'FATS', unit: 'GRAMS' },
        { key: 'carbs', label: 'CARBS', unit: 'GRAMS' },
        { key: 'fiber', label: 'FIBER', unit: 'GRAMS' },
        { key: 'alcohol', label: 'ALCOHOL', unit: 'GRAMS' },
    ] as const;
}
