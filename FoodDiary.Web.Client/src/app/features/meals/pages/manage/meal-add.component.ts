import { ChangeDetectionStrategy, Component } from '@angular/core';

import { MealManageFormComponent } from '../../components/manage/meal-manage-form.component';

@Component({
    selector: 'fd-meal-add',
    templateUrl: './meal-add.component.html',
    styleUrls: ['./meal-add.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [MealManageFormComponent],
})
export class MealAddComponent {}
