import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import type { MealPlanDayViewModel } from '../../../../lib/meal-plan-view.mapper';

@Component({
    selector: 'fd-meal-plan-detail-days',
    imports: [DecimalPipe, TranslatePipe],
    templateUrl: './meal-plan-detail-days.component.html',
    styleUrl: '../../meal-plan-detail-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealPlanDetailDaysComponent {
    public readonly days = input.required<MealPlanDayViewModel[]>();
}
