import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination';

import type { AdminUser } from '../models/admin-user.models';

@Component({
    selector: 'fd-admin-users-table',
    imports: [DatePipe, FdUiPaginationComponent],
    templateUrl: './admin-users-table.html',
    styleUrl: './admin-users.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminUsersTableComponent {
    public readonly users = input.required<AdminUser[]>();
    public readonly totalItems = input.required<number>();
    public readonly page = input.required<number>();
    public readonly pageSize = input.required<number>();
    public readonly totalPages = input.required<number>();

    public readonly details = output<AdminUser>();
    public readonly pageChange = output<number>();
}
