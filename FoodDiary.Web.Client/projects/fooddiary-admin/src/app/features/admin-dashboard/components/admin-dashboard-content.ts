import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent, FdUiLineChartComponent, FdUiSelectComponent } from 'fd-ui-kit';

import type { AdminDashboardOverview } from '../models/admin-dashboard-overview.data';

const DAY_MS = 86_400_000;
const DATE_LENGTH = 10;
const MONTH_LENGTH = 7;
const DATE_MONTH_START = 5;
@Component({
    selector: 'fd-admin-dashboard-content',
    imports: [CommonModule, RouterLink, TranslatePipe, FdUiCardComponent, FdUiLineChartComponent, FdUiSelectComponent],
    templateUrl: './admin-dashboard-content.html',
    styleUrl: '../pages/admin-dashboard.scss',
    host: { class: 'fd-grid fd-gap-md' },
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminDashboardContentComponent {
    public readonly data = input.required<AdminDashboardOverview>();
    protected readonly periodParams = computed(() => ({
        period: 'custom',
        from: this.data().fromUtc.slice(0, DATE_LENGTH),
        to: this.lastDate(this.data().toUtc),
    }));
    protected readonly currency = signal<string | null>(null);
    protected readonly currencyOptions = computed(() =>
        [...new Set([...this.data().period.currencies, ...(this.data().previous?.currencies ?? [])].map(item => item.currency))]
            .sort()
            .map(value => ({ value, label: value })),
    );
    protected readonly selectedCurrency = computed(() =>
        this.currencyOptions().some(item => item.value === this.currency()) ? this.currency() : (this.currencyOptions()[0]?.value ?? null),
    );
    protected readonly cards = computed(() => {
        const data = this.data();
        const revenue = data.period.currencies.find(item => item.currency === this.selectedCurrency()) ?? { gross: 0, net: 0 };
        const previous = data.previous?.currencies.find(item => item.currency === this.selectedCurrency()) ?? { gross: 0, net: 0 };
        const earlier = data.previous?.metrics ?? { registrations: 0, payingUsers: 0, aiTokens: 0 };
        return [
            {
                key: 'GROSS',
                value: revenue.gross,
                previous: previous.gross,
                currency: this.selectedCurrency(),
                digits: '1.2-2',
                hint: 'ADMIN_OVERVIEW.GROSS_HINT',
            },
            {
                key: 'NET',
                value: revenue.net,
                previous: previous.net,
                currency: this.selectedCurrency(),
                digits: '1.2-2',
                hint: 'ADMIN_OVERVIEW.NET_HINT',
            },
            {
                key: 'REGISTRATIONS',
                value: data.period.metrics.registrations,
                previous: earlier.registrations,
                currency: '',
                digits: '1.0-0',
                hint: 'ADMIN_OVERVIEW.REGISTRATIONS_HINT',
            },
            {
                key: 'PAYING_USERS',
                value: data.period.metrics.payingUsers,
                previous: earlier.payingUsers,
                currency: '',
                digits: '1.0-0',
                hint: 'ADMIN_OVERVIEW.PAYING_HINT',
            },
            {
                key: 'AI_TOKENS',
                value: data.period.metrics.aiTokens,
                previous: earlier.aiTokens,
                currency: '',
                digits: '1.0-0',
                hint: 'ADMIN_OVERVIEW.AI_HINT',
            },
        ];
    });
    protected readonly revenuePoints = computed(() => this.points('revenue'));
    protected readonly registrationPoints = computed(() => this.points('registrations'));
    protected readonly aiPoints = computed(() => this.points('aiTokens'));

    protected lastDate(exclusiveEnd: string): string {
        return new Date(Date.parse(exclusiveEnd) - DAY_MS).toISOString().slice(0, DATE_LENGTH);
    }

    private points(metric: 'revenue' | 'registrations' | 'aiTokens'): Array<{ label: string; value: number }> {
        return this.data().period.metrics.trend.map(point => ({
            label: this.data().interval === 'month' ? point.date.slice(0, MONTH_LENGTH) : point.date.slice(DATE_MONTH_START, DATE_LENGTH),
            value:
                metric === 'revenue' ? (point.revenue.find(item => item.currency === this.selectedCurrency())?.gross ?? 0) : point[metric],
        }));
    }
}
