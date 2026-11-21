import { computed, DestroyRef, inject, Injectable, signal } from '@angular/core';
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, type Observable, of, Subject, takeUntil } from 'rxjs';

import { normalizeMealType } from '../../../shared/lib/meal-type.util';
import type { MealPlanId, MealPlanMealId } from '../../../shared/models/semantics/entity-id';
import { QuickMealService } from '../../meals/contracts/quick-meal';
import { RECIPE_LOOKUP } from '../../recipes/contracts/recipe-lookup';
import { MealPlanService } from '../api/meal-plan.service';
import type { MealPlan, MealPlanMeal } from '../models/meal-plan.data';

@Injectable()
export class MealPlanFacade {
    private readonly destroyRef = inject(DestroyRef);
    private readonly service = inject(MealPlanService);
    private readonly recipeLookup = inject(RECIPE_LOOKUP);
    private readonly quickMeal = inject(QuickMealService);
    public readonly hasMealDraft = this.quickMeal.hasItems;
    public readonly addingMealId = signal<MealPlanMealId | null>(null);
    private readonly selectedPlanId = signal<MealPlanId | null>(null);
    private readonly selectionChanged = new Subject<void>();

    public readonly pendingAction = signal<'adopt' | 'shopping' | 'delete' | null>(null);
    public readonly actionErrorKey = signal<string | null>(null);
    private readonly selectedPlanResource = rxResource({
        params: () => this.selectedPlanId(),
        stream: ({ params }): Observable<MealPlan | null> => {
            if (params === null || params.trim().length === 0) {
                return of(null);
            }

            return this.service.getById(params);
        },
    });

    public readonly selectedPlan = computed(() =>
        this.selectedPlanResource.hasValue() ? (this.selectedPlanResource.value() ?? null) : null,
    );
    public readonly isDetailLoading = computed(() => this.selectedPlanResource.isLoading());

    public loadPlan(id: MealPlanId | null): void {
        if (id !== this.selectedPlanId()) {
            this.selectionChanged.next();
            this.actionErrorKey.set(null);
        }
        this.selectedPlanId.set(id);
    }

    public addMealToDiary(meal: MealPlanMeal): void {
        if (this.addingMealId() !== null || this.hasMealDraft()) {
            return;
        }
        this.addingMealId.set(meal.id);
        this.actionErrorKey.set(null);
        this.recipeLookup
            .getById(meal.recipeId)
            .pipe(
                takeUntil(this.selectionChanged),
                takeUntilDestroyed(this.destroyRef),
                finalize(() => {
                    this.addingMealId.set(null);
                }),
            )
            .subscribe({
                next: recipe => {
                    if (recipe === null || this.hasMealDraft()) {
                        this.actionErrorKey.set('MEAL_PLANS.ERROR_ADD_MEAL');
                        return;
                    }
                    this.quickMeal.addRecipe(recipe, meal.servings);
                    this.quickMeal.updateDetails({ mealType: normalizeMealType(meal.mealType) ?? 'OTHER' });
                },
                error: () => {
                    this.actionErrorKey.set('MEAL_PLANS.ERROR_ADD_MEAL');
                },
            });
    }

    public adopt(id: MealPlanId, onSuccess: () => void): void {
        if (this.pendingAction() !== null) {
            return;
        }
        this.pendingAction.set('adopt');
        this.actionErrorKey.set(null);
        this.service
            .adopt(id)
            .pipe(
                takeUntil(this.selectionChanged),
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

    public generateShoppingList(id: MealPlanId, onSuccess: () => void): void {
        if (this.pendingAction() !== null) {
            return;
        }
        this.pendingAction.set('shopping');
        this.actionErrorKey.set(null);
        this.service
            .generateShoppingList(id)
            .pipe(
                takeUntil(this.selectionChanged),
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

    public deletePlan(id: MealPlanId, onSuccess: () => void): void {
        const plan = this.selectedPlan();
        if (plan?.id !== id || plan.isCurated || this.pendingAction() !== null) {
            return;
        }
        this.pendingAction.set('delete');
        this.actionErrorKey.set(null);
        this.service
            .deletePlan(id)
            .pipe(
                takeUntil(this.selectionChanged),
                takeUntilDestroyed(this.destroyRef),
                finalize(() => {
                    this.pendingAction.set(null);
                }),
            )
            .subscribe({
                next: () => {
                    this.selectedPlanId.set(null);
                    onSuccess();
                },
                error: () => {
                    this.actionErrorKey.set('MEAL_PLANS.ERROR_DELETE');
                },
            });
    }
}
