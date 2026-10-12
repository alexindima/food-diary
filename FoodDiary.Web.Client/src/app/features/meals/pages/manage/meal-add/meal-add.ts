import { ChangeDetectionStrategy, Component, viewChild } from '@angular/core';

import type { UnsavedEditor } from '../../../../../shared/forms/unsaved-editor';
import { MealManageFormComponent } from '../../../components/manage/meal-manage-form';

@Component({
    selector: 'fd-meal-add',
    templateUrl: './meal-add.html',
    styleUrls: ['./meal-add.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [MealManageFormComponent],
})
export class MealAddComponent implements UnsavedEditor {
    private readonly editor = viewChild(MealManageFormComponent);

    // eslint-disable-next-line local/prefer-protected-template-members -- Public editor departure contract used by the router and parent view queries.
    public async canLeaveAsync(): Promise<boolean> {
        return this.editor()?.canLeaveAsync() ?? true;
    }
}
