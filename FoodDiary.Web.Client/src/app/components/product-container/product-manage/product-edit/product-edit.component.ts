import { ChangeDetectionStrategy, Component } from '@angular/core';
import { BaseProductManageComponent } from '../base-product-manage.component';

@Component({
    selector: 'fd-product-edit',
    templateUrl: './product-edit.component.html',
    styleUrls: ['./product-edit.component.scss', '../base-product-manage.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [BaseProductManageComponent],
})
export class ProductEditComponent extends BaseProductManageComponent {}
