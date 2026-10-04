import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { injectCurrentLanguage } from '../../../../../../shared/i18n/inject-current-language';
import { LocalizedNumberPipe } from '../../../../../../shared/i18n/localized-number.pipe';
import type { MealPlanDayViewModel } from '../../../../lib/meal-plan-view.mapper';

@Component({
    selector: 'fd-meal-plan-detail-days',
    imports: [LocalizedNumberPipe, TranslatePipe],
    templateUrl: './meal-plan-detail-days.html',
    styleUrl: '../../meal-plan-detail-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealPlanDetailDaysComponent {
    protected readonly language = injectCurrentLanguage();
    public readonly days = input.required<MealPlanDayViewModel[]>();
}
