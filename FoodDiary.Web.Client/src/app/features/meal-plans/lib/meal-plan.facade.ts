import { computed, DestroyRef, effect, inject, Injectable, resource, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, firstValueFrom } from 'rxjs';

import { normalizeMealType } from '../../../shared/lib/meal-type.util';
import type { PageOf } from '../../../shared/models/page-of.data';
import { QuickMealService } from '../../meals/contracts/quick-meal';
import { RECIPE_LOOKUP } from '../../recipes/contracts/recipe-lookup';
import { MealPlanService } from '../api/meal-plan.service';
import type { DietType, MealPlan, MealPlanMeal, MealPlanSummary } from '../models/meal-plan.data';

@Injectable()
export class MealPlanFacade {
    private readonly destroyRef = inject(DestroyRef);
    private readonly service = inject(MealPlanService);
    private readonly recipeLookup = inject(RECIPE_LOOKUP);
    private readonly quickMeal = inject(QuickMealService);
    public readonly hasMealDraft = this.quickMeal.hasItems;
    public readonly addingMealId = signal<string | null>(null);
    private readonly selectedPlanId = signal<string | null>(null);

    public readonly dietTypeFilter = signal<DietType | null>(null);
    public readonly pageIndex = signal(0);
    public readonly pageSize = 50;
    private readonly lastLoadedPage = signal<PageOf<MealPlanSummary> | null>(null);
    public readonly pendingAction = signal<'adopt' | 'shopping' | 'delete' | null>(null);
    public readonly actionErrorKey = signal<string | null>(null);
    private readonly plansResource = resource({
        params: () => ({ dietType: this.dietTypeFilter(), page: this.pageIndex() + 1 }),
        loader: async ({ params }) => firstValueFrom(this.service.getPage(params.dietType ?? undefined, params.page, this.pageSize)),
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

    public readonly plans = computed(() => (this.plansResource.hasValue() ? this.plansResource.value().data : []));
    public readonly totalItems = computed(() =>
        this.plansResource.hasValue() ? this.plansResource.value().totalItems : (this.lastLoadedPage()?.totalItems ?? 0),
    );
    public readonly isLoading = computed(() => this.plansResource.isLoading());
    public readonly hasLoadError = computed(() => this.plansResource.error() !== undefined);
    public readonly selectedPlan = computed(() =>
        this.selectedPlanResource.hasValue() ? (this.selectedPlanResource.value() ?? null) : null,
    );
    public readonly isDetailLoading = computed(() => this.selectedPlanResource.isLoading());

    public constructor() {
        effect(() => {
            if (this.plansResource.hasValue()) {
                this.lastLoadedPage.set(this.plansResource.value());
            }
        });
    }

    public loadPlans(filter: DietType | null = null): void {
        if (filter !== this.dietTypeFilter()) {
            this.pageIndex.set(0);
            this.lastLoadedPage.set(null);
        }
        this.dietTypeFilter.set(filter);
    }

    public changePage(index: number): void {
        const lastIndex = Math.max(0, Math.ceil(this.totalItems() / this.pageSize) - 1);
        this.pageIndex.set(Math.max(0, Math.min(index, lastIndex)));
    }

    public retryPlans(): void {
        this.plansResource.reload();
    }

    public loadPlan(id: string): void {
        this.selectedPlanId.set(id);
    }

    public addMealToDiary(meal: MealPlanMeal, onSuccess: () => void): void {
        if (this.addingMealId() !== null || this.hasMealDraft()) {
            return;
        }
        this.addingMealId.set(meal.id);
        this.actionErrorKey.set(null);
        this.recipeLookup
            .getById(meal.recipeId)
            .pipe(
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
                    onSuccess();
                },
                error: () => {
                    this.actionErrorKey.set('MEAL_PLANS.ERROR_ADD_MEAL');
                },
            });
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

    public deletePlan(id: string, onSuccess: () => void): void {
        const plan = this.selectedPlan();
        if (plan?.id !== id || plan.isCurated || this.pendingAction() !== null) {
            return;
        }
        this.pendingAction.set('delete');
        this.actionErrorKey.set(null);
        this.service
            .deletePlan(id)
            .pipe(
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
