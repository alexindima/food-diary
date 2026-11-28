import { ChangeDetectionStrategy, Component, computed, inject, input, linkedSignal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiInputComponent } from 'fd-ui-kit';
import { map } from 'rxjs';

import { AdminLoadErrorComponent } from '../../../shared/feedback/admin-load-error';
import { adminRouteInputsMatch, restoreAdminPage } from '../../../shared/period/admin-pagination';
import { adminPeriod, adminUtcPeriod } from '../../../shared/period/admin-period';
import { AdminPeriodControlComponent } from '../../../shared/period/admin-period-control';
import { adminPage, adminQueryValue, firstAdminQueryValue } from '../../../shared/period/admin-query';
import { AdminUsersFacade } from '../lib/admin-users.facade';
import { AdminSessionsSectionComponent } from './admin-sessions-section';

const ADMIN_IMPERSONATION_SESSIONS_PAGE_SIZE = 20;

type SessionsRequest = {
    page: number;
    search: string | null;
    filters: Record<string, string>;
};

@Component({
    selector: 'fd-admin-impersonation-sessions-page',
    imports: [
        AdminPeriodControlComponent,
        TranslatePipe,
        FdUiButtonComponent,
        FdUiInputComponent,
        AdminLoadErrorComponent,
        AdminSessionsSectionComponent,
    ],
    templateUrl: './admin-impersonation-sessions-page.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminImpersonationSessionsPageComponent {
    public readonly page = input(undefined, { transform: firstAdminQueryValue });
    public readonly search = input(undefined, { transform: firstAdminQueryValue });
    public readonly actorId = input(undefined, { transform: firstAdminQueryValue });
    public readonly targetId = input(undefined, { transform: firstAdminQueryValue });
    public readonly period = input(undefined, { transform: firstAdminQueryValue });
    public readonly from = input(undefined, { transform: firstAdminQueryValue });
    public readonly to = input(undefined, { transform: firstAdminQueryValue });

    private readonly usersService = inject(AdminUsersFacade);
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly queryInputs = computed(() => ({
        page: this.page(),
        search: this.search(),
        actorId: this.actorId(),
        targetId: this.targetId(),
        period: this.period(),
        from: this.from(),
        to: this.to(),
    }));
    private readonly appliedQuery = computed(() => {
        const { page, search, actorId, targetId, period, from, to } = this.queryInputs();
        return {
            page: adminPage(page ?? null),
            search: search ?? '',
            filters: { actorId: actorId ?? '', targetId: targetId ?? '' },
            periodParams: convertToParamMap({ period: period ?? null, from: from ?? null, to: to ?? null }),
        };
    });
    private readonly sessionsRequest = linkedSignal(() => this.buildRequest());
    private readonly sessionsResource = rxResource({
        params: this.sessionsRequest,
        stream: ({ params }) =>
            this.usersService
                .getImpersonationSessions(params.page, ADMIN_IMPERSONATION_SESSIONS_PAGE_SIZE, params.search, params.filters)
                .pipe(
                    map(response => {
                        if (this.sessionsRequest() !== params || !adminRouteInputsMatch(this.route, this.queryInputs())) {
                            return null;
                        }
                        const correctingPage = restoreAdminPage(this.router, this.route, params.page, {
                            totalItems: response.totalItems,
                            pageSize: ADMIN_IMPERSONATION_SESSIONS_PAGE_SIZE,
                        });
                        return correctingPage ? null : response;
                    }),
                ),
    });
    private readonly pageResult = computed(() => (this.sessionsResource.hasValue() ? this.sessionsResource.value() : null));

    protected readonly loadFailed = computed(() => this.sessionsResource.error() !== undefined);
    protected readonly firstFilter = linkedSignal(() => this.appliedQuery().filters.actorId);
    protected readonly secondFilter = linkedSignal(() => this.appliedQuery().filters.targetId);
    protected readonly pageSize = ADMIN_IMPERSONATION_SESSIONS_PAGE_SIZE;

    protected readonly sessions = computed(() => this.pageResult()?.items ?? []);
    protected readonly sessionsPage = linkedSignal(() => this.appliedQuery().page);
    protected readonly sessionsTotalPages = computed(() => this.pageResult()?.totalPages ?? 1);
    protected readonly sessionsTotalItems = computed(() => this.pageResult()?.totalItems ?? 0);
    protected readonly sessionsSearch = linkedSignal(() => this.appliedQuery().search);
    protected readonly isSessionsLoading = computed(
        () => this.sessionsResource.isLoading() || (this.sessionsResource.hasValue() && this.sessionsResource.value() === null),
    );

    protected onSessionsSearchChange(value: string): void {
        this.sessionsSearch.set(value);
        this.sessionsPage.set(1);
        this.applyFilters();
    }

    protected goToSessionsPage(page: number): void {
        if (this.router.currentNavigation() !== null || this.isSessionsLoading() || page < 1 || page > this.sessionsTotalPages()) {
            return;
        }

        this.sessionsPage.set(page);
        this.applyFilters();
    }

    protected applyFilters(): void {
        void this.router.navigate([], {
            relativeTo: this.route,
            queryParamsHandling: 'merge',
            queryParams: {
                search: adminQueryValue(this.sessionsSearch()),
                page: this.sessionsPage(),
                actorId: adminQueryValue(this.firstFilter()),
                targetId: adminQueryValue(this.secondFilter()),
            },
        });
    }

    protected loadSessions(): void {
        this.sessionsRequest.set(this.buildRequest());
    }

    private buildRequest(): SessionsRequest | undefined {
        const { page, search, filters, periodParams } = this.appliedQuery();
        const range = adminPeriod(periodParams);
        if (range === null) {
            return undefined;
        }
        return { page, search: adminQueryValue(search.trim()), filters: { ...adminUtcPeriod(range), ...filters } };
    }
}
