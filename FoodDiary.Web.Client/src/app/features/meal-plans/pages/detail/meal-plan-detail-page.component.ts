import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiIconModule } from 'fd-ui-kit/material';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader.component';
import { PageBodyComponent } from '../../../../components/shared/page-body/page-body.component';
import { FdPageContainerDirective } from '../../../../directives/layout/page-container.directive';
import { MealPlanFacade } from '../../lib/meal-plan.facade';

@Component({
    selector: 'fd-meal-plan-detail-page',
    standalone: true,
    imports: [
        CommonModule,
        TranslatePipe,
        FdUiButtonComponent,
        FdUiIconModule,
        FdUiLoaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
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

    public constructor() {
        const id = this.route.snapshot.paramMap.get('id');
        if (id) {
            this.facade.loadPlan(id);
        }
    }

    public adopt(): void {
        const plan = this.facade.selectedPlan();
        if (!plan) {
            return;
        }
        this.facade.adopt(plan.id, () => void this.router.navigate(['/meal-plans']));
    }

    public generateShoppingList(): void {
        const plan = this.facade.selectedPlan();
        if (!plan) {
            return;
        }
        this.facade.generateShoppingList(plan.id, () => void this.router.navigate(['/shopping-lists']));
    }

    public goBack(): void {
        void this.router.navigate(['/meal-plans']);
    }
}
