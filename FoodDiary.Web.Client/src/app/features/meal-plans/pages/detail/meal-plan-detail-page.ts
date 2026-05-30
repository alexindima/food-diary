import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader';

import { PageBodyComponent } from '../../../../components/shared/page-body/page-body';
import { FdPageContainerDirective } from '../../../../shared/ui/layout/page-container.directive';
import { MealPlanFacade } from '../../lib/meal-plan.facade';
import { buildMealPlanDetailView } from '../../lib/meal-plan-view.mapper';
import { MealPlanDetailDaysComponent } from './meal-plan-detail-sections/meal-plan-detail-days/meal-plan-detail-days';
import { MealPlanDetailHeaderComponent } from './meal-plan-detail-sections/meal-plan-detail-header/meal-plan-detail-header';

@Component({
    selector: 'fd-meal-plan-detail-page',
    imports: [
        TranslatePipe,
        FdUiButtonComponent,
        FdUiLoaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        MealPlanDetailHeaderComponent,
        MealPlanDetailDaysComponent,
    ],
    providers: [MealPlanFacade],
    templateUrl: './meal-plan-detail-page.html',
    styleUrl: './meal-plan-detail-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealPlanDetailPageComponent {
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    protected readonly facade = inject(MealPlanFacade);
    protected readonly selectedPlanView = computed(() => buildMealPlanDetailView(this.facade.selectedPlan()));

    public constructor() {
        const id = this.route.snapshot.paramMap.get('id');
        if (id !== null && id.length > 0) {
            this.facade.loadPlan(id);
        }
    }

    protected adopt(): void {
        const plan = this.facade.selectedPlan();
        if (plan === null) {
            return;
        }
        this.facade.adopt(plan.id, () => void this.router.navigate(['/meal-plans']));
    }

    protected generateShoppingList(): void {
        const plan = this.facade.selectedPlan();
        if (plan === null) {
            return;
        }
        this.facade.generateShoppingList(plan.id, () => void this.router.navigate(['/shopping-lists']));
    }

    protected goBack(): void {
        void this.router.navigate(['/meal-plans']);
    }
}
