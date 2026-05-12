import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';

import type { AdminUserLoginEvent } from '../api/admin-users.service';
import type { AdminUserLoginDeviceSummaryViewModel } from './admin-users.types';

@Component({
    selector: 'fd-admin-login-activity-section',
    imports: [DatePipe, FormsModule, FdUiButtonComponent, FdUiInputComponent],
    templateUrl: './admin-login-activity-section.component.html',
    styleUrl: './admin-users.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminLoginActivitySectionComponent {
    public readonly search = input.required<string>();
    public readonly summaryItems = input.required<AdminUserLoginDeviceSummaryViewModel[]>();
    public readonly isLoading = input.required<boolean>();
    public readonly totalItems = input.required<number>();
    public readonly events = input.required<AdminUserLoginEvent[]>();
    public readonly page = input.required<number>();
    public readonly totalPages = input.required<number>();

    public readonly searchChange = output<string>();
    public readonly pageChange = output<number>();
}
