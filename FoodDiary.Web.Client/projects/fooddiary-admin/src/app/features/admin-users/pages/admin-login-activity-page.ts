import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiInputComponent } from 'fd-ui-kit';
import { map } from 'rxjs';

import { AdminLoadErrorComponent } from '../../../shared/feedback/admin-load-error';
import { restoreAdminPage } from '../../../shared/period/admin-pagination';
import { adminPeriod, adminUtcPeriod } from '../../../shared/period/admin-period';
import { AdminPeriodControlComponent } from '../../../shared/period/admin-period-control';
import { adminPage, adminQueryValue } from '../../../shared/period/admin-query';
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
    private readonly usersService = inject(AdminUsersFacade);
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly request = signal<LoginEventsRequest | undefined>(undefined);
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
    protected readonly firstFilter = signal('');
    protected readonly secondFilter = signal('');
    protected readonly pageSize = ADMIN_LOGIN_ACTIVITY_PAGE_SIZE;

    protected readonly loginEvents = computed(() => this.loginEventsResult()?.items ?? []);
    protected readonly loginEventsPage = signal(1);
    protected readonly loginEventsTotalPages = computed(() => this.loginEventsResult()?.totalPages ?? 1);
    protected readonly loginEventsTotalItems = computed(() => this.loginEventsResult()?.totalItems ?? 0);
    protected readonly loginEventsSearch = signal('');
    // Keep the pager hidden until navigation to the corrected page finishes.
    protected readonly isLoginEventsLoading = computed(
        () => this.loginEventsResource.isLoading() || (this.loginEventsResource.hasValue() && this.loginEventsResult() === null),
    );

    public constructor() {
        this.route.queryParamMap.pipe(takeUntilDestroyed()).subscribe(params => {
            this.loginEventsSearch.set(params.get('search') ?? '');
            this.loginEventsPage.set(adminPage(params.get('page')));
            this.firstFilter.set(params.get('provider') ?? '');
            this.secondFilter.set(params.get('device') ?? '');
            this.loadLoginEvents();
        });
    }

    protected onLoginEventsSearchChange(value: string): void {
        this.loginEventsSearch.set(value);
        this.loginEventsPage.set(1);
        this.applyFilters();
    }

    protected goToLoginEventsPage(page: number): void {
        if (this.isLoginEventsLoading() || page < 1 || page > this.loginEventsTotalPages()) {
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
        const query = this.route.snapshot.queryParamMap;
        const range = adminPeriod(query);
        if (range === null) {
            this.request.set(undefined);
            return;
        }
        const search = this.loginEventsSearch().trim();
        this.request.set({
            page: this.loginEventsPage(),
            search: search.length > 0 ? search : null,
            filters: {
                ...adminUtcPeriod(range),
                provider: this.firstFilter(),
                device: this.secondFilter(),
                userId: query.get('userId') ?? '',
            },
        });
    }
}
