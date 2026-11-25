import { UpperCasePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit';
import { tap } from 'rxjs';

import { AdminLoadErrorComponent } from '../../../shared/feedback/admin-load-error';
import { AdminMealPlansFacade } from '../lib/admin-meal-plans.facade';
import type { CatalogPlan, CatalogPlanSummary } from '../models/admin-meal-plan.data';
import { AdminMealPlanEditorComponent } from './admin-meal-plan-editor';

@Component({
    selector: 'fd-admin-meal-plans',
    providers: [AdminMealPlansFacade],
    imports: [UpperCasePipe, TranslatePipe, AdminLoadErrorComponent, FdUiButtonComponent, AdminMealPlanEditorComponent],
    templateUrl: './admin-meal-plans.html',
    styleUrl: './admin-meal-plans.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminMealPlansComponent {
    private readonly api = inject(AdminMealPlansFacade);
    private readonly translate = inject(TranslateService);
    private readonly listVersion = signal(0);
    private readonly plansResource = rxResource({ params: this.listVersion, stream: () => this.api.getAll() });
    private readonly detailRequest = signal<{ id: CatalogPlanSummary['id'] } | undefined>(undefined);
    private readonly detailResource = rxResource({
        params: this.detailRequest,
        stream: ({ params }) =>
            this.api.get(params.id).pipe(
                tap(detail => {
                    if (this.detailRequest() === params) {
                        this.selected.set(detail);
                        this.editing.set(true);
                        this.saved.set(false);
                    }
                }),
            ),
    });
    protected readonly plans = computed(() => (this.plansResource.hasValue() ? this.plansResource.value() : []));
    protected readonly loading = computed(() => this.plansResource.isLoading() || this.detailResource.isLoading());
    protected readonly loadFailed = computed(() => this.plansResource.error() !== undefined);
    protected readonly editing = signal(false);
    protected readonly error = computed(() =>
        this.detailResource.error() === undefined ? null : String(this.translate.instant('ADMIN_MEAL_PLANS.LOAD_ERROR')),
    );
    protected readonly saved = signal(false);
    protected readonly selected = signal<CatalogPlan | null>(null);

    protected load(): void {
        this.listVersion.update(version => version + 1);
    }

    protected create(): void {
        this.detailRequest.set(undefined);
        this.selected.set(null);
        this.saved.set(false);
        this.editing.set(true);
    }

    protected edit(plan: CatalogPlanSummary): void {
        this.detailRequest.set({ id: plan.id });
    }

    protected cancel(): void {
        this.detailRequest.set(undefined);
        this.selected.set(null);
        this.editing.set(false);
    }

    protected onSaved(): void {
        this.detailRequest.set(undefined);
        this.selected.set(null);
        this.editing.set(false);
        this.saved.set(true);
        this.load();
    }
}
