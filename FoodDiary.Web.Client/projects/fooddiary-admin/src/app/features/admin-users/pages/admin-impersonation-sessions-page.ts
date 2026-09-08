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
import type { AdminImpersonationSession } from '../models/admin-user.models';
import { AdminSessionsSectionComponent } from './admin-sessions-section';

const ADMIN_IMPERSONATION_SESSIONS_PAGE_SIZE = 20;

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
    protected readonly loadFailed = signal(false);
    private readonly usersService = inject(AdminUsersFacade);
    private readonly destroyRef = inject(DestroyRef);
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private request?: Subscription;
    protected readonly firstFilter = signal('');
    protected readonly secondFilter = signal('');
    protected readonly pageSize = ADMIN_IMPERSONATION_SESSIONS_PAGE_SIZE;

    protected readonly sessions = signal<AdminImpersonationSession[]>([]);
    protected readonly sessionsPage = signal(1);
    protected readonly sessionsTotalPages = signal(1);
    protected readonly sessionsTotalItems = signal(0);
    protected readonly sessionsSearch = signal('');
    protected readonly isSessionsLoading = signal(false);

    public constructor() {
        this.route.queryParamMap.pipe(takeUntilDestroyed()).subscribe(params => {
            this.sessionsSearch.set(params.get('search') ?? '');
            this.sessionsPage.set(adminPage(params.get('page')));
            this.firstFilter.set(params.get('actorId') ?? '');
            this.secondFilter.set(params.get('targetId') ?? '');
            this.loadSessions();
        });
    }

    protected onSessionsSearchChange(value: string): void {
        this.sessionsSearch.set(value);
        this.sessionsPage.set(1);
        this.applyFilters();
    }

    protected goToSessionsPage(page: number): void {
        if (page < 1 || page > this.sessionsTotalPages()) {
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
        this.request?.unsubscribe();
        const range = adminPeriod(this.route.snapshot.queryParamMap);
        if (range === null) {
            this.sessions.set([]);
            this.isSessionsLoading.set(false);
            return;
        }
        const filters = { ...adminUtcPeriod(range), actorId: this.firstFilter(), targetId: this.secondFilter() };
        this.loadFailed.set(false);
        this.isSessionsLoading.set(true);
        this.request = this.usersService
            .getImpersonationSessions(
                this.sessionsPage(),
                ADMIN_IMPERSONATION_SESSIONS_PAGE_SIZE,
                this.resolveSearchQuery(this.sessionsSearch()),
                filters,
            )
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: response => {
                    this.sessions.set(response.items);
                    this.sessionsTotalPages.set(response.totalPages);
                    this.sessionsTotalItems.set(response.totalItems);
                    this.isSessionsLoading.set(false);
                },
                error: () => {
                    this.loadFailed.set(true);
                    this.sessions.set([]);
                    this.sessionsTotalPages.set(1);
                    this.sessionsTotalItems.set(0);
                    this.isSessionsLoading.set(false);
                },
            });
    }

    private resolveSearchQuery(value: string): string | null {
        const trimmed = value.trim();
        return trimmed.length > 0 ? trimmed : null;
    }
}
