import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader.component';

import { PageBodyComponent } from '../../../../components/shared/page-body/page-body.component';
import { FdPageContainerDirective } from '../../../../directives/layout/page-container.directive';
import { MealPlanFacade } from '../../lib/meal-plan.facade';
import { buildMealPlanDetailView } from '../../lib/meal-plan-view.mapper';
import { MealPlanDetailDaysComponent } from './meal-plan-detail-sections/meal-plan-detail-days/meal-plan-detail-days.component';
import { MealPlanDetailHeaderComponent } from './meal-plan-detail-sections/meal-plan-detail-header/meal-plan-detail-header.component';

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
    templateUrl: './meal-plan-detail-page.component.html',
    styleUrl: './meal-plan-detail-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealPlanDetailPageComponent {
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    public readonly facade = inject(MealPlanFacade);
    public readonly selectedPlanView = computed(() => buildMealPlanDetailView(this.facade.selectedPlan()));

    public constructor() {
        const id = this.route.snapshot.paramMap.get('id');
        if (id !== null && id.length > 0) {
            this.facade.loadPlan(id);
        }
    }

    public adopt(): void {
        const plan = this.facade.selectedPlan();
        if (plan === null) {
            return;
        }
        this.facade.adopt(plan.id, () => void this.router.navigate(['/meal-plans']));
    }

    public generateShoppingList(): void {
        const plan = this.facade.selectedPlan();
        if (plan === null) {
            return;
        }
        this.facade.generateShoppingList(plan.id, () => void this.router.navigate(['/shopping-lists']));
    }

    public goBack(): void {
        void this.router.navigate(['/meal-plans']);
    }
}
