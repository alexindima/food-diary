import { ChangeDetectionStrategy, Component } from '@angular/core';
import { BaseConsumptionManageComponent } from '../base-consumption-manage.component';

@Component({
    selector: 'fd-consumption-edit',
    templateUrl: './consumption-edit.component.html',
    styleUrls: ['./consumption-edit.component.less', '../base-consumption-manage.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [BaseConsumptionManageComponent]
})
export class ConsumptionEditComponent extends BaseConsumptionManageComponent {}
