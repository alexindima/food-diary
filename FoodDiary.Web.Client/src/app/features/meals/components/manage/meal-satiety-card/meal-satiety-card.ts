import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';

import { MealSatietyFieldsComponent } from '../../../../../components/shared/meal-satiety-fields/meal-satiety-fields';

@Component({
    selector: 'fd-meal-satiety-card',
    templateUrl: './meal-satiety-card.html',
    styleUrls: ['../meal-manage-form.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslatePipe, FdUiCardComponent, MealSatietyFieldsComponent],
})
export class MealSatietyCardComponent {
    public readonly preMealSatietyLevel = input.required<number | null>();
    public readonly postMealSatietyLevel = input.required<number | null>();

    public readonly preMealSatietyLevelChange = output<number | null>();
    public readonly postMealSatietyLevelChange = output<number | null>();
}
