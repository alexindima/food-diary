import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, linkedSignal } from '@angular/core';
import { rxResource, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, convertToParamMap, Router, RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiInputComponent, FdUiPaginationComponent, FdUiSelectComponent } from 'fd-ui-kit';
import { map } from 'rxjs';

import { connectAdminDevelopmentScreen } from '../../../shared/development/admin-development-screen';
import { restoreAdminPage } from '../../../shared/period/admin-pagination';
import { adminPeriod, adminUtcPeriod } from '../../../shared/period/admin-period';
import { AdminPeriodControlComponent } from '../../../shared/period/admin-period-control';
import { adminPage, adminQueryValue, firstAdminQueryValue } from '../../../shared/period/admin-query';
import { OutgoingEmailDetailsComponent } from '../components/outgoing-email-details';
import { OutgoingStatusCountsComponent } from '../components/outgoing-status-counts';
import { AdminOutgoingEmailsFacade } from '../lib/admin-outgoing-emails.facade';
import type { OutgoingEmailPage } from '../models/outgoing-email';

const PAGE_SIZE = 50;

type OutgoingPageRequest = {
    page: number;
    purpose: string;
    status: string;
    filters: Record<string, string>;
    hasMessageId: boolean;
};

type OutgoingListFilters = Pick<OutgoingPageRequest, 'page' | 'purpose' | 'status'> & { recipient: string };

@Component({
    selector: 'fd-admin-outgoing-emails',
    imports: [
        OutgoingStatusCountsComponent,
        CommonModule,
        AdminPeriodControlComponent,
        RouterLink,
        TranslatePipe,
        FdUiButtonComponent,
        FdUiInputComponent,
        FdUiSelectComponent,
        FdUiPaginationComponent,
        OutgoingEmailDetailsComponent,
    ],
    templateUrl: './admin-outgoing-emails.html',
    styleUrl: './admin-outgoing-emails.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminOutgoingEmailsComponent {
    private readonly api = inject(AdminOutgoingEmailsFacade);
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    public readonly page = input(undefined, { transform: firstAdminQueryValue });
    public readonly purpose = input(undefined, { transform: firstAdminQueryValue });
    public readonly status = input(undefined, { transform: firstAdminQueryValue });
    public readonly recipient = input(undefined, { transform: firstAdminQueryValue });
    public readonly period = input(undefined, { transform: firstAdminQueryValue });
    public readonly from = input(undefined, { transform: firstAdminQueryValue });
    public readonly to = input(undefined, { transform: firstAdminQueryValue });
    public readonly id = input(undefined, { transform: firstAdminQueryValue });
    public readonly correlationId = input(undefined, { transform: firstAdminQueryValue });

    private readonly queryFilters = computed(() => ({
        page: this.page() ?? null,
        purpose: this.purpose() ?? null,
        status: this.status() ?? null,
        recipient: this.recipient() ?? null,
        period: this.period() ?? null,
        from: this.from() ?? null,
        to: this.to() ?? null,
    }));
    private readonly appliedFilters = computed(() => {
        const query = this.queryFilters();
        return {
            page: adminPage(query.page),
            purpose: query.purpose ?? '',
            status: query.status ?? '',
            recipient: query.recipient ?? '',
            id: this.id(),
            correlationId: this.correlationId() ?? '',
        };
    });
    private readonly pageRequest = linkedSignal(() => this.buildRequest(this.appliedFilters()));
    private readonly pageResource = rxResource({
        params: this.pageRequest,
        stream: ({ params }) =>
            this.api.getPage(params.page, params.purpose, params.status, params.filters).pipe(
                map(result => {
                    if (this.pageRequest() !== params) {
                        return null;
                    }
                    const correctingPage = restoreAdminPage(this.router, this.route, params.page, {
                        totalItems: result.totalItems,
                        pageSize: PAGE_SIZE,
                    });
                    return correctingPage ? null : result;
                }),
            ),
    });
    private readonly pageResult = computed(() => (this.pageResource.hasValue() ? this.pageResource.value() : null));
    protected readonly loading = computed(
        () => this.pageResource.isLoading() || (this.pageResource.hasValue() && this.pageResource.value() === null),
    );
    private readonly retainedPage = linkedSignal({
        source: () => ({ result: this.pageResult(), loading: this.loading() }),
        computation: ({ result, loading }, previous): OutgoingEmailPage | null => result ?? (loading ? (previous?.value ?? null) : null),
    });
    private readonly translate = inject(TranslateService);
    private readonly language = toSignal(this.translate.onLangChange);
    protected readonly purposeOptions = computed(() => {
        this.language();
        return [
            { value: '', label: String(this.translate.instant('ADMIN_OUTGOING.ALL')) },
            ...this.purposes.map(value => ({ value, label: String(this.translate.instant(`ADMIN_OUTGOING.PURPOSES.${value}`)) })),
        ];
    });
    protected readonly statusOptions = computed(() => {
        this.language();
        return [
            { value: '', label: String(this.translate.instant('ADMIN_OUTGOING.ALL')) },
            ...this.statuses.map(value => ({ value, label: String(this.translate.instant(`ADMIN_OUTGOING.STATUSES.${value}`)) })),
        ];
    });
    protected readonly messages = computed(() => this.retainedPage()?.items ?? []);
    protected readonly failed = computed(() => this.pageResource.error() !== undefined);
    protected readonly draftPage = linkedSignal(() => this.appliedFilters().page);
    protected readonly total = computed(() => this.retainedPage()?.totalItems ?? 0);
    protected readonly statusCounts = computed(() => this.pageResult()?.statusCounts ?? null);
    protected readonly draftPurpose = linkedSignal(() => this.appliedFilters().purpose);
    protected readonly draftStatus = linkedSignal(() => this.appliedFilters().status);
    protected readonly draftRecipient = linkedSignal(() => this.appliedFilters().recipient);
    protected readonly selected = linkedSignal(() =>
        this.pageRequest()?.hasMessageId === true ? (this.pageResult()?.items[0] ?? null) : null,
    );
    protected readonly purposes = [
        'other',
        'email_verification',
        'account_created',
        'password_reset',
        'bug_report_received',
        'subscription_cancellation',
        'dietologist_invitation',
        'template_test',
    ];
    protected readonly statuses = ['pending', 'processing', 'retry', 'sent', 'failed', 'suppressed'];

    public constructor() {
        connectAdminDevelopmentScreen({
            kind: 'admin-outgoing-emails',
            filterFields: Object.keys(this.queryFilters()),
            read: () => ({
                filters: this.queryFilters(),
                page: this.draftPage(),
                totalItems: this.total(),
                loading: this.loading(),
                error: this.failed(),
                statusCounts: this.statusCounts(),
                items: this.messages().map(message => ({
                    id: message.id,
                    status: message.status,
                    purpose: message.purpose,
                    attemptCount: message.attemptCount,
                    createdAtUtc: message.createdAtUtc,
                    sentAtUtc: message.sentAtUtc,
                    correlationId: message.correlationId,
                })),
            }),
            setFiltersAsync: async params => {
                await this.router.navigate([], { relativeTo: this.route, queryParams: params });
            },
        });
    }

    protected purposeLabelKey(purpose: string): string {
        return `ADMIN_OUTGOING.PURPOSES.${this.purposes.includes(purpose) ? purpose : 'other'}`;
    }

    protected load(reset = false): void {
        if (reset) {
            this.draftPage.set(1);
        }
        const draft = {
            page: this.draftPage(),
            purpose: this.draftPurpose(),
            status: this.draftStatus(),
            recipient: this.draftRecipient(),
        };
        const tree = this.router.createUrlTree([], {
            relativeTo: this.route,
            queryParamsHandling: 'merge',
            queryParams: {
                page: draft.page,
                purpose: adminQueryValue(draft.purpose),
                status: adminQueryValue(draft.status),
                recipient: adminQueryValue(draft.recipient),
                id: reset ? null : (this.id() ?? null),
            },
        });
        if (this.router.serializeUrl(tree) === this.router.url) {
            this.pageRequest.set(this.buildRequest(draft));
        } else {
            void this.router.navigateByUrl(tree);
        }
    }

    private buildRequest({ page, purpose, status, recipient }: OutgoingListFilters): OutgoingPageRequest | undefined {
        const { period, from, to } = this.queryFilters();
        const range = adminPeriod(convertToParamMap({ period, from, to }));
        if (range === null) {
            return undefined;
        }
        const { id, correlationId } = this.appliedFilters();
        return {
            page,
            purpose,
            status,
            filters: {
                recipient,
                ...adminUtcPeriod(range),
                id: id ?? '',
                correlationId,
            },
            hasMessageId: id !== undefined,
        };
    }

    protected goToPage(index: number): void {
        this.draftPage.set(index + 1);
        this.load();
    }

    protected closeDetails(): void {
        this.selected.set(null);
        // A click on the old details can arrive after navigation but before route inputs are updated.
        const committedQuery = this.router.lastSuccessfulNavigation()?.finalUrl?.queryParams;
        if (committedQuery !== undefined && Object.hasOwn(committedQuery, 'id')) {
            void this.router.navigate([], { relativeTo: this.route, queryParamsHandling: 'merge', queryParams: { id: null, page: 1 } });
        }
    }
}
