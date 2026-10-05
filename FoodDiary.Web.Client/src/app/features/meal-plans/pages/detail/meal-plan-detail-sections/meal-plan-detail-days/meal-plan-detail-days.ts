import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';

import { injectCurrentLanguage } from '../../../../../../shared/i18n/inject-current-language';
import { LocalizedNumberPipe } from '../../../../../../shared/i18n/localized-number.pipe';
import type { MealPlanDayViewModel, MealPlanMealViewModel } from '../../../../lib/meal-plan-view.mapper';

@Component({
    selector: 'fd-meal-plan-detail-days',
    imports: [LocalizedNumberPipe, TranslatePipe, FdUiButtonComponent],
    templateUrl: './meal-plan-detail-days.html',
    styleUrl: '../../meal-plan-detail-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealPlanDetailDaysComponent {
    protected readonly language = injectCurrentLanguage();
    public readonly days = input.required<MealPlanDayViewModel[]>();
    public readonly addingMealId = input<string | null>(null);
    public readonly hasMealDraft = input(false);
    public readonly addMeal = output<MealPlanMealViewModel>();
}
