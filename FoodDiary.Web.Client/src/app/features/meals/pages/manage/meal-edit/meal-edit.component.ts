import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { MealManageFormComponent } from '../../../components/manage/meal-manage-form.component';
import type { Meal } from '../../../models/meal.data';

@Component({
    selector: 'fd-meal-edit',
    templateUrl: './meal-edit.component.html',
    styleUrls: ['./meal-edit.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [MealManageFormComponent],
})
export class MealEditComponent {
    public readonly consumption = input<Meal | null>(null);
}
