import { ChangeDetectionStrategy, Component } from '@angular/core';
import { BaseFoodManageComponent } from '../base-food-manage.component';

@Component({
    selector: 'fd-food-add',
    templateUrl: './food-add.component.html',
    styleUrls: ['./food-add.component.less', '../base-food-manage.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [BaseFoodManageComponent]
})
export class FoodAddComponent extends BaseFoodManageComponent {}
