import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, linkedSignal, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiInputComponent, FdUiSelectComponent } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { map } from 'rxjs';

import { AdminLoadErrorComponent } from '../../../shared/feedback/admin-load-error';
import { restoreAdminPage } from '../../../shared/period/admin-pagination';
import { adminPeriod, adminUtcPeriod } from '../../../shared/period/admin-period';
import { AdminPeriodControlComponent } from '../../../shared/period/admin-period-control';
import { ADMIN_DATE_TEXT_LENGTH, adminPage, firstAdminQueryValue } from '../../../shared/period/admin-query';
import { AdminAcquisitionComparisonComponent } from '../components/admin-acquisition-comparison';
import { AdminAcquisitionLinkPreviewComponent } from '../components/admin-acquisition-link-preview';
import { AdminAcquisitionFacade } from '../lib/admin-acquisition.facade';
import type { MarketingAttributionBreakdown, MarketingAttributionRecentEvent } from '../models/admin-acquisition.data';

const PERCENT_SCALE = 100;
const HOURS_PER_DAY = 24;
const MAX_LANDING_PATH_LENGTH = 512;

type AttributionEventFilter = 'all' | 'page_landing' | 'signup_completed' | 'premium_started';
type AttributionChannelFilter = 'all' | 'tracked' | 'direct';
type AcquisitionReportQuery = Record<string, string | number> & { page: number; limit: number };
type AcquisitionEventFilters = { search: string; eventType: AttributionEventFilter; channel: AttributionChannelFilter };

type CampaignUrlBuilderModel = {
    baseUrl: string;
    source: string;
    medium: string;
    campaign: string;
    content: string;
    term: string;
};

@Component({
    selector: 'fd-admin-acquisition',
    imports: [
        FdUiSelectComponent,
        FdUiInputComponent,
        AdminPeriodControlComponent,
        AdminLoadErrorComponent,
        AdminAcquisitionComparisonComponent,
        AdminAcquisitionLinkPreviewComponent,
        TranslatePipe,
        CommonModule,
        FdUiButtonComponent,
        FdUiCardComponent,
    ],
    templateUrl: './admin-acquisition.html',
    styleUrl: './admin-acquisition.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminAcquisitionComponent {
    public readonly page = input(undefined, { transform: firstAdminQueryValue });
    public readonly search = input(undefined, { transform: firstAdminQueryValue });
    public readonly eventType = input(undefined, { transform: firstAdminQueryValue });
    public readonly channel = input(undefined, { transform: firstAdminQueryValue });
    public readonly period = input(undefined, { transform: firstAdminQueryValue });
    public readonly from = input(undefined, { transform: firstAdminQueryValue });
    public readonly to = input(undefined, { transform: firstAdminQueryValue });

    private readonly acquisitionFacade = inject(AdminAcquisitionFacade);
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly appliedQuery = computed(() => {
        const eventType = this.eventType() ?? 'all';
        const channel = this.channel() ?? 'all';
        const filters: AcquisitionEventFilters = {
            search: this.search() ?? '',
            eventType: this.isEventFilter(eventType) ? eventType : 'all',
            channel: this.isChannelFilter(channel) ? channel : 'all',
        };
        return {
            page: adminPage(this.page() ?? null),
            filters,
            periodParams: convertToParamMap({ period: this.period() ?? null, from: this.from() ?? null, to: this.to() ?? null }),
        };
    });
    protected readonly eventPageSize = 50;
    private readonly requestedReport = linkedSignal(() => this.buildRequest());
    private readonly reportResource = rxResource({
        params: this.requestedReport,
        stream: ({ params }) =>
            this.acquisitionFacade.getRange(params).pipe(
                map(report => {
                    if (this.requestedReport() !== params) {
                        return null;
                    }
                    const correctingPage = restoreAdminPage(this.router, this.route, params.page, {
                        totalItems: report.eventTotal,
                        pageSize: params.limit,
                    });
                    return correctingPage ? null : report;
                }),
            ),
    });

    protected readonly report = computed(() => (this.reportResource.hasValue() ? this.reportResource.value() : null));
    protected readonly summary = computed(() => this.report()?.current ?? null);
    // Keep the loading state while the URL switches to the corrected page.
    protected readonly isLoading = computed(
        () => this.reportResource.isLoading() || (this.reportResource.hasValue() && this.reportResource.value() === null),
    );
    protected readonly failed = computed(() => this.reportResource.error() !== undefined);
    protected readonly currentPage = computed(() => this.appliedQuery().page);
    protected readonly eventSearch = linkedSignal(() => this.appliedQuery().filters.search);
    protected readonly eventSearchMaxLength = 320;
    protected readonly utmMaxLength = 160;
    protected readonly eventTypeFilter = linkedSignal(() => this.appliedQuery().filters.eventType);
    protected readonly channelFilter = linkedSignal(() => this.appliedQuery().filters.channel);
    protected readonly builderModel = signal<CampaignUrlBuilderModel>({
        baseUrl: 'https://fooddiary.club/',
        source: 'telegram',
        medium: 'social',
        campaign: '2026_07_launch',
        content: 'creative_a',
        term: '',
    });
    protected readonly builderValidation = computed(() => {
        const model = this.builderModel();
        const values = [model.source, model.medium, model.campaign, model.content, model.term];
        if (values.some(value => value.trim().length > this.utmMaxLength)) {
            return { key: 'ADMIN_ACQUISITION.UTM_LIMIT', max: this.utmMaxLength };
        }
        const url = new URL(this.buildCampaignUrl());
        return url.pathname.length + url.search.length > MAX_LANDING_PATH_LENGTH
            ? { key: 'ADMIN_ACQUISITION.LINK_PATH_LIMIT', max: MAX_LANDING_PATH_LENGTH }
            : null;
    });
    protected readonly attributionRate = computed(() => {
        const data = this.summary();
        if (data === null || data.visits === 0) {
            return '0';
        }

        return ((data.attributedVisits / data.visits) * PERCENT_SCALE).toFixed(1);
    });
    protected loadSummary(): void {
        this.requestedReport.set(this.buildRequest());
    }

    private buildRequest(): AcquisitionReportQuery | undefined {
        const { filters, page, periodParams } = this.appliedQuery();
        const range = adminPeriod(periodParams, '30d');
        if (range === null) {
            return undefined;
        }
        const dates = adminUtcPeriod({
            from: range.from ?? '1970-01-01',
            to: range.to ?? new Date().toISOString().slice(0, ADMIN_DATE_TEXT_LENGTH),
        });
        return {
            ...dates,
            page,
            limit: this.eventPageSize,
            eventType: filters.eventType === 'all' ? '' : filters.eventType,
            channel: filters.channel === 'all' ? '' : filters.channel,
            search: filters.search,
        };
    }

    protected formatAttribution(event: MarketingAttributionRecentEvent): string {
        const source = event.utmSource ?? event.referrerHost ?? 'direct';
        const medium = event.utmMedium ?? (event.referrerHost === null ? 'none' : 'referral');
        const campaign = event.utmCampaign ?? 'none';
        return `${source} / ${medium} / ${campaign}`;
    }

    protected setEventTypeFilter(value: string | null): void {
        if (value !== null && this.isEventFilter(value)) {
            this.eventTypeFilter.set(value);
            this.applyEventFilters();
        }
    }

    protected setChannelFilter(value: string | null): void {
        if (value !== null && this.isChannelFilter(value)) {
            this.channelFilter.set(value);
            this.applyEventFilters();
        }
    }

    protected applyEventFilters(): void {
        void this.router.navigate([], {
            relativeTo: this.route,
            queryParamsHandling: 'merge',
            queryParams: { eventType: this.eventTypeFilter(), channel: this.channelFilter(), search: this.eventSearch(), page: 1 },
        });
    }

    protected goToPage(page: number): void {
        void this.router.navigate([], { relativeTo: this.route, queryParamsHandling: 'merge', queryParams: { page } });
    }

    protected setEventSearch(value: string | number | null): void {
        this.eventSearch.set(value?.toString() ?? '');
    }

    protected formatEventType(value: string): string {
        switch (value) {
            case 'page_landing': {
                return 'Visit';
            }
            case 'signup_completed': {
                return 'Signup';
            }
            case 'premium_started': {
                return 'Premium';
            }
            default: {
                return value;
            }
        }
    }

    protected formatWindow(hours: number): string {
        if (hours < HOURS_PER_DAY) {
            return `${hours} hours`;
        }

        const days = hours / HOURS_PER_DAY;
        return days === 1 ? '24 hours' : `${days} days`;
    }

    protected formatRate(value: number): string {
        return value.toFixed(1);
    }

    protected campaignUrl(): string {
        return this.buildCampaignUrl();
    }

    protected updateBuilderField(field: keyof CampaignUrlBuilderModel, value: string | number | null): void {
        this.builderModel.update(model => ({ ...model, [field]: value?.toString() ?? '' }));
    }

    protected formatBreakdownLabel(item: MarketingAttributionBreakdown): string {
        return item.campaign === 'all' ? `${item.source} / ${item.medium}` : `${item.source} / ${item.medium} / ${item.campaign}`;
    }

    protected formatRelativeDate(value: string | null): string {
        if (value === null) {
            return '-';
        }

        return new Intl.DateTimeFormat(undefined, {
            month: 'short',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit',
        }).format(new Date(value));
    }

    private buildCampaignUrl(): string {
        const model = this.builderModel();
        const rawBaseUrl = model.baseUrl.trim().length > 0 ? model.baseUrl.trim() : 'https://fooddiary.club/';
        let url: URL;
        try {
            url = new URL(rawBaseUrl, 'https://fooddiary.club/');
        } catch {
            url = new URL('https://fooddiary.club/');
        }

        this.setParam(url, 'utm_source', model.source);
        this.setParam(url, 'utm_medium', model.medium);
        this.setParam(url, 'utm_campaign', model.campaign);
        this.setParam(url, 'utm_content', model.content);
        this.setParam(url, 'utm_term', model.term);
        url.search = url.search.replaceAll('+', '%20');
        return url.toString();
    }

    private setParam(url: URL, key: string, value: string): void {
        const normalized = value.trim();
        if (normalized.length === 0) {
            url.searchParams.delete(key);
            return;
        }

        url.searchParams.set(key, normalized);
    }

    private isEventFilter(value: string): value is AttributionEventFilter {
        return value === 'all' || value === 'page_landing' || value === 'signup_completed' || value === 'premium_started';
    }

    private isChannelFilter(value: string): value is AttributionChannelFilter {
        return value === 'all' || value === 'tracked' || value === 'direct';
    }
}
