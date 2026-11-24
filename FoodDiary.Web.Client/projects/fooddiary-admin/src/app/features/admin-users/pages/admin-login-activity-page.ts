import { ChangeDetectionStrategy, Component, computed, inject, input, linkedSignal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiInputComponent } from 'fd-ui-kit';
import { map } from 'rxjs';

import { AdminLoadErrorComponent } from '../../../shared/feedback/admin-load-error';
import { restoreAdminPage } from '../../../shared/period/admin-pagination';
import { adminPeriod, adminUtcPeriod } from '../../../shared/period/admin-period';
import { AdminPeriodControlComponent } from '../../../shared/period/admin-period-control';
import { adminPage, adminQueryValue, firstAdminQueryValue } from '../../../shared/period/admin-query';
import { AdminUsersFacade } from '../lib/admin-users.facade';
import { AdminLoginActivitySectionComponent } from './admin-login-activity-section';

const ADMIN_LOGIN_ACTIVITY_PAGE_SIZE = 20;

type LoginEventsRequest = { page: number; search: string | null; filters: Record<string, string> };

@Component({
    selector: 'fd-admin-login-activity-page',
    imports: [
        AdminPeriodControlComponent,
        TranslatePipe,
        FdUiButtonComponent,
        FdUiInputComponent,
        AdminLoadErrorComponent,
        AdminLoginActivitySectionComponent,
    ],
    templateUrl: './admin-login-activity-page.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminLoginActivityPageComponent {
    public readonly page = input(undefined, { transform: firstAdminQueryValue });
    public readonly search = input(undefined, { transform: firstAdminQueryValue });
    public readonly provider = input(undefined, { transform: firstAdminQueryValue });
    public readonly device = input(undefined, { transform: firstAdminQueryValue });
    public readonly userId = input(undefined, { transform: firstAdminQueryValue });
    public readonly period = input(undefined, { transform: firstAdminQueryValue });
    public readonly from = input(undefined, { transform: firstAdminQueryValue });
    public readonly to = input(undefined, { transform: firstAdminQueryValue });

    private readonly usersService = inject(AdminUsersFacade);
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly appliedQuery = computed(() => ({
        page: adminPage(this.page() ?? null),
        search: this.search() ?? '',
        filters: { provider: this.provider() ?? '', device: this.device() ?? '' },
        userId: this.userId() ?? '',
        periodParams: convertToParamMap({ period: this.period() ?? null, from: this.from() ?? null, to: this.to() ?? null }),
    }));
    private readonly request = linkedSignal(() => {
        const { page, search, filters } = this.appliedQuery();
        return this.buildRequest(page, search, filters);
    });
    private readonly loginEventsResource = rxResource({
        params: this.request,
        stream: ({ params }) =>
            this.usersService.getLoginEvents(params.page, ADMIN_LOGIN_ACTIVITY_PAGE_SIZE, params.search, params.filters).pipe(
                map(response => {
                    if (this.request() !== params) {
                        return null;
                    }
                    const correctingPage = restoreAdminPage(this.router, this.route, params.page, {
                        totalItems: response.totalItems,
                        pageSize: ADMIN_LOGIN_ACTIVITY_PAGE_SIZE,
                    });
                    return correctingPage ? null : response;
                }),
            ),
    });
    private readonly loginEventsResult = computed(() => (this.loginEventsResource.hasValue() ? this.loginEventsResource.value() : null));

    protected readonly loadFailed = computed(() => this.loginEventsResource.error() !== undefined);
    protected readonly firstFilter = linkedSignal(() => this.appliedQuery().filters.provider);
    protected readonly secondFilter = linkedSignal(() => this.appliedQuery().filters.device);
    protected readonly pageSize = ADMIN_LOGIN_ACTIVITY_PAGE_SIZE;

    protected readonly loginEvents = computed(() => this.loginEventsResult()?.items ?? []);
    protected readonly loginEventsPage = linkedSignal(() => this.appliedQuery().page);
    protected readonly loginEventsTotalPages = computed(() => this.loginEventsResult()?.totalPages ?? 1);
    protected readonly loginEventsTotalItems = computed(() => this.loginEventsResult()?.totalItems ?? 0);
    protected readonly loginEventsSearch = linkedSignal(() => this.appliedQuery().search);
    // Keep the pager hidden until navigation to the corrected page finishes.
    protected readonly isLoginEventsLoading = computed(
        () => this.loginEventsResource.isLoading() || (this.loginEventsResource.hasValue() && this.loginEventsResult() === null),
    );

    protected onLoginEventsSearchChange(value: string): void {
        this.loginEventsSearch.set(value);
        this.loginEventsPage.set(1);
        this.applyFilters();
    }

    protected goToLoginEventsPage(page: number): void {
        if (this.router.currentNavigation() !== null || this.isLoginEventsLoading() || page < 1 || page > this.loginEventsTotalPages()) {
            return;
        }

        this.loginEventsPage.set(page);
        this.applyFilters();
    }

    protected applyFilters(): void {
        void this.router.navigate([], {
            relativeTo: this.route,
            queryParamsHandling: 'merge',
            queryParams: {
                search: adminQueryValue(this.loginEventsSearch()),
                page: this.loginEventsPage(),
                provider: adminQueryValue(this.firstFilter()),
                device: adminQueryValue(this.secondFilter()),
            },
        });
    }

    protected loadLoginEvents(): void {
        this.request.set(
            this.buildRequest(this.loginEventsPage(), this.loginEventsSearch(), {
                provider: this.firstFilter(),
                device: this.secondFilter(),
            }),
        );
    }

    private buildRequest(page: number, search: string, filters: Record<string, string>): LoginEventsRequest | undefined {
        const { periodParams, userId } = this.appliedQuery();
        const range = adminPeriod(periodParams);
        if (range === null) {
            return undefined;
        }
        return { page, search: adminQueryValue(search.trim()), filters: { ...adminUtcPeriod(range), ...filters, userId } };
    }
}
