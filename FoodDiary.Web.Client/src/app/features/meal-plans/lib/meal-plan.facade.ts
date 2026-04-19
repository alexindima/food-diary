import { DestroyRef, computed, inject, Injectable, resource, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { firstValueFrom } from 'rxjs';
import { MealPlanService } from '../api/meal-plan.service';
import { DietType, MealPlan, MealPlanSummary } from '../models/meal-plan.data';

@Injectable()
export class MealPlanFacade {
    private readonly destroyRef = inject(DestroyRef);
    private readonly service = inject(MealPlanService);
    private readonly selectedPlanId = signal<string | null>(null);

    public readonly dietTypeFilter = signal<DietType | null>(null);
    private readonly plansResource = resource({
        params: () => this.dietTypeFilter(),
        loader: async ({ params }): Promise<MealPlanSummary[]> =>
            firstValueFrom(this.service.getAll(params ?? undefined)),
    });
    private readonly selectedPlanResource = resource({
        params: () => this.selectedPlanId(),
        loader: async ({ params }): Promise<MealPlan | null> => {
            if (!params) {
                return null;
            }

            return firstValueFrom(this.service.getById(params));
        },
    });

    public readonly plans = computed(() => (this.plansResource.hasValue() ? this.plansResource.value() : []));
    public readonly isLoading = computed(() => this.plansResource.isLoading());
    public readonly selectedPlan = computed(() =>
        this.selectedPlanResource.hasValue() ? (this.selectedPlanResource.value() ?? null) : null,
    );
    public readonly isDetailLoading = computed(() => this.selectedPlanResource.isLoading());

    public loadPlans(dietType?: DietType | null): void {
        this.dietTypeFilter.set(dietType ?? null);
    }

    public loadPlan(id: string): void {
        this.selectedPlanId.set(id);
    }

    public adopt(id: string, onSuccess: () => void): void {
        this.service
            .adopt(id)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({ next: () => onSuccess() });
    }

    public generateShoppingList(id: string, onSuccess: () => void): void {
        this.service
            .generateShoppingList(id)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({ next: () => onSuccess() });
    }
}
