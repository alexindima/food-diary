import { TestBed } from '@angular/core/testing';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { BrowserStorageService } from '../platform/browser-storage.service';
import { MARKETING_ATTRIBUTION_ENABLED } from './marketing-attribution-enabled';
import { MARKETING_NOTICE_VERSION, MarketingConsentService } from './marketing-consent.service';

describe('MarketingConsentService', () => {
    afterEach(() => vi.useRealTimers());

    it.each([
        null,
        { version: 'old', allowed: true, decidedAt: '2026-11-01T00:00:00Z' },
        { version: MARKETING_NOTICE_VERSION, allowed: true, decidedAt: 'invalid' },
        { version: MARKETING_NOTICE_VERSION, allowed: true, decidedAt: '2027-01-01T00:00:00Z' },
        { version: MARKETING_NOTICE_VERSION, allowed: true, decidedAt: '2025-01-01T00:00:00Z' },
    ])('requires a valid, current, unexpired opt-in: %j', value => {
        vi.useFakeTimers();
        vi.setSystemTime(new Date('2026-11-02T00:00:00Z'));
        TestBed.configureTestingModule({
            providers: [
                { provide: MARKETING_ATTRIBUTION_ENABLED, useValue: true },
                { provide: BrowserStorageService, useValue: { getJson: (): unknown => value } },
            ],
        });
        const consent = TestBed.inject(MarketingConsentService);
        expect(consent.isAllowed()).toBe(false);
        expect(consent.grantedAt()).toBeNull();
    });

    it('stops immediately on withdrawal even when storage cannot be changed', () => {
        const old = { version: MARKETING_NOTICE_VERSION, allowed: true, decidedAt: new Date().toISOString() };
        TestBed.configureTestingModule({
            providers: [
                { provide: MARKETING_ATTRIBUTION_ENABLED, useValue: true },
                {
                    provide: BrowserStorageService,
                    useValue: {
                        getJson: (): unknown => old,
                        setJson: (): boolean => false,
                        removeItem: (): boolean => false,
                    },
                },
            ],
        });
        const consent = TestBed.inject(MarketingConsentService);
        expect(consent.isAllowed()).toBe(true);
        consent.decide(false);
        expect(consent.isAllowed()).toBe(false);
        expect(consent.grantedAt()).toBeNull();
    });
});
