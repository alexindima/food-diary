import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';

import { AdminAcquisitionFacade } from '../lib/admin-acquisition.facade';
import type {
    MarketingAttributionBreakdown,
    MarketingAttributionRecentEvent,
    MarketingAttributionSummary,
} from '../models/admin-acquisition.data';

const PERCENT_SCALE = 100;

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
    imports: [CommonModule, FdUiCardComponent],
    templateUrl: './admin-acquisition.html',
    styleUrl: './admin-acquisition.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminAcquisitionComponent {
    private readonly acquisitionFacade = inject(AdminAcquisitionFacade);
    private readonly destroyRef = inject(DestroyRef);

    protected readonly summary = signal<MarketingAttributionSummary | null>(null);
    protected readonly isLoading = signal(false);
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
        if (data === null || data.events === 0) {
            return '0';
        }

        return ((data.attributedEvents / data.events) * PERCENT_SCALE).toFixed(1);
    });

    public constructor() {
        this.loadSummary();
    }

    protected loadSummary(): void {
        this.isLoading.set(true);
        this.acquisitionFacade
            .getSummary()
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
}
