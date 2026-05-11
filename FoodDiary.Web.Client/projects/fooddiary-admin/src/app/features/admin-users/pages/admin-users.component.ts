import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';

import { environment } from '../../../../environments/environment';
import {
    type AdminImpersonationSession,
    type AdminImpersonationStart,
    type AdminUser,
    type AdminUserLoginDeviceSummary,
    type AdminUserLoginEvent,
    AdminUsersService,
} from '../api/admin-users.service';
import { AdminUserEditDialogComponent } from '../dialogs/admin-user-edit-dialog.component';
import { AdminUserImpersonationDialogComponent } from '../dialogs/admin-user-impersonation-dialog.component';

interface AdminUserLoginDeviceSummaryViewModel extends AdminUserLoginDeviceSummary {
    label: string;
}

const ADMIN_USERS_PAGE_SIZE = 20;

@Component({
    selector: 'fd-admin-users',
    standalone: true,
    imports: [CommonModule, FormsModule, FdUiButtonComponent, FdUiInputComponent],
    templateUrl: './admin-users.component.html',
    styleUrl: './admin-users.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminUsersComponent {
    private readonly usersService = inject(AdminUsersService);
    private readonly dialogService = inject(FdUiDialogService);
    private readonly destroyRef = inject(DestroyRef);

    public readonly users = signal<AdminUser[]>([]);
    public readonly totalPages = signal(1);
    public readonly totalItems = signal(0);
    public readonly page = signal(1);
    public readonly limit = ADMIN_USERS_PAGE_SIZE;
    public readonly isLoading = signal(false);
    public readonly search = signal('');
    public readonly includeDeleted = signal(false);
    public readonly sessions = signal<AdminImpersonationSession[]>([]);
    public readonly sessionsPage = signal(1);
    public readonly sessionsTotalPages = signal(1);
    public readonly sessionsTotalItems = signal(0);
    public readonly sessionsSearch = signal('');
    public readonly isSessionsLoading = signal(false);
    public readonly loginEvents = signal<AdminUserLoginEvent[]>([]);
    public readonly loginSummary = signal<AdminUserLoginDeviceSummary[]>([]);
    public readonly loginEventsPage = signal(1);
    public readonly loginEventsTotalPages = signal(1);
    public readonly loginEventsTotalItems = signal(0);
    public readonly loginEventsSearch = signal('');
    public readonly isLoginEventsLoading = signal(false);
    public readonly loginSummaryItems = computed<AdminUserLoginDeviceSummaryViewModel[]>(() =>
        this.loginSummary().map(item => ({
            ...item,
            label: this.formatSummaryKey(item.key),
        })),
    );

    public constructor() {
        this.loadUsers();
        this.loadSessions();
        this.loadLoginEvents();
        this.loadLoginSummary();
    }

    public loadUsers(): void {
        this.isLoading.set(true);
        this.usersService
            .getUsers(this.page(), this.limit, this.resolveSearchQuery(this.search()), this.includeDeleted())
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: response => {
                    this.users.set(response.items);
                    this.totalPages.set(response.totalPages);
                    this.totalItems.set(response.totalItems);
                    this.isLoading.set(false);
                },
                error: () => {
                    this.users.set([]);
                    this.totalPages.set(1);
                    this.totalItems.set(0);
                    this.isLoading.set(false);
                },
            });
    }

    public onSearchChange(value: string): void {
        this.search.set(value);
        this.page.set(1);
        this.loadUsers();
    }

    public toggleIncludeDeleted(): void {
        this.includeDeleted.set(!this.includeDeleted());
        this.page.set(1);
        this.loadUsers();
    }

    public goToPage(page: number): void {
        if (page < 1 || page > this.totalPages()) {
            return;
        }

        this.page.set(page);
        this.loadUsers();
    }

    public openEdit(user: AdminUser): void {
        this.dialogService
            .open(AdminUserEditDialogComponent, {
                size: 'sm',
                data: user,
            })
            .afterClosed()
            .subscribe(updated => {
                if (updated === true) {
                    this.loadUsers();
                }
            });
    }

    public startImpersonation(user: AdminUser): void {
        this.dialogService
            .open<AdminUserImpersonationDialogComponent, AdminUser, AdminImpersonationStart | null>(AdminUserImpersonationDialogComponent, {
                size: 'sm',
                data: user,
            })
            .afterClosed()
            .subscribe(response => {
                if (response === null || response === undefined) {
                    return;
                }

                this.loadSessions();
                const targetUrl = new URL('/dashboard', environment.mainAppUrl);
                targetUrl.searchParams.set('impersonationToken', response.accessToken);
                window.open(targetUrl.toString(), '_blank', 'noopener,noreferrer');
            });
    }

    public loadSessions(): void {
        this.isSessionsLoading.set(true);
        this.usersService
            .getImpersonationSessions(this.sessionsPage(), this.limit, this.resolveSearchQuery(this.sessionsSearch()))
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: response => {
                    this.sessions.set(response.items);
                    this.sessionsTotalPages.set(response.totalPages);
                    this.sessionsTotalItems.set(response.totalItems);
                    this.isSessionsLoading.set(false);
                },
                error: () => {
                    this.sessions.set([]);
                    this.sessionsTotalPages.set(1);
                    this.sessionsTotalItems.set(0);
                    this.isSessionsLoading.set(false);
                },
            });
    }

    public onSessionsSearchChange(value: string): void {
        this.sessionsSearch.set(value);
        this.sessionsPage.set(1);
        this.loadSessions();
    }

    public goToSessionsPage(page: number): void {
        if (page < 1 || page > this.sessionsTotalPages()) {
            return;
        }

        this.sessionsPage.set(page);
        this.loadSessions();
    }

    public loadLoginEvents(): void {
        this.isLoginEventsLoading.set(true);
        this.usersService
            .getLoginEvents(this.loginEventsPage(), this.limit, this.resolveSearchQuery(this.loginEventsSearch()))
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

    public loadLoginSummary(): void {
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

    public onLoginEventsSearchChange(value: string): void {
        this.loginEventsSearch.set(value);
        this.loginEventsPage.set(1);
        this.loadLoginEvents();
    }

    public goToLoginEventsPage(page: number): void {
        if (page < 1 || page > this.loginEventsTotalPages()) {
            return;
        }

        this.loginEventsPage.set(page);
        this.loadLoginEvents();
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
