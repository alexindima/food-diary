import { ChangeDetectionStrategy, Component } from '@angular/core';
import { BaseProductManageComponent } from '../base-product-manage.component';

@Component({
    selector: 'fd-product-add',
    templateUrl: './product-add.component.html',
    styleUrls: ['./product-add.component.less', '../base-product-manage.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [BaseProductManageComponent],
})
export class ProductAddComponent extends BaseProductManageComponent {}
