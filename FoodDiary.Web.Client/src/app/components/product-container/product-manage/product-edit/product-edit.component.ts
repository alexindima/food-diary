import { ChangeDetectionStrategy, Component } from '@angular/core';
import { BaseProductManageComponent } from '../base-product-manage.component';

@Component({
    selector: 'fd-product-edit',
    templateUrl: './product-edit.component.html',
    styleUrls: ['./product-edit.component.less', '../base-product-manage.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [BaseProductManageComponent],
})
export class ProductEditComponent extends BaseProductManageComponent {}
