import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import type { Subscription } from 'rxjs';

import { AdminLoadErrorComponent } from '../../../shared/feedback/admin-load-error';
import { adminPeriod, adminUtcPeriod } from '../../../shared/period/admin-period';
import { AdminPeriodControlComponent } from '../../../shared/period/admin-period-control';
import { ADMIN_DATE_TEXT_LENGTH, adminPage } from '../../../shared/period/admin-query';
import { AdminAcquisitionComparisonComponent } from '../components/admin-acquisition-comparison';
import { AdminAcquisitionFacade } from '../lib/admin-acquisition.facade';
import type {
    MarketingAttributionBreakdown,
    MarketingAttributionRecentEvent,
    MarketingAttributionSummary,
} from '../models/admin-acquisition.data';
import type { MarketingAttributionRange } from '../models/admin-acquisition-range';

const PERCENT_SCALE = 100;
const HOURS_PER_DAY = 24;

type AttributionEventFilter = 'all' | 'page_landing' | 'signup_completed' | 'premium_started';
type AttributionChannelFilter = 'all' | 'tracked' | 'direct';

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
        AdminPeriodControlComponent,
        AdminLoadErrorComponent,
        AdminAcquisitionComparisonComponent,
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
    private readonly acquisitionFacade = inject(AdminAcquisitionFacade);
    private readonly destroyRef = inject(DestroyRef);

    protected readonly summary = signal<MarketingAttributionSummary | null>(null);
    protected readonly isLoading = signal(false);
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private request?: Subscription;
    protected readonly failed = signal(false);
    protected readonly report = signal<MarketingAttributionRange | null>(null);
    protected readonly page = signal(1);
    protected readonly eventSearch = signal('');
    protected readonly eventPageSize = 50;
    protected readonly eventTypeFilter = signal<AttributionEventFilter>('all');
    protected readonly channelFilter = signal<AttributionChannelFilter>('all');
    protected readonly builderModel = signal<CampaignUrlBuilderModel>({
        baseUrl: 'https://fooddiary.club/',
        source: 'telegram',
        medium: 'social',
        campaign: '2026_07_launch',
        content: 'creative_a',
        term: '',
    });
    protected readonly attributionRate = computed(() => {
        const data = this.summary();
        if (data === null || data.visits === 0) {
            return '0';
        }

        return ((data.attributedVisits / data.visits) * PERCENT_SCALE).toFixed(1);
    });
    protected readonly filteredRecentEvents = computed(() => this.summary()?.recentEvents ?? []);

    public constructor() {
        this.route.queryParamMap.pipe(takeUntilDestroyed()).subscribe(params => {
            const eventType = params.get('eventType') ?? 'all';
            const channel = params.get('channel') ?? 'all';
            this.eventTypeFilter.set(this.isEventFilter(eventType) ? eventType : 'all');
            this.channelFilter.set(this.isChannelFilter(channel) ? channel : 'all');
            this.eventSearch.set(params.get('search') ?? '');
            this.page.set(adminPage(params.get('page')));
            this.loadSummary();
        });
    }

    protected loadSummary(): void {
        this.request?.unsubscribe();
        this.failed.set(false);
        this.summary.set(null);
        this.report.set(null);
        const range = adminPeriod(this.route.snapshot.queryParamMap, '30d');
        if (range === null) {
            this.isLoading.set(false);
            return;
        }
        const dates = adminUtcPeriod({
            from: range.from ?? '1970-01-01',
            to: range.to ?? new Date().toISOString().slice(0, ADMIN_DATE_TEXT_LENGTH),
        });
        this.isLoading.set(true);
        this.request = this.acquisitionFacade
            .getRange({
                ...dates,
                page: this.page(),
                limit: this.eventPageSize,
                eventType: this.eventTypeFilter() === 'all' ? '' : this.eventTypeFilter(),
                channel: this.channelFilter() === 'all' ? '' : this.channelFilter(),
                search: this.eventSearch(),
            })
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: response => {
                    this.summary.set(response.current);
                    this.report.set(response);
                    this.isLoading.set(false);
                },
                error: () => {
                    this.failed.set(true);
                    this.summary.set(null);
                    this.isLoading.set(false);
                },
            });
    }

    protected formatAttribution(event: MarketingAttributionRecentEvent): string {
        const source = event.utmSource ?? event.referrerHost ?? 'direct';
        const medium = event.utmMedium ?? (event.referrerHost === null ? 'none' : 'referral');
        const campaign = event.utmCampaign ?? 'none';
        return `${source} / ${medium} / ${campaign}`;
    }

    protected setEventTypeFilter(event: Event): void {
        const value = this.getSelectValue(event);
        if (this.isEventFilter(value)) {
            this.eventTypeFilter.set(value);
            this.applyEventFilters();
        }
    }

    protected setChannelFilter(event: Event): void {
        const value = this.getSelectValue(event);
        if (this.isChannelFilter(value)) {
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

    protected setEventSearch(event: Event): void {
        const target = event.target;
        if (target !== null && 'value' in target && typeof target.value === 'string') {
            this.eventSearch.set(target.value);
        }
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

    protected getSelectValue(event: Event): string {
        const target = event.currentTarget;
        return target instanceof HTMLSelectElement ? target.value : '';
    }

    protected formatRate(value: number): string {
        return value.toFixed(1);
    }

    protected campaignUrl(): string {
        return this.buildCampaignUrl();
    }

    protected updateBuilderField(field: keyof CampaignUrlBuilderModel, event: Event): void {
        const target = event.currentTarget;
        if (!(target instanceof HTMLInputElement)) {
            return;
        }

        this.builderModel.update(model => ({
            ...model,
            [field]: target.value,
        }));
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

    protected isTrackedEvent(event: MarketingAttributionRecentEvent): boolean {
        return (
            event.referrerHost !== null ||
            event.utmSource !== null ||
            event.utmMedium !== null ||
            event.utmCampaign !== null ||
            event.utmContent !== null ||
            event.utmTerm !== null
        );
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
