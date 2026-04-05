import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { Router } from '@angular/router';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiIconModule } from 'fd-ui-kit/material';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader.component';
import { PageBodyComponent } from '../../../../components/shared/page-body/page-body.component';
import { FdPageContainerDirective } from '../../../../directives/layout/page-container.directive';
import { FdCardHoverDirective } from '../../../../directives/card-hover.directive';
import { MealPlanFacade } from '../../lib/meal-plan.facade';
import { DIET_TYPES, DietType } from '../../models/meal-plan.data';

@Component({
    selector: 'fd-meal-plans-list-page',
    standalone: true,
    imports: [
        CommonModule,
        TranslatePipe,
        FdUiButtonComponent,
        FdUiIconModule,
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
export class MealPlansListPageComponent implements OnInit {
    private readonly router = inject(Router);
    public readonly facade = inject(MealPlanFacade);
    public readonly dietTypes = DIET_TYPES;

    public ngOnInit(): void {
        this.facade.loadPlans();
    }

    public filterByDiet(type: DietType | null): void {
        this.facade.dietTypeFilter.set(type);
        this.facade.loadPlans(type);
    }

    public openPlan(id: string): void {
        void this.router.navigate(['/meal-plans', id]);
    }
}
