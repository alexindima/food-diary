import { ChangeDetectionStrategy, Component } from '@angular/core';
import { BaseProductManageComponent } from '../../components/manage/base-product-manage.component';

@Component({
    selector: 'fd-product-add',
    templateUrl: './product-add.component.html',
    styleUrls: ['./product-add.component.scss', '../../../../components/product-container/product-manage/base-product-manage.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [BaseProductManageComponent],
})
export class ProductAddComponent extends BaseProductManageComponent {}
