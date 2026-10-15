import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiSegmentedToggleComponent } from 'fd-ui-kit/segmented-toggle/fd-ui-segmented-toggle';

import type { MealPlanSource } from '../../../../lib/list/meal-plan-list-query';
import { buildMealPlanDietFilterOptions } from '../../../../lib/meal-plan-view.mapper';
import type { DietType } from '../../../../models/meal-plan.data';

@Component({
    selector: 'fd-meal-plan-list-filters',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiSegmentedToggleComponent],
    templateUrl: './meal-plan-list-filters.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealPlanListFiltersComponent {
    public readonly selectedType = input.required<DietType | null>();
    public readonly selectedSource = input<MealPlanSource>('all');
    public readonly sourceChange = output<MealPlanSource>();
    protected readonly sourceOptions = [
        { value: 'all', labelKey: 'MEAL_PLANS.SOURCE.ALL' },
        { value: 'curated', labelKey: 'MEAL_PLANS.SOURCE.CURATED' },
        { value: 'saved', labelKey: 'MEAL_PLANS.SOURCE.SAVED' },
    ];
    protected readonly options = computed(() => buildMealPlanDietFilterOptions(this.selectedType()));
    public readonly filterChange = output<DietType | null>();

    protected selectSource(value: string): void {
        if (value === 'all' || value === 'curated' || value === 'saved') {
            this.sourceChange.emit(value);
        }
    }
}
