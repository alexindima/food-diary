import { ChangeDetectionStrategy, Component } from '@angular/core';

import { MealManageFormComponent } from '../../../components/manage/meal-manage-form';

@Component({
    selector: 'fd-meal-add',
    templateUrl: './meal-add.html',
    styleUrls: ['./meal-add.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [MealManageFormComponent],
})
export class MealAddComponent {}
