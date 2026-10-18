import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom, of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import ruCore from '../../../../../../../assets/i18n/ru/core.json';
import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { AdminAcquisitionFacade } from '../lib/admin-acquisition.facade';
import type { MarketingAttributionSummary } from '../models/admin-acquisition.data';
import type { MarketingAttributionRange } from '../models/admin-acquisition-range';
import { AdminAcquisitionComponent } from './admin-acquisition';

const UTM_LIMIT = 160;
const PATH_LIMIT = 512;
const HOUR_MS = 3_600_000;
const UTM_FIELDS = ['source', 'medium', 'campaign', 'content', 'term'];

describe('AdminAcquisition generated-link ingestion boundaries', () => {
    let fixture: ComponentFixture<AdminAcquisitionComponent>;
    const getRange = vi.fn((params: Record<string, string | number>) => of(emptyRange(params)));
    beforeEach(async () => {
        getRange.mockClear();
        await TestBed.configureTestingModule({
            imports: [AdminAcquisitionComponent],
            providers: [provideRouter([]), ...provideTranslateTesting(), { provide: AdminAcquisitionFacade, useValue: { getRange } }],
        }).compileComponents();
        fixture = TestBed.createComponent(AdminAcquisitionComponent);
        fixture.detectChanges();
        await fixture.whenStable();
        fixture.detectChanges();
    });

    it('limits every native UTM input to160characters', () => {
        for (const field of UTM_FIELDS) {
            expect(
                (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>(`input#admin-acquisition-${field}`)?.maxLength,
            ).toBe(UTM_LIMIT);
        }
    });
    it('keeps a160character source usable without issuing report requests while editing', () => {
        fillBuilder(fixture, { source: 's'.repeat(UTM_LIMIT), medium: '', campaign: '', content: '', term: '' });
        expect(generatedUrl(fixture).searchParams.get('utm_source')).toHaveLength(UTM_LIMIT);
        expect(builderAlert(fixture)).toBeNull();
        expect(getRange).toHaveBeenCalledTimes(1);
    });
    it('withholds a link when restored draft data exceeds the field boundary', () => {
        fillBuilder(fixture, { source: 's'.repeat(UTM_LIMIT + 1) });
        expect(builderAlert(fixture)).not.toBeNull();
        expect((fixture.nativeElement as HTMLElement).querySelector('.admin-acquisition__url')).toBeNull();
    });
    it('accepts512encoded path characters, rejects513 and recovers when shortened', () => {
        verifyPathBoundary(fixture);
    });
    it('checks encoded Unicode path length even when every field is at most160characters', () => {
        fillBuilder(fixture, {
            source: 's'.repeat(UTM_LIMIT),
            medium: 'm'.repeat(UTM_LIMIT),
            campaign: 'c'.repeat(UTM_LIMIT),
            content: 'Ж'.repeat(UTM_LIMIT),
            term: 't'.repeat(UTM_LIMIT),
        });
        expect(builderAlert(fixture)).not.toBeNull();
        expect((fixture.nativeElement as HTMLElement).querySelector('.admin-acquisition__url')).toBeNull();
    });
    it('preserves a literal path plus and publishes query spaces in canonical encoded form', () => {
        fillBuilder(fixture, {
            'base-url': 'https://fooddiary.club/a+b?keep=two+words#hero',
            source: 'audit',
            medium: '',
            campaign: '',
            content: '',
            term: '',
        });
        const url = generatedUrl(fixture);
        expect(url.pathname).toBe('/a+b');
        expect(url.search).toContain('keep=two%20words');
        expect(url.searchParams.get('keep')).toBe('two words');
        expect(url.hash).toBe('#hero');
    });
    it('renders the actual Russian boundary message without replacement characters', async () => {
        const translate = TestBed.inject(TranslateService);
        translate.setTranslation('ru', ruCore);
        await firstValueFrom(translate.use('ru'));
        fillBuilder(fixture, { 'base-url': `https://fooddiary.club/ru?padding=${'p'.repeat(PATH_LIMIT)}` });
        const alert = builderAlert(fixture);
        expect(alert).not.toBeNull();
        const messages: Readonly<Record<string, string>> = ruCore.ADMIN_ACQUISITION;
        expect(alert?.textContent.trim()).toBe(messages['LINK_PATH_LIMIT'].replace('{{max}}', String(PATH_LIMIT)));
        expect(alert?.textContent).not.toMatch(/[\uFFFD\u00D0\u00D1]/u);
    });
});

function fillBuilder(fixture: ComponentFixture<AdminAcquisitionComponent>, values: Record<string, string>): void {
    for (const [key, value] of Object.entries(values)) {
        const input = (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>(`input#admin-acquisition-${key}`);
        if (input === null) {
            throw new Error(`Builder field missing: ${key}`);
        }
        input.value = value;
        input.dispatchEvent(new Event('input', { bubbles: true }));
    }
    fixture.detectChanges();
}
function builderAlert(fixture: ComponentFixture<AdminAcquisitionComponent>): HTMLElement | null {
    return (fixture.nativeElement as HTMLElement).querySelector('.admin-acquisition__builder-panel [role=alert]');
}
function generatedUrl(fixture: ComponentFixture<AdminAcquisitionComponent>): URL {
    const output = (fixture.nativeElement as HTMLElement).querySelector('.admin-acquisition__url');
    expect(output).not.toBeNull();
    return new URL(output?.textContent.trim() ?? '');
}
function verifyPathBoundary(fixture: ComponentFixture<AdminAcquisitionComponent>): void {
    const prefix = '/ru?padding=';
    const suffix = '&utm_source=x';
    const padding = 'p'.repeat(PATH_LIMIT - prefix.length - suffix.length);
    fillBuilder(fixture, {
        'base-url': `https://fooddiary.club${prefix}${padding}`,
        source: 'x',
        medium: '',
        campaign: '',
        content: '',
        term: '',
    });
    const url = generatedUrl(fixture);
    expect(url.pathname.length + url.search.length).toBe(PATH_LIMIT);
    fillBuilder(fixture, { 'base-url': `https://fooddiary.club${prefix}${padding}p` });
    expect(builderAlert(fixture)).not.toBeNull();
    expect((fixture.nativeElement as HTMLElement).querySelector('.admin-acquisition__url')).toBeNull();
    fillBuilder(fixture, { 'base-url': `https://fooddiary.club${prefix}${padding}` });
    expect(generatedUrl(fixture).searchParams.get('utm_source')).toBe('x');
    expect(builderAlert(fixture)).toBeNull();
}

function emptyRange(params: Record<string, string | number>): MarketingAttributionRange {
    const fromUtc = String(params['fromUtc']);
    const toUtc = String(params['toUtc']);
    const hours = (Date.parse(toUtc) - Date.parse(fromUtc)) / HOUR_MS;
    const summary: MarketingAttributionSummary = {
        windowHours: hours,
        generatedAtUtc: toUtc,
        events: 0,
        visits: 0,
        signups: 0,
        premiumStarts: 0,
        anonymousVisitors: 0,
        sessions: 0,
        attributedEvents: 0,
        organicEvents: 0,
        attributedVisits: 0,
        organicVisits: 0,
        signupRatePercent: 0,
        premiumRatePercent: 0,
        lastEventAtUtc: null,
        topCampaigns: [],
        topSources: [],
        recentEvents: [],
    };
    return {
        fromUtc,
        toUtc,
        previousFromUtc: new Date(Date.parse(fromUtc) - hours * HOUR_MS).toISOString(),
        current: summary,
        previous: summary,
        byDay: [],
        eventTotal: 0,
    };
}
