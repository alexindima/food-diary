import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination';

import type { AdminImpersonationSession } from '../models/admin-user.models';

@Component({
    selector: 'fd-admin-sessions-section',
    imports: [DatePipe, FdUiInputComponent, FdUiPaginationComponent],
    templateUrl: './admin-sessions-section.html',
    styleUrl: './admin-users.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminSessionsSectionComponent {
    public readonly search = input.required<string>();
    public readonly isLoading = input.required<boolean>();
    public readonly totalItems = input.required<number>();
    public readonly sessions = input.required<AdminImpersonationSession[]>();
    public readonly page = input.required<number>();
    public readonly pageSize = input.required<number>();
    public readonly totalPages = input.required<number>();

    public readonly searchChange = output<string>();
    public readonly pageChange = output<number>();
}
