import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';

import type { AdminUser } from '../api/admin-users.service';

@Component({
    selector: 'fd-admin-users-table',
    imports: [FdUiButtonComponent],
    templateUrl: './admin-users-table.component.html',
    styleUrl: './admin-users.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminUsersTableComponent {
    public readonly users = input.required<AdminUser[]>();
    public readonly totalItems = input.required<number>();
    public readonly page = input.required<number>();
    public readonly totalPages = input.required<number>();

    public readonly edit = output<AdminUser>();
    public readonly impersonate = output<AdminUser>();
    public readonly pageChange = output<number>();
}
