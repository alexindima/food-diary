import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';

import type { MealPlanDetailHeaderViewModel } from '../../../../lib/meal-plan-view.mapper';

@Component({
    selector: 'fd-meal-plan-detail-header',
    imports: [TranslatePipe, FdUiButtonComponent],
    templateUrl: './meal-plan-detail-header.component.html',
    styleUrl: '../../meal-plan-detail-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealPlanDetailHeaderComponent {
    public readonly plan = input.required<MealPlanDetailHeaderViewModel>();
    public readonly adoptPlan = output();
    public readonly generateShoppingList = output();
}
