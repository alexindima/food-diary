import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import type { Product } from '../../../../../shared/models/product.data';
import { ProductManageFormComponent } from '../../../components/manage/product-manage-form/product-manage-form';

@Component({
    selector: 'fd-product-edit',
    templateUrl: './product-edit.html',
    styleUrls: ['./product-edit.scss', '../../../components/manage/product-manage-form/product-manage-form.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [ProductManageFormComponent],
})
export class ProductEditComponent {
    public readonly product = input<Product | null>(null);
}
