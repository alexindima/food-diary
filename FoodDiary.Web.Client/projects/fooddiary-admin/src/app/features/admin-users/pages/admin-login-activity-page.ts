import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiInputComponent } from 'fd-ui-kit';
import type { Subscription } from 'rxjs';

import { AdminLoadErrorComponent } from '../../../shared/feedback/admin-load-error';
import { adminPeriod, adminUtcPeriod } from '../../../shared/period/admin-period';
import { AdminPeriodControlComponent } from '../../../shared/period/admin-period-control';
import { adminPage, adminQueryValue } from '../../../shared/period/admin-query';
import { AdminUsersFacade } from '../lib/admin-users.facade';
import type { AdminUserLoginEvent } from '../models/admin-user.models';
import { AdminLoginActivitySectionComponent } from './admin-login-activity-section';

const ADMIN_LOGIN_ACTIVITY_PAGE_SIZE = 20;

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
    protected readonly loadFailed = signal(false);
    private readonly usersService = inject(AdminUsersFacade);
    private readonly destroyRef = inject(DestroyRef);
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private request?: Subscription;
    protected readonly firstFilter = signal('');
    protected readonly secondFilter = signal('');
    protected readonly pageSize = ADMIN_LOGIN_ACTIVITY_PAGE_SIZE;

    protected readonly loginEvents = signal<AdminUserLoginEvent[]>([]);
    protected readonly loginEventsPage = signal(1);
    protected readonly loginEventsTotalPages = signal(1);
    protected readonly loginEventsTotalItems = signal(0);
    protected readonly loginEventsSearch = signal('');
    protected readonly isLoginEventsLoading = signal(false);

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
        if (page < 1 || page > this.loginEventsTotalPages()) {
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
        this.request?.unsubscribe();
        const range = adminPeriod(this.route.snapshot.queryParamMap);
        if (range === null) {
            this.loginEvents.set([]);
            this.isLoginEventsLoading.set(false);
            return;
        }
        const filters = { ...adminUtcPeriod(range), provider: this.firstFilter(), device: this.secondFilter() };
        this.loadFailed.set(false);
        this.isLoginEventsLoading.set(true);
        this.request = this.usersService
            .getLoginEvents(this.loginEventsPage(), ADMIN_LOGIN_ACTIVITY_PAGE_SIZE, this.resolveSearchQuery(this.loginEventsSearch()), {
                ...filters,
                userId: this.route.snapshot.queryParamMap.get('userId') ?? '',
            })
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: response => {
                    this.loginEvents.set(response.items);
                    this.loginEventsTotalPages.set(response.totalPages);
                    this.loginEventsTotalItems.set(response.totalItems);
                    this.isLoginEventsLoading.set(false);
                },
                error: () => {
                    this.loadFailed.set(true);
                    this.loginEvents.set([]);
                    this.loginEventsTotalPages.set(1);
                    this.loginEventsTotalItems.set(0);
                    this.isLoginEventsLoading.set(false);
                },
            });
    }

    private resolveSearchQuery(value: string): string | null {
        const trimmed = value.trim();
        return trimmed.length > 0 ? trimmed : null;
    }
}
