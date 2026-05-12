import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon.component';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader.component';

import type { Product } from '../models/product.data';
import type { ProductSelectItemViewModel } from './product-list-dialog.types';

@Component({
    selector: 'fd-product-list-dialog-content',
    imports: [TranslatePipe, FdUiHintDirective, FdUiButtonComponent, FdUiIconComponent, FdUiLoaderComponent],
    templateUrl: './product-list-dialog-content.component.html',
    styleUrl: './product-list-dialog.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductListDialogContentComponent {
    public readonly isLoading = input.required<boolean>();
    public readonly items = input.required<readonly ProductSelectItemViewModel[]>();

    public readonly productSelected = output<Product>();
}
