import { ChangeDetectionStrategy, Component } from '@angular/core';

import { BaseMealManageComponent } from '../../components/manage/base-meal-manage.component';

@Component({
    selector: 'fd-meal-edit',
    templateUrl: './meal-edit.component.html',
    styleUrls: ['./meal-edit.component.scss', '../../components/manage/base-meal-manage.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [BaseMealManageComponent],
})
export class MealEditComponent extends BaseMealManageComponent {}
