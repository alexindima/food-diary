import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { AdminUsersFacade } from '../lib/admin-users.facade';
import type { AdminUserLoginEvent } from '../models/admin-user.models';
import { AdminLoginActivitySectionComponent } from './admin-login-activity-section';

const ADMIN_LOGIN_ACTIVITY_PAGE_SIZE = 20;

@Component({
    selector: 'fd-admin-login-activity-page',
    imports: [AdminLoginActivitySectionComponent],
    templateUrl: './admin-login-activity-page.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminLoginActivityPageComponent {
    private readonly usersService = inject(AdminUsersFacade);
    private readonly destroyRef = inject(DestroyRef);
    protected readonly pageSize = ADMIN_LOGIN_ACTIVITY_PAGE_SIZE;

    protected readonly loginEvents = signal<AdminUserLoginEvent[]>([]);
    protected readonly loginEventsPage = signal(1);
    protected readonly loginEventsTotalPages = signal(1);
    protected readonly loginEventsTotalItems = signal(0);
    protected readonly loginEventsSearch = signal('');
    protected readonly isLoginEventsLoading = signal(false);

    public constructor() {
        this.loadLoginEvents();
    }

    protected onLoginEventsSearchChange(value: string): void {
        this.loginEventsSearch.set(value);
        this.loginEventsPage.set(1);
        this.loadLoginEvents();
    }

    protected goToLoginEventsPage(page: number): void {
        if (page < 1 || page > this.loginEventsTotalPages()) {
            return;
        }

        this.loginEventsPage.set(page);
        this.loadLoginEvents();
    }

    private loadLoginEvents(): void {
        this.isLoginEventsLoading.set(true);
        this.usersService
            .getLoginEvents(this.loginEventsPage(), ADMIN_LOGIN_ACTIVITY_PAGE_SIZE, this.resolveSearchQuery(this.loginEventsSearch()))
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: response => {
                    this.loginEvents.set(response.items);
                    this.loginEventsTotalPages.set(response.totalPages);
                    this.loginEventsTotalItems.set(response.totalItems);
                    this.isLoginEventsLoading.set(false);
                },
                error: () => {
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
