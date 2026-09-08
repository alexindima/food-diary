import { CommonModule, DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { fdUiCoerceInputTextValue, FdUiInputComponent, type FdUiInputValue } from 'fd-ui-kit/input/fd-ui-input';
import { FdUiSelectComponent, type FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select';
import type { Subscription } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { AdminLoadErrorComponent } from '../../../shared/feedback/admin-load-error';
import { adminPeriod } from '../../../shared/period/admin-period';
import { AdminPeriodControlComponent } from '../../../shared/period/admin-period-control';
import { adminPage, adminQueryValue } from '../../../shared/period/admin-query';
import { AdminUserCreateDialogComponent } from '../dialogs/admin-user-create-dialog';
import { AdminUserDetailsDialogComponent, type AdminUserDetailsDialogResult } from '../dialogs/admin-user-details-dialog';
import { AdminUserEditDialogComponent } from '../dialogs/admin-user-edit-dialog';
import { AdminUserImpersonationDialogComponent } from '../dialogs/admin-user-impersonation-dialog';
import { AdminUserSetPasswordDialogComponent } from '../dialogs/admin-user-set-password-dialog';
import { AdminUsersFacade } from '../lib/admin-users.facade';
import type { AdminImpersonationStart, AdminUser, AdminUserStatusFilter } from '../models/admin-user.models';
import { AdminUsersTableComponent } from './admin-users-table';

const ADMIN_USERS_PAGE_SIZE = 20;

@Component({
    selector: 'fd-admin-users',
    imports: [
        AdminLoadErrorComponent,
        AdminPeriodControlComponent,
        TranslatePipe,
        CommonModule,
        FdUiButtonComponent,
        FdUiInputComponent,
        FdUiSelectComponent,
        AdminUsersTableComponent,
    ],
    templateUrl: './admin-users.html',
    styleUrl: './admin-users.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminUsersComponent {
    protected readonly loadFailed = signal(false);
    private readonly usersService = inject(AdminUsersFacade);
    private readonly dialogService = inject(FdUiDialogService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly document = inject(DOCUMENT);
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private request?: Subscription;
    protected readonly role = signal('');
    protected readonly confirmed = signal('');
    protected readonly lastLoginFrom = signal('');
    protected readonly lastLoginTo = signal('');

    protected readonly users = signal<AdminUser[]>([]);
    protected readonly totalPages = signal(1);
    protected readonly totalItems = signal(0);
    protected readonly page = signal(1);
    protected readonly limit = ADMIN_USERS_PAGE_SIZE;
    protected readonly isLoading = signal(false);
    protected readonly search = signal('');
    protected readonly status = signal<AdminUserStatusFilter>('active');
    protected readonly statusOptions: Array<FdUiSelectOption<AdminUserStatusFilter>> = [
        { value: 'active', label: 'Active users' },
        { value: 'inactive', label: 'Inactive users' },
        { value: 'deleted', label: 'Deleted users' },
    ];

    public constructor() {
        this.route.queryParamMap.pipe(takeUntilDestroyed()).subscribe(params => {
            this.search.set(params.get('search') ?? '');
            this.status.set(params.get('status') === 'deleted' ? 'deleted' : params.get('status') === 'inactive' ? 'inactive' : 'active');
            this.role.set(params.get('role') ?? '');
            this.confirmed.set(params.get('emailConfirmed') ?? '');
            this.lastLoginFrom.set(params.get('lastLoginFrom') ?? '');
            this.lastLoginTo.set(params.get('lastLoginTo') ?? '');
            this.page.set(adminPage(params.get('page')));
            this.loadUsers();
        });
    }

    protected applyFilters(): void {
        void this.router.navigate([], {
            relativeTo: this.route,
            queryParamsHandling: 'merge',
            queryParams: {
                search: adminQueryValue(this.search()),
                status: this.status(),
                role: adminQueryValue(this.role()),
                emailConfirmed: adminQueryValue(this.confirmed()),
                lastLoginFrom: adminQueryValue(this.lastLoginFrom()),
                lastLoginTo: adminQueryValue(this.lastLoginTo()),
                page: this.page(),
            },
        });
    }

    protected controlValue(event: Event): string {
        const target = event.target;
        return target !== null && 'value' in target && typeof target.value === 'string' ? target.value : '';
    }

    protected loadUsers(): void {
        this.request?.unsubscribe();
        const range = adminPeriod(this.route.snapshot.queryParamMap);
        if (range === null) {
            this.users.set([]);
            this.totalItems.set(0);
            this.totalPages.set(1);
            this.loadFailed.set(false);
            this.isLoading.set(false);
            return;
        }
        this.loadFailed.set(false);
        this.isLoading.set(true);
        this.request = this.usersService
            .getUsers(this.page(), this.limit, this.resolveSearchQuery(this.search()), {
                status: this.status(),
                ...range,
                role: this.role(),
                emailConfirmed: this.confirmed(),
                lastLoginFrom: this.lastLoginFrom(),
                lastLoginTo: this.lastLoginTo(),
            })
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: response => {
                    this.users.set(response.items);
                    this.totalPages.set(response.totalPages);
                    this.totalItems.set(response.totalItems);
                    this.isLoading.set(false);
                },
                error: () => {
                    this.loadFailed.set(true);
                    this.users.set([]);
                    this.totalPages.set(1);
                    this.totalItems.set(0);
                    this.isLoading.set(false);
                },
            });
    }

    protected openCreate(): void {
        this.dialogService
            .open<AdminUserCreateDialogComponent, never, boolean>(AdminUserCreateDialogComponent, { size: 'sm' })
            .afterClosed()
            .subscribe(created => {
                if (created === true) {
                    this.page.set(1);
                    this.loadUsers();
                }
            });
    }

    protected onSearchChange(value: FdUiInputValue): void {
        this.search.set(fdUiCoerceInputTextValue(value));
        this.page.set(1);
        this.applyFilters();
    }

    protected onStatusChange(value: AdminUserStatusFilter | null): void {
        this.status.set(value ?? 'active');
        this.page.set(1);
        this.applyFilters();
    }

    protected goToPage(page: number): void {
        if (page < 1 || page > this.totalPages()) {
            return;
        }

        this.page.set(page);
        this.applyFilters();
    }

    protected openDetails(user: AdminUser): void {
        this.dialogService
            .open<AdminUserDetailsDialogComponent, AdminUser, AdminUserDetailsDialogResult>(AdminUserDetailsDialogComponent, {
                size: 'xl',
                data: user,
            })
            .afterClosed()
            .subscribe(action => {
                if (action === 'edit') {
                    this.openEdit(user);
                    return;
                }

                if (action === 'setPassword') {
                    this.openSetPassword(user);
                    return;
                }

                if (action === 'impersonate') {
                    this.startImpersonation(user);
                }
            });
    }

    protected openEdit(user: AdminUser): void {
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

    protected openSetPassword(user: AdminUser): void {
        this.dialogService
            .open<AdminUserSetPasswordDialogComponent, AdminUser, boolean>(AdminUserSetPasswordDialogComponent, {
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

    protected startImpersonation(user: AdminUser): void {
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
                targetUrl.searchParams.set('impersonationCode', response.code);
                this.document.defaultView?.open(targetUrl.toString(), '_blank', 'noopener,noreferrer');
            });
    }

    private resolveSearchQuery(value: string): string | null {
        const trimmed = value.trim();
        return trimmed.length > 0 ? trimmed : null;
    }
}
