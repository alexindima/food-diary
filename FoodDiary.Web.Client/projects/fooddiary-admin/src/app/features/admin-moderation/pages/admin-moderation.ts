import { CommonModule, formatDate } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, LOCALE_ID, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiInputComponent, FdUiSelectComponent } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination';
import type { Subscription } from 'rxjs';

import { AdminLoadErrorComponent } from '../../../shared/feedback/admin-load-error';
import { adminPeriod, adminUtcPeriod } from '../../../shared/period/admin-period';
import { AdminPeriodControlComponent } from '../../../shared/period/admin-period-control';
import { adminPage, adminQueryValue } from '../../../shared/period/admin-query';
import { AdminModerationContextComponent } from '../components/admin-moderation-context';
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
    imports: [
        AdminPeriodControlComponent,
        AdminModerationContextComponent,
        TranslatePipe,
        FdUiInputComponent,
        FdUiSelectComponent,
        AdminLoadErrorComponent,
        CommonModule,
        FdUiButtonComponent,
        FdUiPaginationComponent,
    ],
    templateUrl: './admin-moderation.html',
    styleUrl: './admin-moderation.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminModerationComponent {
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private request?: Subscription;
    protected readonly targetType = signal('');
    protected readonly reporterId = signal('');
    protected readonly targetId = signal('');
    protected readonly loadFailed = signal(false);
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
        this.route.queryParamMap.pipe(takeUntilDestroyed()).subscribe(params => {
            this.statusFilter.set(params.get('status') ?? 'Pending');
            this.targetType.set(params.get('targetType') ?? '');
            this.reporterId.set(params.get('reporterId') ?? '');
            this.targetId.set(params.get('targetId') ?? '');
            this.page.set(adminPage(params.get('page')));
            this.loadReports();
        });
    }

    protected loadReports(): void {
        this.request?.unsubscribe();
        const range = adminPeriod(this.route.snapshot.queryParamMap);
        if (range === null) {
            this.reports.set([]);
            this.totalItems.set(0);
            this.isLoading.set(false);
            return;
        }
        this.loadFailed.set(false);
        this.isLoading.set(true);
        this.request = this.moderationFacade
            .getReports(this.page(), this.limit, this.resolveStatusFilter(), {
                ...adminUtcPeriod(range),
                targetType: this.targetType(),
                reporterId: this.reporterId(),
                targetId: this.targetId(),
            })
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: response => {
                    this.reports.set(response.items);
                    this.totalPages.set(response.totalPages);
                    this.totalItems.set(response.totalItems);
                    this.isLoading.set(false);
                },
                error: () => {
                    this.loadFailed.set(true);
                    this.reports.set([]);
                    this.totalPages.set(1);
                    this.totalItems.set(0);
                    this.isLoading.set(false);
                },
            });
    }

    protected onStatusChange(status: string): void {
        this.statusFilter.set(status);
        this.applyFilters();
    }

    protected applyFilters(): void {
        void this.router.navigate([], {
            relativeTo: this.route,
            queryParamsHandling: 'merge',
            queryParams: {
                page: 1,
                status: this.statusFilter(),
                targetType: adminQueryValue(this.targetType()),
                reporterId: adminQueryValue(this.reporterId()),
                targetId: adminQueryValue(this.targetId()),
            },
        });
    }

    protected getSelectValue(event: Event): string {
        return event.target instanceof HTMLSelectElement ? event.target.value : '';
    }

    protected goToPage(page: number): void {
        if (page < 1 || page > this.totalPages()) {
            return;
        }

        void this.router.navigate([], { relativeTo: this.route, queryParamsHandling: 'merge', queryParams: { page } });
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
