import { computed, DestroyRef, effect, inject, Injectable, signal } from '@angular/core';
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { map, type Observable } from 'rxjs';

import type { PageOf } from '../../../../shared/models/page-of.data';
import { resolvePaginationPage } from '../../../../shared/navigation/pagination-query.utils';
import { MealPlanService } from '../../api/meal-plan.service';
import type { DietType, MealPlanSummary } from '../../models/meal-plan.data';
import type { MealPlanListQuery, MealPlanSource } from './meal-plan-list-query';
import { MEAL_PLAN_LIST_QUERY_STATE } from './meal-plan-list-query-state';

@Injectable()
export class MealPlanListFacade {
    private readonly destroyRef = inject(DestroyRef);
    private readonly service = inject(MealPlanService);
    private readonly queryState = inject(MEAL_PLAN_LIST_QUERY_STATE);

    public readonly dietTypeFilter = signal(this.queryState.initial.dietType);
    public readonly sourceFilter = signal(this.queryState.initial.source);
    public readonly pageIndex = signal(this.queryState.initial.page - 1);
    public readonly pageSize = 50;
    private readonly lastLoadedPage = signal<{ dietType: DietType | null; source: MealPlanSource; page: PageOf<MealPlanSummary> } | null>(
        null,
    );
    private readonly plansResource = rxResource({
        params: () => this.queryState.current(),
        stream: ({ params }) => this.loadListPage(params),
    });

    public readonly plans = computed(() => this.currentLoadedPage()?.data ?? []);
    public readonly totalItems = computed(() => this.currentLoadedPage()?.totalItems ?? this.cachedListPage()?.totalItems ?? 0);
    public readonly isLoading = computed(() => this.plansResource.isLoading());
    public readonly hasLoadError = computed(() => this.plansResource.error() !== undefined);

    public constructor() {
        this.connectListRoute();
        effect(() => {
            const page = this.currentLoadedPage();
            if (page !== null) {
                this.lastLoadedPage.set({ dietType: this.queryState.current().dietType, source: this.queryState.current().source, page });
            }
        });
    }

    public loadPlans(filter: DietType | null): void {
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

    public filterBySource(source: MealPlanSource): void {
        if (source !== this.sourceFilter()) {
            this.pageIndex.set(0);
            this.lastLoadedPage.set(null);
            this.sourceFilter.set(source);
        }
    }

    public retryPlans(): void {
        this.plansResource.reload();
    }

    private connectListRoute(): void {
        this.queryState.changes.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(query => {
            this.dietTypeFilter.set(query.dietType);
            this.sourceFilter.set(query.source);
            this.pageIndex.set(query.page - 1);
        });
        effect(() => {
            this.writeListQuery({ page: this.pageIndex() + 1, dietType: this.dietTypeFilter(), source: this.sourceFilter() });
        });
        void this.queryState.normalizePageAsync().catch(() => {
            /* Retain the current catalogue if navigation fails. */
        });
    }

    private currentLoadedPage(): PageOf<MealPlanSummary> | null {
        return this.plansResource.hasValue() ? this.plansResource.value() : null;
    }

    private cachedListPage(): PageOf<MealPlanSummary> | null {
        const loaded = this.lastLoadedPage();
        const query = this.queryState.current();
        return loaded?.dietType === query.dietType && loaded.source === query.source ? loaded.page : null;
    }

    private loadListPage(query: MealPlanListQuery): Observable<PageOf<MealPlanSummary> | null> {
        const isCurated = query.source === 'all' ? undefined : query.source === 'curated';
        return this.service.getPage(query.dietType ?? undefined, query.page, this.pageSize, isCurated).pipe(
            map(page => {
                if (this.destroyRef.destroyed || !this.queryState.isCurrent(query)) {
                    return null;
                }
                const resolved = resolvePaginationPage(query.page, page.totalPages);
                if (resolved !== query.page) {
                    this.pageIndex.set(resolved - 1);
                    this.writeListQuery({ ...query, page: resolved }, true);
                    return null;
                }
                return page;
            }),
        );
    }

    private writeListQuery(query: MealPlanListQuery, replaceUrl = false): void {
        void this.queryState.writeAsync(query, { replaceUrl }).catch(() => {
            /* Keep successful catalogue data on navigation failure. */
        });
    }
}
