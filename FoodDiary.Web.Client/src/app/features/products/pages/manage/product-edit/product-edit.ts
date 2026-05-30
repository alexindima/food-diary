import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { ProductManageFormComponent } from '../../../components/manage/product-manage-form/product-manage-form';
import type { Product } from '../../../models/product.data';

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
