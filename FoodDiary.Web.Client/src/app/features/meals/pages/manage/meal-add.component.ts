import { ChangeDetectionStrategy, Component } from '@angular/core';

import { BaseMealManageComponent } from '../../components/manage/base-meal-manage.component';

@Component({
    selector: 'fd-meal-add',
    templateUrl: './meal-add.component.html',
    styleUrls: ['./meal-add.component.scss', '../../components/manage/base-meal-manage.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [BaseMealManageComponent],
})
export class MealAddComponent extends BaseMealManageComponent {}
