import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination.component';

import type { AdminUser } from '../api/admin-users.service';

@Component({
    selector: 'fd-admin-users-table',
    imports: [DatePipe, FdUiPaginationComponent],
    templateUrl: './admin-users-table.component.html',
    styleUrl: './admin-users.component.scss',
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
