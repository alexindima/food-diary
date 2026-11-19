import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { describe, expect, it } from 'vitest';

import { stubScrollIntoView } from '../../../../../../src/testing/dom-testing';
import { provideTranslateTesting } from '../../../../../../src/testing/translate-testing.module';
import { AdminPeriodControlComponent } from './admin-period-control';

async function openPeriodAsync(url: string, prefix = '', defaultPeriod = 'all'): Promise<ComponentFixture<AdminPeriodControlComponent>> {
    stubScrollIntoView();
    await TestBed.configureTestingModule({
        imports: [AdminPeriodControlComponent],
        providers: [provideRouter([]), ...provideTranslateTesting()],
    }).compileComponents();
    await TestBed.inject(Router).navigateByUrl(url);
    const fixture = TestBed.createComponent(AdminPeriodControlComponent);
    fixture.componentRef.setInput('queryPrefix', prefix);
    fixture.componentRef.setInput('defaultPeriod', defaultPeriod);
    fixture.detectChanges();
    return fixture;
}

async function choosePresetAsync(fixture: ComponentFixture<AdminPeriodControlComponent>, key: string): Promise<void> {
    const host = fixture.nativeElement as HTMLElement;
    const trigger = host.querySelector<HTMLButtonElement>('fd-ui-select button');
    trigger?.click();
    fixture.detectChanges();
    const option = Array.from(document.querySelectorAll<HTMLElement>('[role="option"]')).find(
        element => element.textContent.trim() === `ADMIN_COMMON.PERIOD_${key}`,
    );
    if (option === undefined) {
        throw new Error(`Missing period option ${key}`);
    }
    option.click();
    await fixture.whenStable();
    fixture.detectChanges();
}

describe('AdminPeriodControlComponent independent filters', () => {
    it.each(['', 'activity_'])('updates only its own period with prefix %s', async prefix => {
        const fixture = await openPeriodAsync('/?period=90d&activity_period=30d', prefix);
        const router = TestBed.inject(Router);
        await choosePresetAsync(fixture, '7d');
        const params = router.parseUrl(router.url).queryParams;
        expect(params[`${prefix}period`]).toBe('7d');
        expect(params[prefix === '' ? 'activity_period' : 'period']).toBe(prefix === '' ? '30d' : '90d');
    });
});

it('shows an invalid period instead of crashing on an unsupported preset from the URL', async () => {
    const fixture = await openPeriodAsync('/?period=constructor&page=4&search=kept');
    const router = TestBed.inject(Router);
    const host = fixture.nativeElement as HTMLElement;
    expect(host.querySelector('[role="alert"]')?.textContent).toContain('ADMIN_COMMON.INVALID_PERIOD');
    await choosePresetAsync(fixture, 'today');
    expect(host.querySelector('[role="alert"]')).toBeNull();
    expect(router.parseUrl(router.url).queryParams).toEqual({ period: 'today', search: 'kept' });
});

it('shows the scoped default without changing the URL and rejects an incomplete custom range', async () => {
    const url = '/?period=7d&search=kept';
    const fixture = await openPeriodAsync(url, 'activity_', '30d');
    const host = fixture.nativeElement as HTMLElement;
    const router = TestBed.inject(Router);
    expect(host.querySelector('fd-ui-select button')?.textContent).toContain('ADMIN_COMMON.PERIOD_30d');
    expect(host.querySelector('fd-ui-date-input')).toBeNull();
    expect(host.querySelector('[role="alert"]')).toBeNull();
    expect(router.url).toBe(url);

    await router.navigateByUrl(`${url}&activity_period=custom&activity_from=2024-02-29`);
    fixture.detectChanges();
    expect(host.querySelector('fd-ui-select button')?.textContent).toContain('ADMIN_COMMON.PERIOD_custom');
    expect(host.querySelectorAll('fd-ui-date-input')).toHaveLength(2);
    expect(host.querySelector('[role="alert"]')?.textContent).toContain('ADMIN_COMMON.INVALID_PERIOD');
});

it('applies a selected leap day to the scoped period and leaves the other filter intact', async () => {
    const originalUrl = '/?period=90d&activity_period=custom&activity_from=2024-02-28&activity_to=2024-03-01&page=4';
    const fixture = await openPeriodAsync(originalUrl, 'activity_');
    const router = TestBed.inject(Router);
    const host = fixture.nativeElement as HTMLElement;
    const start = host.querySelector<HTMLButtonElement>('fd-ui-date-input button[aria-haspopup="dialog"]');
    const apply = host.querySelector<HTMLButtonElement>('fd-ui-button button');
    if (start === null || apply === null) {
        throw new Error('Missing custom period input');
    }
    start.click();
    fixture.detectChanges();
    const leapDay = document.querySelector<HTMLButtonElement>('[role="gridcell"][data-date="2024-02-29"]');
    if (leapDay === null) {
        throw new Error('Missing leap day in the calendar');
    }
    leapDay.click();
    expect(router.url).toBe(originalUrl);
    apply.click();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(host.querySelector('[role="alert"]')).toBeNull();
    expect(router.parseUrl(router.url).queryParams).toEqual({
        period: '90d',
        activity_period: 'custom',
        activity_from: '2024-02-29',
        activity_to: '2024-03-01',
    });
    await choosePresetAsync(fixture, '7d');
    expect(router.parseUrl(router.url).queryParams).toEqual({ period: '90d', activity_period: '7d' });
    await choosePresetAsync(fixture, 'custom');
    expect(host.querySelectorAll('fd-ui-date-input')).toHaveLength(2);
    expect(router.parseUrl(router.url).queryParams).toEqual({ period: '90d', activity_period: '7d' });
});
