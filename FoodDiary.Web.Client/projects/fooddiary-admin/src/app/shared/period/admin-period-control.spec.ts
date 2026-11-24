import { Component, input } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter, Router, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { FdUiDateInputComponent, FdUiSelectComponent } from 'fd-ui-kit';
import { describe, expect, it } from 'vitest';

import { stubScrollIntoView } from '../../../../../../src/testing/dom-testing';
import { provideTranslateTesting } from '../../../../../../src/testing/translate-testing.module';
import { AdminPeriodControlComponent } from './admin-period-control';
import { firstAdminQueryValue } from './admin-query';

@Component({
    imports: [AdminPeriodControlComponent],
    template: `
        <fd-admin-period
            [queryPrefix]="prefix() ?? ''"
            [defaultPeriod]="defaultPeriod() ?? 'all'"
            [period]="prefix() === 'activity_' ? activity_period() : period()"
            [periodFrom]="prefix() === 'activity_' ? activity_from() : from()"
            [periodTo]="prefix() === 'activity_' ? activity_to() : to()"
        />
    `,
})
class PeriodHost {
    public readonly prefix = input<string>();
    public readonly defaultPeriod = input<string>();
    public readonly period = input(undefined, { transform: firstAdminQueryValue });
    public readonly from = input(undefined, { transform: firstAdminQueryValue });
    public readonly to = input(undefined, { transform: firstAdminQueryValue });
    public readonly activity_period = input(undefined, { transform: firstAdminQueryValue });
    public readonly activity_from = input(undefined, { transform: firstAdminQueryValue });
    public readonly activity_to = input(undefined, { transform: firstAdminQueryValue });
}

type PeriodFixture = RouterTestingHarness['fixture'];

async function openPeriodAsync(url: string, prefix = '', defaultPeriod = 'all'): Promise<PeriodFixture> {
    stubScrollIntoView();
    await TestBed.configureTestingModule({
        providers: [
            provideRouter(
                [{ path: '', component: PeriodHost, data: { prefix, defaultPeriod } }],
                withComponentInputBinding({ unmatchedInputBehavior: 'undefinedIfStale' }),
            ),
            ...provideTranslateTesting(),
        ],
    }).compileComponents();
    const { fixture } = await RouterTestingHarness.create(url);
    fixture.detectChanges();
    return fixture;
}

async function choosePresetAsync(fixture: PeriodFixture, key: string): Promise<void> {
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

it('adapts date model values to calendar dates and clears them when the period model is reset', async () => {
    const fixture = await openPeriodAsync('/?period=custom&from=2024-02-28&to=2024-03-03&page=4');
    const router = TestBed.inject(Router);
    const host = fixture.nativeElement as HTMLElement;
    const dateControls = fixture.debugElement.queryAll(By.directive(FdUiDateInputComponent));
    const apply = host.querySelector<HTMLButtonElement>('fd-ui-button button');
    if (apply === null || dateControls.length !== 2) {
        throw new Error('Missing custom period controls');
    }
    const [start, end] = dateControls.map(node => node.injector.get(FdUiDateInputComponent));
    start.value.set(new Date('2024-02-29T23:50:00'));
    end.value.set(new Date('2024-03-01T00:10:00'));
    fixture.detectChanges();
    apply.click();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(router.parseUrl(router.url).queryParams).toEqual({ period: 'custom', from: '2024-02-29', to: '2024-03-01' });

    const preset = fixture.debugElement.query(By.directive(FdUiSelectComponent)).injector.get(FdUiSelectComponent);
    preset.value.set(null);
    await fixture.whenStable();
    fixture.detectChanges();
    expect(router.parseUrl(router.url).queryParams).toEqual({ period: 'all' });
    expect(host.querySelector('fd-ui-date-input')).toBeNull();
});
