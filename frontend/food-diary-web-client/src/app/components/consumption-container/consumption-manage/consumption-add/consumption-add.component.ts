import { ChangeDetectionStrategy, Component } from '@angular/core';
import { BaseConsumptionManageComponent } from '../base-consumption-manage.component';

@Component({
    selector: 'app-consumption-add',
    standalone: true,
    templateUrl: './consumption-add.component.html',
    styleUrls: ['./consumption-add.component.less', '../base-consumption-manage.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [BaseConsumptionManageComponent],
})
export class ConsumptionAddComponent extends BaseConsumptionManageComponent {}
