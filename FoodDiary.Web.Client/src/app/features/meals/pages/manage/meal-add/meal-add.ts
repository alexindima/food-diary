import { ChangeDetectionStrategy, Component, computed, inject, input, viewChild } from '@angular/core';
import { Router } from '@angular/router';

import { canLeaveEditor, type UnsavedEditor } from '../../../../../shared/forms/unsaved-editor';
import { normalizeMealType } from '../../../../../shared/lib/meal-type.util';
import { getStringProperty } from '../../../../../shared/lib/unknown-value.utils';
import { MealManageFormComponent } from '../../../components/manage/meal-manage-form';

function firstQueryValue(value: string | string[] | undefined): string | undefined {
    return Array.isArray(value) ? value[0] : value;
}

@Component({
    selector: 'fd-meal-add',
    templateUrl: './meal-add.html',
    styleUrls: ['./meal-add.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [MealManageFormComponent],
})
export class MealAddComponent implements UnsavedEditor {
    private readonly editor = viewChild(MealManageFormComponent);
    private readonly navigationMealType = getStringProperty(inject(Router).currentNavigation()?.extras.state, 'mealType');

    public readonly manual = input(false, {
        transform: (value: string | string[] | undefined) => firstQueryValue(value) === '1',
    });
    public readonly mealType = input(undefined, { transform: firstQueryValue });
    protected readonly initialMealType = computed(() => normalizeMealType(this.navigationMealType ?? this.mealType()));

    // eslint-disable-next-line local/prefer-protected-template-members -- Public editor departure contract used by the router and parent view queries.
    public async canLeaveAsync(): Promise<boolean> {
        return canLeaveEditor(this.editor());
    }
}
