import { computed, DestroyRef, effect, inject, Injectable, resource, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, firstValueFrom } from 'rxjs';

import { normalizeMealType } from '../../../shared/lib/meal-type.util';
import type { PageOf } from '../../../shared/models/page-of.data';
import type { MealPlanId, MealPlanMealId } from '../../../shared/models/semantics/entity-id';
import { resolvePaginationPage } from '../../../shared/navigation/pagination-query.utils';
import { QuickMealService } from '../../meals/contracts/quick-meal';
import { RECIPE_LOOKUP } from '../../recipes/contracts/recipe-lookup';
import { MealPlanService } from '../api/meal-plan.service';
import type { DietType, MealPlan, MealPlanMeal, MealPlanSummary } from '../models/meal-plan.data';
import { type MealPlanListQuery, mealPlanListQueryKey } from './list/meal-plan-list-query';
import { MEAL_PLAN_LIST_QUERY_STATE } from './list/meal-plan-list-query-state';

@Injectable()
export class MealPlanFacade {
    private readonly destroyRef = inject(DestroyRef);
    private readonly service = inject(MealPlanService);
    private readonly recipeLookup = inject(RECIPE_LOOKUP);
    private readonly quickMeal = inject(QuickMealService);
    private readonly queryState = inject(MEAL_PLAN_LIST_QUERY_STATE, { optional: true });
    private readonly initialQuery: MealPlanListQuery = this.queryState?.initial ?? { page: 1, dietType: null };
    public readonly hasMealDraft = this.quickMeal.hasItems;
    public readonly addingMealId = signal<MealPlanMealId | null>(null);
    private readonly selectedPlanId = signal<MealPlanId | null>(null);

    public readonly dietTypeFilter = signal(this.initialQuery.dietType);
    public readonly pageIndex = signal(this.initialQuery.page - 1);
    public readonly pageSize = 50;
    private readonly lastLoadedPage = signal<{ dietType: DietType | null; page: PageOf<MealPlanSummary> } | null>(null);
    public readonly pendingAction = signal<'adopt' | 'shopping' | 'delete' | null>(null);
    public readonly actionErrorKey = signal<string | null>(null);
    private readonly plansResource = resource({
        params: () => this.currentListQuery(),
        loader: async ({ params }): Promise<{ key: string; page: PageOf<MealPlanSummary> }> => this.loadListPageAsync(params),
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

    public readonly plans = computed(() => this.currentLoadedPage()?.data ?? []);
    public readonly totalItems = computed(() => this.currentLoadedPage()?.totalItems ?? this.cachedListPage()?.totalItems ?? 0);
    public readonly isLoading = computed(() => this.plansResource.isLoading());
    public readonly hasLoadError = computed(() => this.plansResource.error() !== undefined);
    public readonly selectedPlan = computed(() =>
        this.selectedPlanResource.hasValue() ? (this.selectedPlanResource.value() ?? null) : null,
    );
    public readonly isDetailLoading = computed(() => this.selectedPlanResource.isLoading());

    public constructor() {
        this.connectListRoute();
        effect(() => {
            const page = this.currentLoadedPage();
            if (page !== null) {
                this.lastLoadedPage.set({ dietType: this.currentListQuery().dietType, page });
            }
        });
    }

    public loadPlans(filter?: DietType | null): void {
        if (filter === undefined) {
            return;
        }
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

    private connectListRoute(): void {
        if (this.queryState === null) {
            return;
        }
        this.queryState.changes.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(query => {
            this.dietTypeFilter.set(query.dietType);
            this.pageIndex.set(query.page - 1);
        });
        effect(() => {
            this.writeListQuery({ page: this.pageIndex() + 1, dietType: this.dietTypeFilter() });
        });
        void this.queryState.normalizePageAsync().catch(() => {
            /* Retain the current catalogue if navigation fails. */
        });
    }

    private currentListQuery(): MealPlanListQuery {
        return this.queryState?.current() ?? { page: this.pageIndex() + 1, dietType: this.dietTypeFilter() };
    }

    private currentLoadedPage(): PageOf<MealPlanSummary> | null {
        if (!this.plansResource.hasValue()) {
            return null;
        }
        const loaded = this.plansResource.value();
        return loaded.key === mealPlanListQueryKey(this.currentListQuery()) ? loaded.page : null;
    }

    private cachedListPage(): PageOf<MealPlanSummary> | null {
        const loaded = this.lastLoadedPage();
        return loaded?.dietType === this.currentListQuery().dietType ? loaded.page : null;
    }

    private async loadListPageAsync(query: MealPlanListQuery): Promise<{ key: string; page: PageOf<MealPlanSummary> }> {
        const key = mealPlanListQueryKey(query);
        const page = await firstValueFrom(this.service.getPage(query.dietType ?? undefined, query.page, this.pageSize));
        if (key === mealPlanListQueryKey(this.currentListQuery())) {
            const resolved = resolvePaginationPage(query.page, page.totalPages);
            if (resolved !== query.page) {
                this.pageIndex.set(resolved - 1);
                this.writeListQuery({ ...query, page: resolved }, true);
            }
        }
        return { key, page };
    }

    private writeListQuery(query: MealPlanListQuery, replaceUrl = false): void {
        if (this.queryState !== null) {
            void this.queryState.writeAsync(query, { replaceUrl }).catch(() => {
                /* Keep successful catalogue data on navigation failure. */
            });
        }
    }

    public loadPlan(id: MealPlanId): void {
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

    public adopt(id: MealPlanId, onSuccess: () => void): void {
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

    public generateShoppingList(id: MealPlanId, onSuccess: () => void): void {
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
