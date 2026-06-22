import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdTourService } from 'fd-tour';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';

import { PageBodyComponent } from '../../../../components/shared/page-body/page-body';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header';
import { LocalizedTourDefinitionService } from '../../../../shared/tours/localized-tour-definition.service';
import { FdPageContainerDirective } from '../../../../shared/ui/layout/page-container.directive';
import { MealPlanFacade } from '../../lib/meal-plan.facade';
import { buildMealPlanCards } from '../../lib/meal-plan-view.mapper';
import type { DietType } from '../../models/meal-plan.data';
import { MealPlanListContentComponent } from './meal-plan-list-sections/meal-plan-list-content/meal-plan-list-content';
import { MealPlanListFiltersComponent } from './meal-plan-list-sections/meal-plan-list-filters/meal-plan-list-filters';
import { MEAL_PLANS_LIST_TOUR } from './meal-plans-list-tour';

@Component({
    selector: 'fd-meal-plans-list-page',
    imports: [
        TranslatePipe,
        FdUiHintDirective,
        FdUiButtonComponent,
        PageBodyComponent,
        PageHeaderComponent,
        FdPageContainerDirective,
        MealPlanListFiltersComponent,
        MealPlanListContentComponent,
    ],
    providers: [MealPlanFacade],
    templateUrl: './meal-plans-list-page.html',
    styleUrl: './meal-plans-list-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealPlansListPageComponent {
    private readonly router = inject(Router);
    private readonly tourService = inject(FdTourService);
    private readonly localizedTour = inject(LocalizedTourDefinitionService);
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

    protected startMealPlansListTour(force = true): void {
        this.tourService.start(this.localizedTour.build(MEAL_PLANS_LIST_TOUR), { force });
    }
}
