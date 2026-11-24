import { ChangeDetectionStrategy, Component, computed, inject, input, linkedSignal } from '@angular/core';
import { rxResource, toSignal } from '@angular/core/rxjs-interop';
import { form, FormField, FormRoot, maxLength } from '@angular/forms/signals';
import { ActivatedRoute, convertToParamMap, Router, RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiInputComponent, FdUiPaginationComponent, FdUiSelectComponent, FdUiTabsComponent } from 'fd-ui-kit';
import { map } from 'rxjs';

import { AdminLoadErrorComponent } from '../../../shared/feedback/admin-load-error';
import { restoreAdminPage } from '../../../shared/period/admin-pagination';
import { adminPeriod, adminUtcPeriod } from '../../../shared/period/admin-period';
import { AdminPeriodControlComponent } from '../../../shared/period/admin-period-control';
import { adminPage, firstAdminQueryValue } from '../../../shared/period/admin-query';
import { AdminBugReportListComponent } from '../components/admin-bug-report-list';
import { REPORT_STATUSES, reportStatusPresentation } from '../lib/admin-bug-presentation';
import { AdminBugsFacade } from '../lib/admin-bugs.facade';

const SEARCH_MAX_LENGTH = 320;
const STATUS_MAX_LENGTH = 32;
const PAGE_SIZE = 25;
type BugReportRequest = Record<string, string | number> & { page: number; id?: string };

@Component({
    selector: 'fd-admin-bugs',
    imports: [
        AdminBugReportListComponent,
        TranslatePipe,
        RouterLink,
        FormField,
        FormRoot,
        FdUiButtonComponent,
        FdUiInputComponent,
        FdUiSelectComponent,
        FdUiPaginationComponent,
        AdminLoadErrorComponent,
        AdminPeriodControlComponent,
        FdUiTabsComponent,
    ],
    styleUrls: ['../../../shared/admin-records.scss', './admin-bugs.scss'],
    templateUrl: './admin-bugs.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminBugsPageComponent {
    public readonly id = input(undefined, { transform: firstAdminQueryValue });
    public readonly page = input(undefined, { transform: firstAdminQueryValue });
    public readonly source = input(undefined, { transform: firstAdminQueryValue });
    public readonly search = input(undefined, { transform: firstAdminQueryValue });
    public readonly status = input(undefined, { transform: firstAdminQueryValue });
    public readonly period = input(undefined, { transform: firstAdminQueryValue });
    public readonly from = input(undefined, { transform: firstAdminQueryValue });
    public readonly to = input(undefined, { transform: firstAdminQueryValue });

    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly api = inject(AdminBugsFacade);
    private readonly translate = inject(TranslateService);
    private readonly languageChange = toSignal(this.translate.onLangChange, { initialValue: null });
    private readonly detailRoute = this.route.routeConfig?.path === ':id';
    private readonly appliedQuery = computed(() => ({
        id: this.detailRoute ? (this.id() ?? null) : null,
        page: adminPage(this.page() ?? null),
        source: this.source() === 'logs' ? 'logs' : 'email',
        filters: { search: this.search() ?? '', status: this.status() ?? '' },
        periodParams: convertToParamMap({ period: this.period() ?? null, from: this.from() ?? null, to: this.to() ?? null }),
    }));
    private readonly requestedPage = linkedSignal(() => this.buildRequest());
    private readonly reportResource = rxResource({
        params: this.requestedPage,
        stream: ({ params }) =>
            this.api.getPage(params).pipe(
                map(result => {
                    if (this.requestedPage() !== params) {
                        return null;
                    }
                    const correctingPage =
                        params.id === undefined &&
                        restoreAdminPage(this.router, this.route, params.page, { totalItems: result.totalItems, pageSize: PAGE_SIZE });
                    return correctingPage ? null : result;
                }),
            ),
    });
    protected readonly result = computed(() => (this.reportResource.hasValue() ? this.reportResource.value() : null));
    protected readonly loading = computed(
        () => this.reportResource.isLoading() || (this.reportResource.hasValue() && this.reportResource.value() === null),
    );
    protected readonly failed = computed(() => this.reportResource.error() !== undefined);
    protected readonly currentPage = computed(() => this.appliedQuery().page);
    protected readonly detailId = computed(() => this.appliedQuery().id);
    protected readonly activeSource = computed(() => {
        const source = this.appliedQuery().source;
        return this.detailId() === null ? source : (this.result()?.items[0]?.source ?? source);
    });
    protected readonly tabs = [
        { value: 'email', labelKey: 'ADMIN_BUGS.TAB_EMAIL' },
        { value: 'logs', labelKey: 'ADMIN_BUGS.TAB_LOGS' },
    ];
    protected readonly statusOptions = computed(() => {
        this.languageChange();
        return [
            { value: '', label: this.translatedLabel('ADMIN_COMMON.ALL') },
            ...REPORT_STATUSES.map(value => ({ value, label: this.translatedLabel(reportStatusPresentation(value).key) })),
        ];
    });
    protected readonly formModel = linkedSignal(() => this.appliedQuery().filters);
    private readonly submitFormAsync = async (): Promise<void> => {
        await this.applyAsync();
    };
    protected readonly form = form(
        this.formModel,
        path => {
            maxLength(path.search, SEARCH_MAX_LENGTH);
            maxLength(path.status, STATUS_MAX_LENGTH);
        },
        { submission: { action: this.submitFormAsync } },
    );

    private buildRequest(): BugReportRequest | undefined {
        const { id, page, source, filters, periodParams } = this.appliedQuery();
        if (id !== null) {
            return { page: 1, limit: PAGE_SIZE, id };
        }
        const period = adminPeriod(periodParams);
        return period === null ? undefined : { page, limit: PAGE_SIZE, source, ...filters, ...adminUtcPeriod(period) };
    }

    private translatedLabel(key: string): string {
        const value: unknown = this.translate.instant(key);
        return typeof value === 'string' ? value : key;
    }

    protected async applyAsync(): Promise<boolean> {
        const navigated = await this.router.navigate(['/bugs'], {
            queryParams: { ...this.formModel(), source: this.activeSource(), page: 1 },
            queryParamsHandling: 'merge',
        });
        if (!navigated) {
            this.retry();
        }
        return navigated;
    }

    protected goToPage(page: number): void {
        if (this.router.currentNavigation() === null && !this.loading()) {
            void this.router.navigate([], { relativeTo: this.route, queryParams: { page }, queryParamsHandling: 'merge' });
        }
    }

    protected selectSource(source: string): void {
        void this.router.navigate(['/bugs'], {
            queryParams: { source, page: 1, search: null, status: null },
            queryParamsHandling: 'merge',
        });
    }

    protected resetFilters(): void {
        void this.router.navigate(['/bugs'], {
            queryParams: { source: this.activeSource(), page: 1, search: null, status: null, period: null, from: null, to: null },
            queryParamsHandling: 'merge',
        });
    }

    protected retry(): void {
        this.formModel.set(this.appliedQuery().filters);
        this.requestedPage.set(this.buildRequest());
    }
}
