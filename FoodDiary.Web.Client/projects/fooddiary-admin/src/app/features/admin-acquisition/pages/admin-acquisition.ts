import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';

import { AdminAcquisitionFacade, DEFAULT_ADMIN_ACQUISITION_WINDOW_HOURS } from '../lib/admin-acquisition.facade';
import type {
    MarketingAttributionBreakdown,
    MarketingAttributionRecentEvent,
    MarketingAttributionSummary,
} from '../models/admin-acquisition.data';

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
    imports: [CommonModule, FdUiButtonComponent, FdUiCardComponent],
    templateUrl: './admin-acquisition.html',
    styleUrl: './admin-acquisition.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminAcquisitionComponent {
    private readonly acquisitionFacade = inject(AdminAcquisitionFacade);
    private readonly destroyRef = inject(DestroyRef);

    protected readonly summary = signal<MarketingAttributionSummary | null>(null);
    protected readonly isLoading = signal(false);
    protected readonly selectedWindowHours = signal(DEFAULT_ADMIN_ACQUISITION_WINDOW_HOURS);
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
    protected readonly filteredRecentEvents = computed(() => {
        const data = this.summary();
        if (data === null) {
            return [];
        }

        const eventType = this.eventTypeFilter();
        const channel = this.channelFilter();
        return data.recentEvents.filter(event => {
            const matchesType = eventType === 'all' || event.eventType === eventType;
            const isTracked = this.isTrackedEvent(event);
            const matchesChannel = channel === 'all' || (channel === 'tracked' ? isTracked : !isTracked);
            return matchesType && matchesChannel;
        });
    });

    public constructor() {
        this.loadSummary();
    }

    protected loadSummary(): void {
        this.isLoading.set(true);
        this.acquisitionFacade
            .getSummary(this.selectedWindowHours())
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: response => {
                    this.summary.set(response);
                    this.isLoading.set(false);
                },
                error: () => {
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

    protected setWindow(event: Event): void {
        const value = Number(this.getSelectValue(event));
        if (!Number.isFinite(value) || value < 1) {
            return;
        }

        this.selectedWindowHours.set(value);
        this.loadSummary();
    }

    protected setEventTypeFilter(event: Event): void {
        const value = this.getSelectValue(event);
        if (this.isEventFilter(value)) {
            this.eventTypeFilter.set(value);
        }
    }

    protected setChannelFilter(event: Event): void {
        const value = this.getSelectValue(event);
        if (this.isChannelFilter(value)) {
            this.channelFilter.set(value);
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
