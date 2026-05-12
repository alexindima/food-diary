import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';

import type { AdminImpersonationSession } from '../api/admin-users.service';

@Component({
    selector: 'fd-admin-sessions-section',
    imports: [DatePipe, FormsModule, FdUiButtonComponent, FdUiInputComponent],
    templateUrl: './admin-sessions-section.component.html',
    styleUrl: './admin-users.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminSessionsSectionComponent {
    public readonly search = input.required<string>();
    public readonly isLoading = input.required<boolean>();
    public readonly totalItems = input.required<number>();
    public readonly sessions = input.required<AdminImpersonationSession[]>();
    public readonly page = input.required<number>();
    public readonly totalPages = input.required<number>();

    public readonly searchChange = output<string>();
    public readonly pageChange = output<number>();
}
