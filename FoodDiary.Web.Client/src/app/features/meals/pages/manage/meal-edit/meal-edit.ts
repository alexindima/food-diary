import { ChangeDetectionStrategy, Component, input, viewChild } from '@angular/core';

import type { UnsavedEditor } from '../../../../../shared/forms/unsaved-editor';
import type { Meal } from '../../../../../shared/models/meal.data';
import { MealManageFormComponent } from '../../../components/manage/meal-manage-form';

@Component({
    selector: 'fd-meal-edit',
    templateUrl: './meal-edit.html',
    styleUrls: ['./meal-edit.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [MealManageFormComponent],
})
export class MealEditComponent implements UnsavedEditor {
    private readonly editor = viewChild(MealManageFormComponent);
    public readonly meal = input<Meal | null>(null);

    // eslint-disable-next-line local/prefer-protected-template-members -- Public editor departure contract used by the router and parent view queries.
    public async canLeaveAsync(): Promise<boolean> {
        return this.editor()?.canLeaveAsync() ?? true;
    }
}
