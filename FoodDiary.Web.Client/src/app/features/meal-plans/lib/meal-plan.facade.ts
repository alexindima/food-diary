import { computed, DestroyRef, inject, Injectable, resource, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, firstValueFrom } from 'rxjs';

import { MealPlanService } from '../api/meal-plan.service';
import type { DietType, MealPlan, MealPlanSummary } from '../models/meal-plan.data';

@Injectable()
export class MealPlanFacade {
    private readonly destroyRef = inject(DestroyRef);
    private readonly service = inject(MealPlanService);
    private readonly selectedPlanId = signal<string | null>(null);

    public readonly dietTypeFilter = signal<DietType | null>(null);
    public readonly pendingAction = signal<'adopt' | 'shopping' | null>(null);
    public readonly actionErrorKey = signal<string | null>(null);
    private readonly plansResource = resource({
        params: () => this.dietTypeFilter(),
        loader: async ({ params }): Promise<MealPlanSummary[]> => {
            const page = await firstValueFrom(this.service.getPage(params ?? undefined));
            return page.data;
        },
    });
    private readonly selectedPlanResource = resource({
        params: () => this.selectedPlanId(),
        loader: async ({ params }): Promise<MealPlan | null> => {
            if (params === null || params.trim().length === 0) {
                return null;
            }

            return firstValueFrom(this.service.getById(params));
        },
    });

    public readonly plans = computed(() => (this.plansResource.hasValue() ? this.plansResource.value() : []));
    public readonly isLoading = computed(() => this.plansResource.isLoading());
    public readonly hasLoadError = computed(() => this.plansResource.error() !== undefined);
    public readonly selectedPlan = computed(() =>
        this.selectedPlanResource.hasValue() ? (this.selectedPlanResource.value() ?? null) : null,
    );
    public readonly isDetailLoading = computed(() => this.selectedPlanResource.isLoading());

    public loadPlans(dietType?: DietType | null): void {
        this.dietTypeFilter.set(dietType ?? null);
    }

    public retryPlans(): void {
        this.plansResource.reload();
    }

    public loadPlan(id: string): void {
        this.selectedPlanId.set(id);
    }

    public adopt(id: string, onSuccess: () => void): void {
        if (this.pendingAction() !== null) {
            return;
        }
        this.pendingAction.set('adopt');
        this.actionErrorKey.set(null);
        this.service
            .adopt(id)
            .pipe(
                takeUntilDestroyed(this.destroyRef),
                finalize(() => {
                    this.pendingAction.set(null);
                }),
            )
            .subscribe({
                next: () => {
                    onSuccess();
                },
                error: () => {
                    this.actionErrorKey.set('MEAL_PLANS.ERROR_ADOPT');
                },
            });
    }

    public generateShoppingList(id: string, onSuccess: () => void): void {
        if (this.pendingAction() !== null) {
            return;
        }
        this.pendingAction.set('shopping');
        this.actionErrorKey.set(null);
        this.service
            .generateShoppingList(id)
            .pipe(
                takeUntilDestroyed(this.destroyRef),
                finalize(() => {
                    this.pendingAction.set(null);
                }),
            )
            .subscribe({
                next: () => {
                    onSuccess();
                },
                error: () => {
                    this.actionErrorKey.set('MEAL_PLANS.ERROR_SHOPPING_LIST');
                },
            });
    }
}
