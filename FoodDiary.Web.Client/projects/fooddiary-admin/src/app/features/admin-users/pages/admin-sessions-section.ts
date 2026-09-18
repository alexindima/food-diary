import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, model } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination';

import type { AdminImpersonationSession } from '../models/admin-user.models';

@Component({
    selector: 'fd-admin-sessions-section',
    imports: [RouterLink, DatePipe, FdUiPaginationComponent],
    templateUrl: './admin-sessions-section.html',
    styleUrl: './admin-users.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminSessionsSectionComponent {
    public readonly isLoading = input.required<boolean>();
    public readonly totalItems = input.required<number>();
    public readonly sessions = input.required<AdminImpersonationSession[]>();
    public readonly page = model.required<number>();
    public readonly pageSize = input.required<number>();
    public readonly totalPages = input.required<number>();
}
