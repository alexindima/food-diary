import { DestroyRef, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MealPlanService } from '../api/meal-plan.service';
import { DietType, MealPlan, MealPlanSummary } from '../models/meal-plan.data';

@Injectable()
export class MealPlanFacade {
    private readonly destroyRef = inject(DestroyRef);
    private readonly service = inject(MealPlanService);

    public readonly plans = signal<MealPlanSummary[]>([]);
    public readonly isLoading = signal(false);
    public readonly selectedPlan = signal<MealPlan | null>(null);
    public readonly isDetailLoading = signal(false);
    public readonly dietTypeFilter = signal<DietType | null>(null);

    public loadPlans(dietType?: DietType | null): void {
        this.isLoading.set(true);
        this.service
            .getAll(dietType ?? undefined)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: plans => {
                    this.plans.set(plans);
                    this.isLoading.set(false);
                },
                error: () => {
                    this.plans.set([]);
                    this.isLoading.set(false);
                },
            });
    }

    public loadPlan(id: string): void {
        this.isDetailLoading.set(true);
        this.service
            .getById(id)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: plan => {
                    this.selectedPlan.set(plan);
                    this.isDetailLoading.set(false);
                },
                error: () => {
                    this.selectedPlan.set(null);
                    this.isDetailLoading.set(false);
                },
            });
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
