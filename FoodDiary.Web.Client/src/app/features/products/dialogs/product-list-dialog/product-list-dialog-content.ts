import { ChangeDetectionStrategy, Component, input, output, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader';

import type { Product } from '../../../../shared/models/product.data';
import type { ProductSelectItemViewModel } from './product-list-dialog.types';

@Component({
    selector: 'fd-product-list-dialog-content',
    imports: [TranslatePipe, FdUiHintDirective, FdUiButtonComponent, FdUiIconComponent, FdUiLoaderComponent],
    templateUrl: './product-list-dialog-content.html',
    styleUrl: './product-list-dialog.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductListDialogContentComponent {
    public readonly isLoading = input.required<boolean>();
    public readonly items = input.required<readonly ProductSelectItemViewModel[]>();

    public readonly productSelected = output<Product>();
    protected readonly failedImages = signal<ReadonlySet<string>>(new Set());

    protected imageFailed(url: string | undefined): void {
        if (url !== undefined) {
            this.failedImages.update(images => new Set([...images, url]));
        }
    }
}
