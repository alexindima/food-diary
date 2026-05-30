import { CommonModule, formatDate } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, LOCALE_ID, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination';

import {
    AdminModerationActionDialogComponent,
    type AdminModerationActionDialogData,
    type AdminModerationActionDialogResult,
} from '../dialogs/admin-moderation-action-dialog';
import { AdminModerationFacade } from '../lib/admin-moderation.facade';
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
    imports: [CommonModule, FormsModule, FdUiButtonComponent, FdUiPaginationComponent],
    templateUrl: './admin-moderation.html',
    styleUrl: './admin-moderation.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminModerationComponent {
    private readonly moderationFacade = inject(AdminModerationFacade);
    private readonly dialogService = inject(FdUiDialogService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly locale = inject(LOCALE_ID);

    protected readonly reports = signal<AdminContentReport[]>([]);
    protected readonly reportItems = computed<AdminContentReportViewModel[]>(() =>
        this.reports().map(report => ({
            ...report,
            targetIdShort: `${report.targetId.slice(0, TARGET_ID_PREVIEW_LENGTH)}...`,
            createdText: this.formatDateLabel(report.createdAtUtc),
            reviewedText: this.formatDateLabel(report.reviewedAtUtc),
        })),
    );
    protected readonly totalPages = signal(1);
    protected readonly totalItems = signal(0);
    protected readonly page = signal(1);
    protected readonly limit = ADMIN_MODERATION_PAGE_SIZE;
    protected readonly isLoading = signal(false);
    protected readonly statusFilter = signal<string>('Pending');

    public constructor() {
        this.loadReports();
    }

    protected loadReports(): void {
        this.isLoading.set(true);
        this.moderationFacade
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

    protected onStatusChange(status: string): void {
        this.statusFilter.set(status);
        this.page.set(1);
        this.loadReports();
    }

    protected goToPage(page: number): void {
        if (page < 1 || page > this.totalPages()) {
            return;
        }

        this.page.set(page);
        this.loadReports();
    }

    protected openAction(report: AdminContentReport, action: 'review' | 'dismiss'): void {
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
