import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon.component';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader.component';

import { PageBodyComponent } from '../../../../components/shared/page-body/page-body.component';
import { FdCardHoverDirective } from '../../../../directives/card-hover.directive';
import { FdPageContainerDirective } from '../../../../directives/layout/page-container.directive';
import { MealPlanFacade } from '../../lib/meal-plan.facade';
import { DIET_TYPES, type DietType, type MealPlanSummary } from '../../models/meal-plan.data';

@Component({
    selector: 'fd-meal-plans-list-page',
    standalone: true,
    imports: [
        CommonModule,
        TranslatePipe,
        FdUiButtonComponent,
        FdUiIconComponent,
        FdUiLoaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        FdCardHoverDirective,
    ],
    providers: [MealPlanFacade],
    templateUrl: './meal-plans-list-page.component.html',
    styleUrl: './meal-plans-list-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealPlansListPageComponent {
    private readonly router = inject(Router);
    public readonly facade = inject(MealPlanFacade);
    public readonly dietTypes = DIET_TYPES;
    public readonly planCards = computed<MealPlanCardViewModel[]>(() =>
        this.facade.plans().map(plan => ({
            ...plan,
            dietTypeKey: `MEAL_PLANS.DIET_TYPE.${plan.dietType.toUpperCase()}`,
        })),
    );

    public constructor() {
        this.facade.loadPlans();
    }

    public filterByDiet(type: DietType | null): void {
        this.facade.loadPlans(type);
    }

    public openPlan(id: string): void {
        void this.router.navigate(['/meal-plans', id]);
    }
}

interface MealPlanCardViewModel extends MealPlanSummary {
    dietTypeKey: string;
}
