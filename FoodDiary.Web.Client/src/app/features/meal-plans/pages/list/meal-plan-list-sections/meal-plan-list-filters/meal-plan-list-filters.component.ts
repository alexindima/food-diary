import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';

import { buildMealPlanDietFilterOptions } from '../../../../lib/meal-plan-view.mapper';
import type { DietType } from '../../../../models/meal-plan.data';

@Component({
    selector: 'fd-meal-plan-list-filters',
    imports: [TranslatePipe, FdUiButtonComponent],
    templateUrl: './meal-plan-list-filters.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealPlanListFiltersComponent {
    public readonly selectedType = input.required<DietType | null>();
    public readonly options = computed(() => buildMealPlanDietFilterOptions(this.selectedType()));
    public readonly filterChange = output<DietType | null>();
}
