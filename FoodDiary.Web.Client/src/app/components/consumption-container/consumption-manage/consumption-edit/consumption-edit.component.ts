import { ChangeDetectionStrategy, Component } from '@angular/core';
import { BaseConsumptionManageComponent } from '../base-consumption-manage.component';

@Component({
    selector: 'fd-consumption-edit',
    templateUrl: './consumption-edit.component.html',
    styleUrls: ['./consumption-edit.component.scss', '../base-consumption-manage.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [BaseConsumptionManageComponent]
})
export class ConsumptionEditComponent extends BaseConsumptionManageComponent {}
