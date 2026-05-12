import { CommonModule, formatDate } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, LOCALE_ID, signal } from '@angular/core';
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
import type { AdminContentReport } from '../models/admin-moderation.data';

type AdminContentReportViewModel = {
    targetIdShort: string;
    createdText: string;
    reviewedText: string;
} & AdminContentReport;

const TARGET_ID_PREVIEW_LENGTH = 8;
const ADMIN_MODERATION_PAGE_SIZE = 20;

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
    private readonly locale = inject(LOCALE_ID);

    public readonly reports = signal<AdminContentReport[]>([]);
    public readonly reportItems = computed<AdminContentReportViewModel[]>(() =>
        this.reports().map(report => ({
            ...report,
            targetIdShort: `${report.targetId.slice(0, TARGET_ID_PREVIEW_LENGTH)}...`,
            createdText: this.formatDateLabel(report.createdAtUtc),
            reviewedText: this.formatDateLabel(report.reviewedAtUtc),
        })),
    );
    public readonly totalPages = signal(1);
    public readonly totalItems = signal(0);
    public readonly page = signal(1);
    public readonly limit = ADMIN_MODERATION_PAGE_SIZE;
    public readonly isLoading = signal(false);
    public readonly statusFilter = signal<string>('Pending');

    public constructor() {
        this.loadReports();
    }

    public loadReports(): void {
        this.isLoading.set(true);
        this.moderationService
            .getReports(this.page(), this.limit, this.resolveStatusFilter())
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
                if (result?.confirmed === true) {
                    this.loadReports();
                }
            });
    }

    private formatDateLabel(value?: string | Date | null): string {
        return value === null || value === undefined ? '-' : formatDate(value, 'short', this.locale);
    }

    private resolveStatusFilter(): string | null {
        const status = this.statusFilter();
        return status.length > 0 ? status : null;
    }
}
