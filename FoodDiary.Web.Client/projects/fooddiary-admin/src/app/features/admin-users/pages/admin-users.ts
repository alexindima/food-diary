import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, input, linkedSignal } from '@angular/core';
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiDateInputComponent } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { fdUiCoerceInputTextValue, FdUiInputComponent, type FdUiInputValue } from 'fd-ui-kit/input/fd-ui-input';
import { FdUiSelectComponent, type FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select';
import { finalize, map } from 'rxjs';

import { connectAdminDevelopmentScreen } from '../../../shared/development/admin-development-screen';
import { AdminLoadErrorComponent } from '../../../shared/feedback/admin-load-error';
import { restoreAdminPage } from '../../../shared/period/admin-pagination';
import { adminPeriod } from '../../../shared/period/admin-period';
import { AdminPeriodControlComponent } from '../../../shared/period/admin-period-control';
import { adminPage, adminQueryValue, firstAdminQueryValue } from '../../../shared/period/admin-query';
import { AdminUserCreateDialogComponent } from '../dialogs/admin-user-create-dialog';
import { AdminUsersFacade } from '../lib/admin-users.facade';
import type { AdminUser, AdminUserStatusFilter, PagedResponse } from '../models/admin-user.models';
import { AdminUsersTableComponent } from './admin-users-table';

const ADMIN_USERS_PAGE_SIZE = 20;

type UserListFilters = {
    page: number;
    search: string;
    status: AdminUserStatusFilter;
    role: string;
    emailConfirmed: string;
    lastActiveFrom: string;
    lastActiveTo: string;
};

type UserListRequest = { page: number; search: string | null; filters: Record<string, string> };

@Component({
    selector: 'fd-admin-users',
    imports: [
        FdUiDateInputComponent,
        AdminLoadErrorComponent,
        AdminPeriodControlComponent,
        TranslatePipe,
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
    private readonly usersService = inject(AdminUsersFacade);
    private readonly dialogService = inject(FdUiDialogService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    public readonly page = input(undefined, { transform: firstAdminQueryValue });
    public readonly search = input(undefined, { transform: firstAdminQueryValue });
    public readonly status = input(undefined, { transform: firstAdminQueryValue });
    public readonly role = input(undefined, { transform: firstAdminQueryValue });
    public readonly emailConfirmed = input(undefined, { transform: firstAdminQueryValue });
    public readonly lastActiveFrom = input(undefined, { transform: firstAdminQueryValue });
    public readonly lastActiveTo = input(undefined, { transform: firstAdminQueryValue });
    public readonly period = input(undefined, { transform: firstAdminQueryValue });
    public readonly from = input(undefined, { transform: firstAdminQueryValue });
    public readonly to = input(undefined, { transform: firstAdminQueryValue });

    private readonly queryFilters = computed(() => ({
        page: this.page() ?? null,
        search: this.search() ?? null,
        status: this.status() ?? null,
        role: this.role() ?? null,
        emailConfirmed: this.emailConfirmed() ?? null,
        lastActiveFrom: this.lastActiveFrom() ?? null,
        lastActiveTo: this.lastActiveTo() ?? null,
    }));
    private readonly registrationPeriod = computed(() =>
        adminPeriod(convertToParamMap({ period: this.period() ?? null, from: this.from() ?? null, to: this.to() ?? null })),
    );
    private readonly appliedFilters = computed((): UserListFilters => {
        const query = this.queryFilters();
        return {
            page: adminPage(query.page),
            search: query.search ?? '',
            status: query.status === 'deleted' || query.status === 'inactive' ? query.status : 'active',
            role: query.role ?? '',
            emailConfirmed: query.emailConfirmed ?? '',
            lastActiveFrom: query.lastActiveFrom ?? '',
            lastActiveTo: query.lastActiveTo ?? '',
        };
    });
    private readonly request = linkedSignal(() => this.buildRequest());
    private readonly usersResource = rxResource({
        params: this.request,
        stream: ({ params }) =>
            this.usersService.getUsers(params.page, this.limit, params.search, params.filters).pipe(
                map(response => {
                    if (this.request() !== params) {
                        return null;
                    }
                    const correctingPage = restoreAdminPage(this.router, this.route, params.page, {
                        totalItems: response.totalItems,
                        pageSize: this.limit,
                    });
                    return correctingPage ? null : response;
                }),
            ),
    });
    private readonly pageResult = computed(() => (this.usersResource.hasValue() ? this.usersResource.value() : null));
    protected readonly loadFailed = computed(() => this.usersResource.error() !== undefined);
    protected readonly isLoading = computed(
        () => this.usersResource.isLoading() || (this.usersResource.hasValue() && this.pageResult() === null),
    );
    private readonly retainedPage = linkedSignal({
        source: () => ({ result: this.pageResult(), loading: this.isLoading() }),
        computation: ({ result, loading }, previous): PagedResponse<AdminUser> | null =>
            result ?? (loading ? (previous?.value ?? null) : null),
    });
    private readonly draftDefaults = computed(() => ({ ...this.appliedFilters(), period: this.registrationPeriod() }));
    protected readonly users = computed(() => this.retainedPage()?.items ?? []);
    protected readonly totalPages = computed(() => this.retainedPage()?.totalPages ?? 1);
    protected readonly totalItems = computed(() => this.retainedPage()?.totalItems ?? 0);
    protected readonly draftPage = linkedSignal(() => this.draftDefaults().page);
    protected readonly draftSearch = linkedSignal(() => this.draftDefaults().search);
    protected readonly draftStatus = linkedSignal(() => this.draftDefaults().status);
    protected readonly draftRole = linkedSignal(() => this.draftDefaults().role);
    protected readonly draftConfirmed = linkedSignal(() => this.draftDefaults().emailConfirmed);
    protected readonly draftLastActiveFrom = linkedSignal(() => this.draftDefaults().lastActiveFrom);
    protected readonly draftLastActiveTo = linkedSignal(() => this.draftDefaults().lastActiveTo);
    protected readonly limit = ADMIN_USERS_PAGE_SIZE;
    protected readonly statusOptions: Array<FdUiSelectOption<AdminUserStatusFilter>> = [
        { value: 'active', label: 'Active users' },
        { value: 'inactive', label: 'Inactive users' },
        { value: 'deleted', label: 'Deleted users' },
    ];

    public constructor() {
        connectAdminDevelopmentScreen({
            kind: 'admin-users',
            filterFields: Object.keys(this.queryFilters()),
            read: () => ({
                filters: this.queryFilters(),
                page: this.draftPage(),
                totalItems: this.totalItems(),
                totalPages: this.totalPages(),
                loading: this.isLoading(),
                error: this.loadFailed(),
                items: this.users().map(user => ({ id: user.id })),
            }),
            setFiltersAsync: async params => {
                await this.router.navigate([], { relativeTo: this.route, queryParams: params });
            },
            openEntityAsync: async id => {
                if (!this.users().some(user => user.id === id)) {
                    throw new Error('ID is not in the current list.');
                }
                await this.router.navigate(['/users', id], { queryParamsHandling: 'preserve' });
            },
        });
    }

    protected applyFilters(): void {
        void this.router.navigate([], {
            relativeTo: this.route,
            queryParamsHandling: 'merge',
            queryParams: {
                search: adminQueryValue(this.draftSearch()),
                status: this.draftStatus(),
                role: adminQueryValue(this.draftRole()),
                emailConfirmed: adminQueryValue(this.draftConfirmed()),
                lastActiveFrom: adminQueryValue(this.draftLastActiveFrom()),
                lastActiveTo: adminQueryValue(this.draftLastActiveTo()),
                page: this.draftPage(),
            },
        });
    }

    protected loadUsers(): void {
        this.request.set(this.buildRequest());
    }

    protected openCreate(): void {
        const dialogRef = this.dialogService.open<AdminUserCreateDialogComponent, never, boolean>(AdminUserCreateDialogComponent, {
            size: 'sm',
        });
        const unregisterCleanup = this.destroyRef.onDestroy(() => {
            dialogRef.close();
        });
        dialogRef
            .afterClosed()
            .pipe(takeUntilDestroyed(this.destroyRef), finalize(unregisterCleanup))
            .subscribe(created => {
                if (created === true) {
                    this.refreshAfterCreate();
                }
            });
    }

    protected onSearchChange(value: FdUiInputValue): void {
        this.draftSearch.set(fdUiCoerceInputTextValue(value));
        this.draftPage.set(1);
        this.applyFilters();
    }

    protected onStatusChange(value: AdminUserStatusFilter | null): void {
        this.draftStatus.set(value ?? 'active');
        this.draftPage.set(1);
        this.applyFilters();
    }

    protected goToPage(page: number): void {
        if (this.isLoading() || page < 1 || page > this.totalPages()) {
            return;
        }

        this.draftPage.set(page);
        this.applyFilters();
    }

    protected openDetails(user: AdminUser): void {
        void this.router.navigate(['/users', user.id], { queryParamsHandling: 'preserve' });
    }

    private refreshAfterCreate(): void {
        const currentPage = this.page();
        this.draftPage.set(1);
        if (currentPage === undefined || currentPage === '1') {
            this.loadUsers();
            return;
        }
        void this.router.navigate([], {
            relativeTo: this.route,
            queryParams: { page: 1 },
            queryParamsHandling: 'merge',
            replaceUrl: true,
        });
    }

    private buildRequest(): UserListRequest | undefined {
        const { page, search, ...filters } = this.appliedFilters();
        const range = this.registrationPeriod();
        return range === null ? undefined : { page, search: adminQueryValue(search.trim()), filters: { ...filters, ...range } };
    }
}
