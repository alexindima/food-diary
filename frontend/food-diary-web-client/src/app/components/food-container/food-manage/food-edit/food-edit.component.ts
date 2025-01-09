import { ChangeDetectionStrategy, Component } from '@angular/core';
import { BaseFoodManageComponent } from '../base-food-manage.component';

@Component({
    selector: 'fd-food-edit',
    templateUrl: './food-edit.component.html',
    styleUrls: ['./food-edit.component.less', '../base-food-manage.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [BaseFoodManageComponent]
})
export class FoodEditComponent extends BaseFoodManageComponent {}
