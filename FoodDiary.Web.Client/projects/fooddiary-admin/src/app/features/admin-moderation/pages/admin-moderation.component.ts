import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';

import { AdminModerationService } from '../api/admin-moderation.service';
import {
    AdminModerationActionDialogComponent,
    type AdminModerationActionDialogData,
    type AdminModerationActionDialogResult,
} from '../dialogs/admin-moderation-action-dialog.component';
import { type AdminContentReport } from '../models/admin-moderation.data';

@Component({
    selector: 'fd-admin-moderation',
    standalone: true,
    imports: [CommonModule, FormsModule, FdUiButtonComponent],
    templateUrl: './admin-moderation.component.html',
    styleUrl: './admin-moderation.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminModerationComponent {
    private readonly moderationService = inject(AdminModerationService);
    private readonly dialogService = inject(FdUiDialogService);
    private readonly destroyRef = inject(DestroyRef);

    public readonly reports = signal<AdminContentReport[]>([]);
    public readonly totalPages = signal(1);
    public readonly totalItems = signal(0);
    public readonly page = signal(1);
    public readonly limit = 20;
    public readonly isLoading = signal(false);
    public readonly statusFilter = signal<string>('Pending');

    public constructor() {
        this.loadReports();
    }

    public loadReports(): void {
        this.isLoading.set(true);
        this.moderationService
            .getReports(this.page(), this.limit, this.statusFilter() || null)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: response => {
                    this.reports.set(response.items);
                    this.totalPages.set(response.totalPages);
                    this.totalItems.set(response.totalItems);
                    this.isLoading.set(false);
                },
                error: () => {
                    this.reports.set([]);
                    this.totalPages.set(1);
                    this.totalItems.set(0);
                    this.isLoading.set(false);
                },
            });
    }

    public onStatusChange(status: string): void {
        this.statusFilter.set(status);
        this.page.set(1);
        this.loadReports();
    }

    public goToPage(page: number): void {
        if (page < 1 || page > this.totalPages()) {
            return;
        }

        this.page.set(page);
        this.loadReports();
    }

    public openAction(report: AdminContentReport, action: 'review' | 'dismiss'): void {
        const data: AdminModerationActionDialogData = { report, action };
        this.dialogService
            .open<AdminModerationActionDialogComponent, AdminModerationActionDialogData, AdminModerationActionDialogResult>(
                AdminModerationActionDialogComponent,
                { size: 'sm', data },
            )
            .afterClosed()
            .subscribe(result => {
                if (result?.confirmed) {
                    this.loadReports();
                }
            });
    }
}
