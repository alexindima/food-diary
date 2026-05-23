import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { type AdminUserLoginDeviceSummary, type AdminUserLoginEvent, AdminUsersService } from '../api/admin-users.service';
import { AdminLoginActivitySectionComponent } from './admin-login-activity-section.component';
import type { AdminUserLoginDeviceSummaryViewModel } from './admin-users.types';

const ADMIN_LOGIN_ACTIVITY_PAGE_SIZE = 20;

@Component({
    selector: 'fd-admin-login-activity-page',
    imports: [AdminLoginActivitySectionComponent],
    templateUrl: './admin-login-activity-page.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminLoginActivityPageComponent {
    private readonly usersService = inject(AdminUsersService);
    private readonly destroyRef = inject(DestroyRef);
    protected readonly pageSize = ADMIN_LOGIN_ACTIVITY_PAGE_SIZE;

    protected readonly loginEvents = signal<AdminUserLoginEvent[]>([]);
    protected readonly loginSummary = signal<AdminUserLoginDeviceSummary[]>([]);
    protected readonly loginEventsPage = signal(1);
    protected readonly loginEventsTotalPages = signal(1);
    protected readonly loginEventsTotalItems = signal(0);
    protected readonly loginEventsSearch = signal('');
    protected readonly isLoginEventsLoading = signal(false);
    protected readonly loginSummaryItems = computed<AdminUserLoginDeviceSummaryViewModel[]>(() =>
        this.loginSummary().map(item => ({
            ...item,
            label: this.formatSummaryKey(item.key),
        })),
    );

    public constructor() {
        this.loadLoginEvents();
        this.loadLoginSummary();
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

    private loadLoginSummary(): void {
        this.usersService
            .getLoginSummary()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: response => {
                    this.loginSummary.set(response);
                },
                error: () => {
                    this.loginSummary.set([]);
                },
            });
    }

    private formatSummaryKey(key: string): string {
        const [category, value] = key.split(':', 2);
        return `${category.toUpperCase()} / ${value.length > 0 ? value : 'Unknown'}`;
    }

    private resolveSearchQuery(value: string): string | null {
        const trimmed = value.trim();
        return trimmed.length > 0 ? trimmed : null;
    }
}
