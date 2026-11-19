import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { FdUiLineChartComponent } from 'fd-ui-kit';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import type { AdminDashboardOverview, DashboardCurrency } from '../models/admin-dashboard-overview.data';
import { AdminDashboardContentComponent } from './admin-dashboard-content';

function revenue(values: Pick<DashboardCurrency, 'currency' | 'gross' | 'net'>): DashboardCurrency {
    return { ...values, refunds: 0, chargebacks: 0, successfulPayments: 1 };
}

const DATA: AdminDashboardOverview = {
    fromUtc: '2026-07-01T00:00:00Z',
    toUtc: '2026-08-01T00:00:00Z',
    interval: 'month',
    period: {
        fromUtc: '2026-07-01T00:00:00Z',
        toUtc: '2026-08-01T00:00:00Z',
        currencies: [revenue({ currency: 'USD', gross: 32, net: 30 }), revenue({ currency: 'EUR', gross: 45, net: 43 })],
        metrics: {
            registrations: 4,
            payingUsers: 2,
            aiTokens: 800,
            trend: [
                { date: '2026-07-01', registrations: 3, aiTokens: 700, revenue: [{ currency: 'EUR', gross: 30 }] },
                { date: '2026-07-02', registrations: 1, aiTokens: 100, revenue: [{ currency: 'USD', gross: 32 }] },
            ],
        },
    },
    previous: {
        fromUtc: '2026-06-01T00:00:00Z',
        toUtc: '2026-07-01T00:00:00Z',
        currencies: [revenue({ currency: 'USD', gross: 22, net: 20 }), revenue({ currency: 'JPY', gross: 15, net: 14 })],
        metrics: { registrations: 6, payingUsers: 1, aiTokens: 1000, trend: [] },
    },
    totalUsersNow: 40,
    premiumUsersNow: 10,
    pendingReportsNow: 2,
};

type ContentFixture = ComponentFixture<AdminDashboardContentComponent>;
const scrollIntoViewDescriptor = Object.getOwnPropertyDescriptor(Element.prototype, 'scrollIntoView');

beforeEach(() => {
    if (scrollIntoViewDescriptor === undefined) {
        Object.defineProperty(Element.prototype, 'scrollIntoView', { configurable: true, value: vi.fn() });
    }
});

afterEach(() => {
    if (scrollIntoViewDescriptor === undefined) {
        Reflect.deleteProperty(Element.prototype, 'scrollIntoView');
    }
});

async function setupAsync(data: AdminDashboardOverview): Promise<ContentFixture> {
    await TestBed.configureTestingModule({
        imports: [AdminDashboardContentComponent],
        providers: [provideRouter([]), provideTranslateTesting()],
    }).compileComponents();
    const fixture = TestBed.createComponent(AdminDashboardContentComponent);
    fixture.componentRef.setInput('data', data);
    fixture.detectChanges();
    return fixture;
}

function host(fixture: ContentFixture): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

function grossCard(fixture: ContentFixture): string {
    return host(fixture).querySelector('.admin-dashboard__metrics fd-ui-card')?.textContent ?? '';
}

function chartPoints(fixture: ContentFixture): Array<ReturnType<FdUiLineChartComponent['points']>> {
    return fixture.debugElement
        .queryAll(By.directive(FdUiLineChartComponent))
        .map(chart => chart.injector.get(FdUiLineChartComponent).points());
}

async function selectCurrencyAsync(fixture: ContentFixture, currency: string): Promise<void> {
    const trigger = host(fixture).querySelector<HTMLButtonElement>('fd-ui-select button');
    if (trigger === null) {
        throw new Error('Expected a dashboard currency selector');
    }
    trigger.click();
    fixture.detectChanges();
    await fixture.whenStable();
    const option = [...document.querySelectorAll<HTMLButtonElement>('[role="option"]')].find(
        candidate => candidate.textContent.trim() === currency,
    );
    if (option === undefined) {
        throw new Error(`Expected dashboard currency option ${currency}`);
    }
    option.click();
    fixture.detectChanges();
}

describe('Admin dashboard displayed period', () => {
    it('renders inclusive date ranges and carries the selected period to each drill-down', async () => {
        const fixture = await setupAsync(DATA);
        const rendered = host(fixture);

        expect(rendered.textContent).toContain('2026-07-01 — 2026-07-31');
        expect(rendered.textContent).toContain('2026-06-01 — 2026-06-30');
        for (const path of ['/billing', '/users', '/ai-usage']) {
            const link = rendered.querySelector<HTMLAnchorElement>(`a[href^="${path}?"]`);
            expect(link?.pathname).toBe(path);
            expect(Object.fromEntries(new URLSearchParams(link?.search))).toEqual({
                period: 'custom',
                from: '2026-07-01',
                to: '2026-07-31',
            });
        }
    });

    it('switches current and comparison revenue together and keeps the other trends', async () => {
        const fixture = await setupAsync(DATA);

        expect(grossCard(fixture)).toContain('45.00 EUR');
        expect(grossCard(fixture)).toContain('0.00 EUR');
        expect(chartPoints(fixture)[0]).toEqual([
            { label: '2026-07', value: 30 },
            { label: '2026-07', value: 0 },
        ]);

        await selectCurrencyAsync(fixture, 'JPY');
        expect(grossCard(fixture)).toContain('0.00 JPY');
        expect(grossCard(fixture)).toContain('15.00 JPY');
        await selectCurrencyAsync(fixture, 'USD');
        expect(grossCard(fixture)).toContain('32.00 USD');
        expect(grossCard(fixture)).toContain('22.00 USD');
        expect(chartPoints(fixture)).toEqual([
            [
                { label: '2026-07', value: 0 },
                { label: '2026-07', value: 32 },
            ],
            [
                { label: '2026-07', value: 3 },
                { label: '2026-07', value: 1 },
            ],
            [
                { label: '2026-07', value: 700 },
                { label: '2026-07', value: 100 },
            ],
        ]);
    });

    it('omits unavailable currency and comparison controls and labels daily trend points by day', async () => {
        const fixture = await setupAsync({
            ...DATA,
            interval: 'day',
            period: { ...DATA.period, currencies: [] },
            previous: null,
        });

        expect(host(fixture).querySelector('fd-ui-select')).toBeNull();
        expect(host(fixture).textContent).not.toContain('ADMIN_OVERVIEW.PREVIOUS');
        expect(grossCard(fixture)).toContain('0.00');
        expect(chartPoints(fixture)[1]).toEqual([
            { label: '07-01', value: 3 },
            { label: '07-02', value: 1 },
        ]);
    });
});
