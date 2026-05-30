import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import type { FormArray, FormGroup } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';

import { AiInputActionBarComponent } from '../../../../../components/shared/ai-input-bar/ai-input-action-bar';
import type { AiInputBarResult } from '../../../../../components/shared/ai-input-bar/ai-input-bar.types';
import type { ConsumptionAiSessionManageDto } from '../../../models/meal.data';
import { MealAiSessionsComponent } from '../meal-ai-sessions/meal-ai-sessions';
import { MealItemsListComponent } from '../meal-items-list/meal-items-list';
import type { ConsumptionItemFormData } from '../meal-manage-lib/meal-manage.types';

@Component({
    selector: 'fd-meal-items-section',
    templateUrl: './meal-items-section.html',
    styleUrls: ['../meal-manage-form.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslatePipe, AiInputActionBarComponent, MealItemsListComponent, MealAiSessionsComponent],
})
export class MealItemsSectionComponent {
    public readonly items = input.required<FormArray<FormGroup<ConsumptionItemFormData>>>();
    public readonly aiSessions = input.required<ConsumptionAiSessionManageDto[]>();
    public readonly selectedMealType = input.required<string | null>();
    public readonly isProcessing = input.required<boolean>();
    public readonly renderVersion = input.required<number>();

    public readonly mealRecognized = output<AiInputBarResult>();
    public readonly addItem = output();
    public readonly editItem = output<number>();
    public readonly removeItem = output<number>();
    public readonly openItemSelect = output<number>();
    public readonly editSession = output<number>();
    public readonly deleteSession = output<number>();
}
