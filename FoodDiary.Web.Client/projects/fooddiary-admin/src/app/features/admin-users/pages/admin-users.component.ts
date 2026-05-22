import { CommonModule, DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiSelectComponent, type FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select.component';

import { environment } from '../../../../environments/environment';
import { type AdminImpersonationStart, type AdminUser, AdminUsersService, type AdminUserStatusFilter } from '../api/admin-users.service';
import { AdminUserEditDialogComponent } from '../dialogs/admin-user-edit-dialog.component';
import { AdminUserImpersonationDialogComponent } from '../dialogs/admin-user-impersonation-dialog.component';
import { AdminUsersTableComponent } from './admin-users-table.component';

const ADMIN_USERS_PAGE_SIZE = 20;

@Component({
    selector: 'fd-admin-users',
    imports: [CommonModule, FormsModule, FdUiInputComponent, FdUiSelectComponent, AdminUsersTableComponent],
    templateUrl: './admin-users.component.html',
    styleUrl: './admin-users.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminUsersComponent {
    private readonly usersService = inject(AdminUsersService);
    private readonly dialogService = inject(FdUiDialogService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly document = inject(DOCUMENT);

    public readonly users = signal<AdminUser[]>([]);
    public readonly totalPages = signal(1);
    public readonly totalItems = signal(0);
    public readonly page = signal(1);
    public readonly limit = ADMIN_USERS_PAGE_SIZE;
    public readonly isLoading = signal(false);
    public readonly search = signal('');
    public readonly status = signal<AdminUserStatusFilter>('active');
    protected readonly statusOptions: Array<FdUiSelectOption<AdminUserStatusFilter>> = [
        { value: 'active', label: 'Active users' },
        { value: 'inactive', label: 'Inactive users' },
        { value: 'deleted', label: 'Deleted users' },
    ];

    public constructor() {
        this.loadUsers();
    }

    public loadUsers(): void {
        this.isLoading.set(true);
        this.usersService
            .getUsers(this.page(), this.limit, this.resolveSearchQuery(this.search()), this.status())
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

    public onStatusChange(value: AdminUserStatusFilter | null): void {
        this.status.set(value ?? 'active');
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

                const targetUrl = new URL('/dashboard', environment.mainAppUrl);
                targetUrl.searchParams.set('impersonationToken', response.accessToken);
                this.document.defaultView?.open(targetUrl.toString(), '_blank', 'noopener,noreferrer');
            });
    }

    private resolveSearchQuery(value: string): string | null {
        const trimmed = value.trim();
        return trimmed.length > 0 ? trimmed : null;
    }
}
