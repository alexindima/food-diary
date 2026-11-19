import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { AdminAiUsageService } from '../api/admin-ai-usage.service';
import type { AdminAiUsageSummary } from '../models/admin-ai-usage.data';
import { AdminAiUsageComponent } from './admin-ai-usage';

const summary: AdminAiUsageSummary = {
    totalTokens: 100,
    inputTokens: 60,
    outputTokens: 40,
    byDay: [{ date: '2026-01-15', totalTokens: 100, inputTokens: 60, outputTokens: 40 }],
    byOperation: [{ key: 'recipe-analysis', totalTokens: 100, inputTokens: 60, outputTokens: 40 }],
    byModel: [{ key: 'analysis-model', totalTokens: 100, inputTokens: 60, outputTokens: 40 }],
    byUser: [{ id: 'account-1', email: 'member@example.test', totalTokens: 100, inputTokens: 60, outputTokens: 40 }],
};

describe('AdminAiUsageComponent', () => {
    let getSummary: ReturnType<typeof vi.fn<AdminAiUsageService['getSummary']>>;

    beforeEach(async () => {
        getSummary = vi.fn<AdminAiUsageService['getSummary']>().mockReturnValue(of(summary));

        await TestBed.configureTestingModule({
            providers: [
                ...provideTranslateTesting(),
                provideRouter([{ path: 'ai-usage', component: AdminAiUsageComponent }]),
                { provide: AdminAiUsageService, useValue: { getSummary } },
            ],
        }).compileComponents();
    });

    it('loads a filtered account and renders the daily chart and usage breakdowns', async () => {
        const harness = await RouterTestingHarness.create('/ai-usage?userId=account-1&period=custom&from=2026-01-15&to=2026-01-16');

        expect(getSummary).toHaveBeenCalledExactlyOnceWith({ from: '2026-01-15', to: '2026-01-17', userId: 'account-1' });
        expect(harness.routeNativeElement?.querySelector('fd-ui-line-chart')).toBeTruthy();
        expect(harness.routeNativeElement?.textContent).toContain('recipe-analysis');
        expect(harness.routeNativeElement?.textContent).toContain('analysis-model');
        expect(harness.routeNativeElement?.querySelector('a[href^="/users/account-1?"]')?.textContent).toBe('member@example.test');
    });

    it('retries the current period after a request fails', async () => {
        getSummary.mockReturnValueOnce(throwError(() => new Error('ai usage failed')));
        const harness = await RouterTestingHarness.create('/ai-usage?period=custom&from=2026-01-15&to=2026-01-16');

        expect(harness.routeNativeElement?.querySelector('[role="alert"]')).toBeTruthy();
        expect(harness.routeNativeElement?.querySelector('fd-ui-line-chart')).toBeNull();

        harness.routeNativeElement?.querySelector<HTMLButtonElement>(':scope > fd-ui-button button')?.click();
        harness.detectChanges();

        const period = { from: '2026-01-15', to: '2026-01-17' };
        expect(getSummary.mock.calls).toEqual([[period], [period]]);
        expect(harness.routeNativeElement?.querySelector('[role="alert"]')).toBeNull();
        expect(harness.routeNativeElement?.textContent).toContain('analysis-model');
    });

    it('shows a valid empty period with zero totals and empty breakdowns', async () => {
        getSummary.mockReturnValueOnce(
            of({ totalTokens: 0, inputTokens: 0, outputTokens: 0, byDay: [], byOperation: [], byModel: [], byUser: [] }),
        );
        const harness = await RouterTestingHarness.create('/ai-usage?period=today');
        const root = harness.routeNativeElement;
        expect(root?.querySelector('[role="alert"]')).toBeNull();
        expect(Array.from(root?.querySelectorAll('.admin-ai-usage strong') ?? [], element => element.textContent.trim())).toEqual([
            '0',
            '0',
            '0',
        ]);
        expect(Array.from(root?.querySelectorAll('tbody td') ?? [], cell => cell.textContent.trim())).toEqual([
            'ADMIN_COMMON.EMPTY',
            'ADMIN_COMMON.EMPTY',
            'ADMIN_COMMON.EMPTY',
        ]);
    });

    it('clears stale usage and ignores a late response after switching to an invalid period', async () => {
        const obsolete = new Subject<AdminAiUsageSummary>();
        getSummary.mockReturnValueOnce(of(summary)).mockReturnValueOnce(obsolete);
        const harness = await RouterTestingHarness.create('/ai-usage');
        expect(harness.routeNativeElement?.textContent).toContain('analysis-model');

        await harness.navigateByUrl('/ai-usage?userId=account-1', AdminAiUsageComponent);
        expect(harness.routeNativeElement?.querySelector('[role="status"]')).toBeTruthy();

        await harness.navigateByUrl('/ai-usage?period=custom&from=2026-01-16&to=2026-01-15', AdminAiUsageComponent);
        obsolete.next(summary);
        harness.detectChanges();

        expect(getSummary).toHaveBeenCalledTimes(2);
        expect(harness.routeNativeElement?.querySelector('[role="status"]')).toBeNull();
        expect(harness.routeNativeElement?.querySelector('[role="alert"]')?.textContent).toContain('ADMIN_COMMON.INVALID_PERIOD');
        expect(harness.routeNativeElement?.querySelector('fd-ui-line-chart')).toBeNull();
    });

    it('keeps current usage visible when an older account request fails', async () => {
        const obsolete = new Subject<AdminAiUsageSummary>();
        getSummary.mockReturnValueOnce(obsolete);
        const harness = await RouterTestingHarness.create('/ai-usage?userId=older-account');

        await harness.navigateByUrl('/ai-usage?userId=account-1', AdminAiUsageComponent);
        obsolete.error(new Error('older account failed'));
        harness.detectChanges();

        expect(harness.routeNativeElement?.querySelector('[role="alert"]')).toBeNull();
        expect(harness.routeNativeElement?.textContent).toContain('analysis-model');
    });
});
