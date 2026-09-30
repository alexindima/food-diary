import { UpperCasePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit';
import { finalize } from 'rxjs';

import { AdminLoadErrorComponent } from '../../../shared/feedback/admin-load-error';
import { AdminMealPlansFacade } from '../lib/admin-meal-plans.facade';
import type { CatalogPlan } from '../models/admin-meal-plan.data';
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
    private readonly destroyRef = inject(DestroyRef);
    private readonly translate = inject(TranslateService);
    protected readonly plans = signal<CatalogPlan[]>([]);
    protected readonly loading = signal(false);
    protected readonly loadFailed = signal(false);
    protected readonly editing = signal(false);
    protected readonly error = signal<string | null>(null);
    protected readonly saved = signal(false);
    protected readonly selected = signal<CatalogPlan | null>(null);

    public constructor() {
        this.load();
    }

    protected load(): void {
        this.loading.set(true);
        this.loadFailed.set(false);
        this.api
            .getAll()
            .pipe(
                takeUntilDestroyed(this.destroyRef),
                finalize(() => {
                    this.loading.set(false);
                }),
            )
            .subscribe({
                next: plans => {
                    this.plans.set(plans);
                },
                error: () => {
                    this.loadFailed.set(true);
                },
            });
    }

    protected create(): void {
        this.selected.set(null);
        this.saved.set(false);
        this.error.set(null);
        this.editing.set(true);
    }

    protected edit(plan: CatalogPlan): void {
        this.loading.set(true);
        this.error.set(null);
        this.api
            .get(plan.id)
            .pipe(
                takeUntilDestroyed(this.destroyRef),
                finalize(() => {
                    this.loading.set(false);
                }),
            )
            .subscribe({
                next: detail => {
                    this.selected.set(detail);
                    this.editing.set(true);
                    this.saved.set(false);
                },
                error: () => {
                    this.error.set(String(this.translate.instant('ADMIN_MEAL_PLANS.LOAD_ERROR')));
                },
            });
    }

    protected cancel(): void {
        this.editing.set(false);
    }

    protected onSaved(): void {
        this.editing.set(false);
        this.saved.set(true);
        this.load();
    }
}
