import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdTourService } from 'fd-tour';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import type { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader';
import { finalize } from 'rxjs';

import {
    ConfirmDeleteDialogComponent,
    type ConfirmDeleteDialogData,
} from '../../../../components/shared/confirm-delete-dialog/confirm-delete-dialog';
import { PageBodyComponent } from '../../../../components/shared/page-body/page-body';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header';
import { entityId } from '../../../../shared/models/semantics/entity-id';
import { LocalizedTourDefinitionService } from '../../../../shared/tours/localized-tour-definition.service';
import { FdPageContainerDirective } from '../../../../shared/ui/layout/page-container.directive';
import { MealPlanFacade } from '../../lib/meal-plan.facade';
import { buildMealPlanDetailView } from '../../lib/meal-plan-view.mapper';
import type { MealPlanMeal } from '../../models/meal-plan.data';
import { MealPlanDetailDaysComponent } from './meal-plan-detail-sections/meal-plan-detail-days/meal-plan-detail-days';
import { MealPlanDetailHeaderComponent } from './meal-plan-detail-sections/meal-plan-detail-header/meal-plan-detail-header';
import { MEAL_PLAN_DETAIL_TOUR } from './meal-plan-detail-tour';

@Component({
    selector: 'fd-meal-plan-detail-page',
    imports: [
        TranslatePipe,
        FdUiHintDirective,
        FdUiButtonComponent,
        FdUiLoaderComponent,
        PageBodyComponent,
        PageHeaderComponent,
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
    private readonly tourService = inject(FdTourService);
    private readonly localizedTour = inject(LocalizedTourDefinitionService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly dialogs = inject(FdUiDialogService);
    private readonly translate = inject(TranslateService);
    private deleteDialog: FdUiDialogRef<ConfirmDeleteDialogComponent, boolean> | null = null;
    protected readonly facade = inject(MealPlanFacade);
    protected readonly selectedPlanView = computed(() => buildMealPlanDetailView(this.facade.selectedPlan()));

    public constructor() {
        this.destroyRef.onDestroy(() => this.deleteDialog?.close());
        const id = this.route.snapshot.paramMap.get('id');
        if (id !== null && id.length > 0) {
            this.facade.loadPlan(entityId<'meal-plan'>(id));
        }
    }

    protected confirmDeletePlan(): void {
        const plan = this.facade.selectedPlan();
        if (plan === null || plan.isCurated || this.facade.pendingAction() !== null || this.deleteDialog !== null) {
            return;
        }
        const dialog = this.dialogs.open<ConfirmDeleteDialogComponent, ConfirmDeleteDialogData, boolean>(ConfirmDeleteDialogComponent, {
            preset: 'confirm',
            data: {
                title: this.translate.instant('MEAL_PLANS.DELETE_CONFIRM_TITLE'),
                message: this.translate.instant('MEAL_PLANS.DELETE_CONFIRM_MESSAGE', { name: plan.name }),
                confirmLabel: this.translate.instant('MEAL_PLANS.DELETE_PLAN'),
            },
        });
        this.deleteDialog = dialog;
        dialog
            .afterClosed()
            .pipe(
                takeUntilDestroyed(this.destroyRef),
                finalize(() => {
                    this.deleteDialog = null;
                }),
            )
            .subscribe(confirmed => {
                if (confirmed === true && this.facade.selectedPlan()?.id === plan.id) {
                    this.facade.deletePlan(plan.id, () => void this.router.navigate(['/meal-plans']));
                }
            });
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

    protected addMealToDiary(meal: MealPlanMeal): void {
        this.facade.addMealToDiary(meal, () => void this.router.navigate(['/dashboard']));
    }

    protected goBack(): void {
        void this.router.navigate(['/meal-plans']);
    }

    protected startMealPlanDetailTour(force = true): void {
        this.tourService.start(this.localizedTour.build(MEAL_PLAN_DETAIL_TOUR), { force });
    }
}
