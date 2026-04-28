import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { AdminUserEditDialogComponent } from '../dialogs/admin-user-edit-dialog.component';
import { AdminImpersonationSession, AdminUser, AdminUsersService } from '../api/admin-users.service';
import { environment } from '../../../../environments/environment';

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
    public readonly limit = 20;
    public readonly isLoading = signal(false);
    public readonly search = signal('');
    public readonly includeDeleted = signal(false);
    public readonly impersonatingUserId = signal<string | null>(null);
    public readonly sessions = signal<AdminImpersonationSession[]>([]);
    public readonly sessionsPage = signal(1);
    public readonly sessionsTotalPages = signal(1);
    public readonly sessionsTotalItems = signal(0);
    public readonly sessionsSearch = signal('');
    public readonly isSessionsLoading = signal(false);

    public constructor() {
        this.loadUsers();
        this.loadSessions();
    }

    public loadUsers(): void {
        this.isLoading.set(true);
        this.usersService
            .getUsers(this.page(), this.limit, this.search().trim() || null, this.includeDeleted())
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
                if (updated) {
                    this.loadUsers();
                }
            });
    }

    public startImpersonation(user: AdminUser): void {
        const reason = window.prompt(`Reason for impersonating ${user.email}`);
        const normalizedReason = reason?.trim();
        if (!normalizedReason) {
            return;
        }

        this.impersonatingUserId.set(user.id);
        this.usersService
            .startImpersonation(user.id, normalizedReason)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: response => {
                    this.impersonatingUserId.set(null);
                    this.loadSessions();
                    const targetUrl = new URL('/dashboard', environment.mainAppUrl);
                    targetUrl.searchParams.set('impersonationToken', response.accessToken);
                    window.open(targetUrl.toString(), '_blank', 'noopener,noreferrer');
                },
                error: () => {
                    this.impersonatingUserId.set(null);
                },
            });
    }

    public loadSessions(): void {
        this.isSessionsLoading.set(true);
        this.usersService
            .getImpersonationSessions(this.sessionsPage(), this.limit, this.sessionsSearch().trim() || null)
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
}
