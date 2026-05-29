import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';

import { PageBodyComponent } from '../../../../components/shared/page-body/page-body.component';
import { FdPageContainerDirective } from '../../../../directives/layout/page-container.directive';
import { MealPlanFacade } from '../../lib/meal-plan.facade';
import { buildMealPlanCards } from '../../lib/meal-plan-view.mapper';
import type { DietType } from '../../models/meal-plan.data';
import { MealPlanListContentComponent } from './meal-plan-list-sections/meal-plan-list-content/meal-plan-list-content.component';
import { MealPlanListFiltersComponent } from './meal-plan-list-sections/meal-plan-list-filters/meal-plan-list-filters.component';

@Component({
    selector: 'fd-meal-plans-list-page',
    imports: [TranslatePipe, PageBodyComponent, FdPageContainerDirective, MealPlanListFiltersComponent, MealPlanListContentComponent],
    providers: [MealPlanFacade],
    templateUrl: './meal-plans-list-page.component.html',
    styleUrl: './meal-plans-list-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealPlansListPageComponent {
    private readonly router = inject(Router);
    protected readonly facade = inject(MealPlanFacade);
    protected readonly planCards = computed(() => buildMealPlanCards(this.facade.plans()));

    public constructor() {
        this.facade.loadPlans();
    }

    protected filterByDiet(type: DietType | null): void {
        this.facade.loadPlans(type);
    }

    protected openPlan(id: string): void {
        void this.router.navigate(['/meal-plans', id]);
    }
}
