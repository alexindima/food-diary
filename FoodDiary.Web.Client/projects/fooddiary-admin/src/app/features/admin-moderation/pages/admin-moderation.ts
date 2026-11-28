import { CommonModule, formatDate } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, input, linkedSignal, LOCALE_ID } from '@angular/core';
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiInputComponent, FdUiSelectComponent } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiPaginationComponent } from 'fd-ui-kit/pagination/fd-ui-pagination';
import { map } from 'rxjs';

import { AdminLoadErrorComponent } from '../../../shared/feedback/admin-load-error';
import { adminRouteInputsMatch, restoreAdminPage } from '../../../shared/period/admin-pagination';
import { adminPeriod, adminUtcPeriod } from '../../../shared/period/admin-period';
import { AdminPeriodControlComponent } from '../../../shared/period/admin-period-control';
import { adminPage, adminQueryValue, firstAdminQueryValue } from '../../../shared/period/admin-query';
import { AdminModerationContextComponent } from '../components/admin-moderation-context';
import { AdminModerationNavigationComponent } from '../components/admin-moderation-navigation';
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

type ReportFilters = { page: number; status: string; targetType: string; reporterId: string; targetId: string };
type ReportRequest = { page: number; status: string | null; filters: Record<string, string> };

@Component({
    selector: 'fd-admin-moderation',
    imports: [
        AdminPeriodControlComponent,
        AdminModerationContextComponent,
        AdminModerationNavigationComponent,
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
    public readonly page = input(undefined, { transform: firstAdminQueryValue });
    public readonly status = input(undefined, { transform: firstAdminQueryValue });
    public readonly targetType = input(undefined, { transform: firstAdminQueryValue });
    public readonly reporterId = input(undefined, { transform: firstAdminQueryValue });
    public readonly targetId = input(undefined, { transform: firstAdminQueryValue });
    public readonly period = input(undefined, { transform: firstAdminQueryValue });
    public readonly from = input(undefined, { transform: firstAdminQueryValue });
    public readonly to = input(undefined, { transform: firstAdminQueryValue });

    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly moderationFacade = inject(AdminModerationFacade);
    private readonly dialogService = inject(FdUiDialogService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly locale = inject(LOCALE_ID);

    private readonly query = computed(() => ({
        page: this.page(),
        status: this.status(),
        targetType: this.targetType(),
        reporterId: this.reporterId(),
        targetId: this.targetId(),
        period: this.period(),
        from: this.from(),
        to: this.to(),
    }));
    private readonly appliedFilters = computed((): ReportFilters => {
        const query = this.query();
        return {
            page: adminPage(query.page ?? null),
            status: query.status ?? 'Pending',
            targetType: query.targetType ?? '',
            reporterId: query.reporterId ?? '',
            targetId: query.targetId ?? '',
        };
    });
    private readonly request = linkedSignal(() => this.buildRequest());
    private readonly reportsResource = rxResource({
        params: this.request,
        stream: ({ params }) =>
            this.moderationFacade.getReports(params.page, this.limit, params.status, params.filters).pipe(
                map(response => {
                    if (this.request() !== params || !adminRouteInputsMatch(this.route, this.query())) {
                        return null;
                    }
                    return restoreAdminPage(this.router, this.route, params.page, { totalItems: response.totalItems, pageSize: this.limit })
                        ? null
                        : response;
                }),
            ),
    });
    private readonly result = computed(() => (this.reportsResource.hasValue() ? this.reportsResource.value() : null));
    protected readonly isLoading = computed(
        () => this.reportsResource.isLoading() || (this.reportsResource.hasValue() && this.result() === null),
    );
    protected readonly loadFailed = computed(() => this.reportsResource.error() !== undefined);
    protected readonly statusFilter = linkedSignal(() => this.appliedFilters().status);
    protected readonly draftTargetType = linkedSignal(() => this.appliedFilters().targetType);
    protected readonly draftReporterId = linkedSignal(() => this.appliedFilters().reporterId);
    protected readonly draftTargetId = linkedSignal(() => this.appliedFilters().targetId);
    protected readonly reportItems = computed<AdminContentReportViewModel[]>(() =>
        (this.result()?.items ?? []).map(report => ({
            ...report,
            targetIdShort: `${report.targetId.slice(0, TARGET_ID_PREVIEW_LENGTH)}...`,
            createdText: this.formatDateLabel(report.createdAtUtc),
            reviewedText: this.formatDateLabel(report.reviewedAtUtc),
        })),
    );
    protected readonly totalPages = computed(() => this.result()?.totalPages ?? 1);
    protected readonly totalItems = computed(() => this.result()?.totalItems ?? 0);
    protected readonly pageNumber = computed(() => this.appliedFilters().page);
    protected readonly limit = ADMIN_MODERATION_PAGE_SIZE;

    protected loadReports(): void {
        this.request.set(this.buildRequest());
    }

    protected applyFilters(): void {
        void this.router.navigate([], {
            relativeTo: this.route,
            queryParamsHandling: 'merge',
            queryParams: {
                page: 1,
                status: this.statusFilter(),
                targetType: adminQueryValue(this.draftTargetType()),
                reporterId: adminQueryValue(this.draftReporterId()),
                targetId: adminQueryValue(this.draftTargetId()),
            },
        });
    }

    protected goToPage(page: number): void {
        if (page < 1 || page > this.totalPages() || this.isLoading() || this.router.currentNavigation() !== null) {
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
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(result => {
                if (result?.confirmed === true) {
                    this.loadReports();
                }
            });
    }

    private formatDateLabel(value?: string | Date | null): string {
        return value === null || value === undefined ? '-' : formatDate(value, 'short', this.locale);
    }

    private buildRequest(): ReportRequest | undefined {
        const { page, status, ...filters } = this.appliedFilters();
        const { period, from, to } = this.query();
        const range = adminPeriod(convertToParamMap({ period: period ?? null, from: from ?? null, to: to ?? null }));
        return range === null ? undefined : { page, status: adminQueryValue(status), filters: { ...adminUtcPeriod(range), ...filters } };
    }
}
