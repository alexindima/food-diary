import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader.component';

import { PageBodyComponent } from '../../../../components/shared/page-body/page-body.component';
import { FdPageContainerDirective } from '../../../../directives/layout/page-container.directive';
import { MealPlanFacade } from '../../lib/meal-plan.facade';
import type { MealPlan, MealPlanDay, MealPlanMeal } from '../../models/meal-plan.data';

@Component({
    selector: 'fd-meal-plan-detail-page',
    standalone: true,
    imports: [CommonModule, TranslatePipe, FdUiButtonComponent, FdUiLoaderComponent, PageBodyComponent, FdPageContainerDirective],
    providers: [MealPlanFacade],
    templateUrl: './meal-plan-detail-page.component.html',
    styleUrl: './meal-plan-detail-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealPlanDetailPageComponent {
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    public readonly facade = inject(MealPlanFacade);
    public readonly selectedPlanView = computed<MealPlanDetailViewModel | null>(() => {
        const plan = this.facade.selectedPlan();
        if (plan === null) {
            return null;
        }

        return {
            ...plan,
            dietTypeKey: `MEAL_PLANS.DIET_TYPE.${plan.dietType.toUpperCase()}`,
            days: plan.days.map(day => ({
                ...day,
                meals: day.meals.map(meal => ({
                    ...meal,
                    mealTypeKey: `MEAL_PLANS.MEAL_TYPE.${meal.mealType.toUpperCase()}`,
                })),
            })),
        };
    });

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

interface MealPlanDetailViewModel extends Omit<MealPlan, 'days'> {
    dietTypeKey: string;
    days: MealPlanDayViewModel[];
}

interface MealPlanDayViewModel extends Omit<MealPlanDay, 'meals'> {
    meals: MealPlanMealViewModel[];
}

interface MealPlanMealViewModel extends MealPlanMeal {
    mealTypeKey: string;
}
