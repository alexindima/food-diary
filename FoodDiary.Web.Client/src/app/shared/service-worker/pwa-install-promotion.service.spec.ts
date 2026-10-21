import { DOCUMENT } from '@angular/common';
import { ApplicationRef, PLATFORM_ID, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Subject } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { BrowserStorageService } from '../platform/browser-storage.service';
import { type PwaInstallResult, PwaInstallService } from './pwa-install.service';
import { PwaInstallPromotionService } from './pwa-install-promotion.service';
import { PWA_PROMOTION_STORAGE_KEY, PWA_PROMOTION_VISITS_STORAGE_KEY } from './pwa-install-promotion-state';

const START = 1_700_000_000_000;
const HALF_HOUR = 1_800_000;
const HOUR = 3_600_000;
const THIRTY_DAYS = 2_592_000_000;
const MANUAL_INSTALL_ATTEMPTS = 3;
let stable: Subject<boolean>;
let view: EventTarget;
let documentRef: EventTarget & { visibilityState: string };
let saved: Map<string, unknown>;
let storage: { getJson: ReturnType<typeof vi.fn>; setJson: ReturnType<typeof vi.fn> };
let pwa: {
    isMobileDevice: ReturnType<typeof signal<boolean>>;
    isStandalone: ReturnType<typeof signal<boolean>>;
    showInstallCard: ReturnType<typeof signal<boolean>>;
    installAsync: ReturnType<typeof vi.fn<() => Promise<PwaInstallResult>>>;
};

beforeEach(() => {
    vi.useFakeTimers();
    vi.setSystemTime(START);
    stable = new Subject<boolean>();
    view = new EventTarget();
    documentRef = Object.assign(new EventTarget(), { defaultView: view, visibilityState: 'visible' });
    saved = new Map<string, unknown>();
    storage = {
        getJson: vi.fn((_scope: string, key: string): unknown => saved.get(key) ?? null),
        setJson: vi.fn((_scope: string, key: string, value: unknown): void => {
            saved.set(key, value);
        }),
    };
    pwa = {
        isMobileDevice: signal(true),
        isStandalone: signal(false),
        showInstallCard: signal(true),
        installAsync: vi.fn<() => Promise<PwaInstallResult>>().mockResolvedValue('dismissed'),
    };
    configureProviders();
});

afterEach(() => {
    TestBed.resetTestingModule();
    vi.useRealTimers();
});

function configureProviders(): void {
    TestBed.configureTestingModule({
        providers: [
            PwaInstallPromotionService,
            { provide: DOCUMENT, useValue: documentRef },
            { provide: PLATFORM_ID, useValue: 'browser' },
            { provide: ApplicationRef, useValue: { isStable: stable } },
            { provide: BrowserStorageService, useValue: storage },
            { provide: PwaInstallService, useValue: pwa },
        ],
    });
}

function start(): PwaInstallPromotionService {
    const service = TestBed.inject(PwaInstallPromotionService);
    service.initialize();
    service.setActive(true);
    return service;
}

function returnAfter(milliseconds: number): void {
    documentRef.visibilityState = 'hidden';
    documentRef.dispatchEvent(new Event('visibilitychange'));
    vi.setSystemTime(Date.now() + milliseconds);
    documentRef.visibilityState = 'visible';
    documentRef.dispatchEvent(new Event('visibilitychange'));
}

function qualify(): PwaInstallPromotionService {
    const service = start();
    returnAfter(HALF_HOUR);
    returnAfter(HALF_HOUR);
    return service;
}

describe('PWA promotion visits', () => {
    it('offers installation on the third visit and not on first or second visits', () => {
        const service = start();
        expect(service.shouldShowBanner()).toBe(false);
        returnAfter(HALF_HOUR);
        expect(service.shouldShowBanner()).toBe(false);
        returnAfter(HALF_HOUR);
        expect(service.shouldShowBanner()).toBe(true);
    });

    it('does not inflate visits on reloads or concurrent document initialization', () => {
        start();
        TestBed.resetTestingModule();
        configureProviders();
        const reloaded = start();
        expect(saved.get(PWA_PROMOTION_VISITS_STORAGE_KEY)).toMatchObject({ visits: 1 });
        returnAfter(HALF_HOUR);
        expect(reloaded.shouldShowBanner()).toBe(false);
    });

    it('keeps a continuously visible session active after Angular stabilizes', async () => {
        const service = start();
        service.initialize();
        stable.next(true);
        await vi.advanceTimersByTimeAsync(HOUR);
        expect(saved.get(PWA_PROMOTION_VISITS_STORAGE_KEY)).toMatchObject({ visits: 1 });
        expect(service.shouldShowBanner()).toBe(false);
    });

    it('measures inactivity from the instant the document becomes hidden', () => {
        const service = start();
        vi.setSystemTime(START + HALF_HOUR - 1);
        returnAfter(HALF_HOUR - 1);
        expect(saved.get(PWA_PROMOTION_VISITS_STORAGE_KEY)).toMatchObject({ visits: 1 });
        returnAfter(HALF_HOUR);
        expect(service.shouldShowBanner()).toBe(false);
    });

    it.each(['anonymous', 'desktop', 'standalone'])('does not count %s usage', context => {
        if (context === 'desktop') {
            pwa.isMobileDevice.set(false);
        }
        if (context === 'standalone') {
            pwa.isStandalone.set(true);
        }
        const service = TestBed.inject(PwaInstallPromotionService);
        service.initialize();
        service.setActive(context !== 'anonymous');
        returnAfter(HOUR);
        expect(storage.setJson).not.toHaveBeenCalled();
        expect(service.shouldShowBanner()).toBe(false);
    });
});

describe('PWA promotion dismissal preferences', () => {
    it('snoozes for exactly thirty days and preserves the decision after reload', () => {
        const service = qualify();
        service.snooze();
        const snoozedAt = Date.now();
        expect(saved.get(PWA_PROMOTION_STORAGE_KEY)).toMatchObject({ snoozedUntil: snoozedAt + THIRTY_DAYS });
        expect(service.shouldShowBanner()).toBe(false);
        TestBed.resetTestingModule();
        configureProviders();
        const reloaded = start();
        vi.setSystemTime(snoozedAt + THIRTY_DAYS - 1);
        documentRef.dispatchEvent(new Event('visibilitychange'));
        expect(reloaded.shouldShowBanner()).toBe(false);
        vi.setSystemTime(snoozedAt + THIRTY_DAYS);
        documentRef.dispatchEvent(new Event('visibilitychange'));
        expect(reloaded.shouldShowBanner()).toBe(true);
    });

    it('preserves permanent opt-out after time passes and allows explicit restoration', () => {
        const service = qualify();
        service.dismissPermanently();
        returnAfter(THIRTY_DAYS + THIRTY_DAYS);
        expect(service.shouldShowBanner()).toBe(false);
        TestBed.resetTestingModule();
        configureProviders();
        const reloaded = start();
        expect(reloaded.isDismissed()).toBe(true);
        reloaded.allowOffers();
        expect(reloaded.isDismissed()).toBe(false);
        expect(reloaded.shouldShowBanner()).toBe(true);
    });

    it('snoozes a rejected browser prompt without blocking subsequent manual installation', async () => {
        const service = qualify();
        await service.installAsync();
        expect(service.isDismissed()).toBe(true);
        pwa.installAsync.mockResolvedValue('accepted');
        await service.installAsync();
        expect(pwa.installAsync).toHaveBeenCalledTimes(2);
        service.dismissPermanently();
        await service.installAsync();
        expect(pwa.installAsync).toHaveBeenCalledTimes(MANUAL_INSTALL_ATTEMPTS);
    });

    it('handles denied storage with in-memory preferences', () => {
        storage.getJson.mockReturnValue(null);
        storage.setJson.mockImplementation(() => {});
        const service = qualify();
        expect(service.shouldShowBanner()).toBe(true);
        service.snooze();
        expect(service.shouldShowBanner()).toBe(false);
        service.dismissPermanently();
        returnAfter(THIRTY_DAYS + THIRTY_DAYS);
        expect(service.isDismissed()).toBe(true);
        service.allowOffers();
        expect(service.shouldShowBanner()).toBe(true);
    });
});

describe('PWA promotion lifecycle and cross-tab state', () => {
    it('does not overwrite a concurrent opt-out while recording activity', () => {
        const service = qualify();
        storage.getJson.mockImplementation((_scope: string, key: string): unknown => {
            if (key === PWA_PROMOTION_VISITS_STORAGE_KEY) {
                saved.set(PWA_PROMOTION_STORAGE_KEY, { never: true, snoozedUntil: null });
            }
            return saved.get(key) ?? null;
        });
        documentRef.dispatchEvent(new Event('visibilitychange'));
        expect(saved.get(PWA_PROMOTION_STORAGE_KEY)).toMatchObject({ never: true });
        view.dispatchEvent(Object.assign(new Event('storage'), { key: PWA_PROMOTION_STORAGE_KEY }));
        expect(service.shouldShowBanner()).toBe(false);
    });

    it('reacts to preference changes and storage clearing in another tab', () => {
        const service = qualify();
        saved.set(PWA_PROMOTION_STORAGE_KEY, { visits: 3, lastSeenAt: Date.now(), never: true });
        view.dispatchEvent(Object.assign(new Event('storage'), { key: PWA_PROMOTION_STORAGE_KEY }));
        expect(service.shouldShowBanner()).toBe(false);
        saved.clear();
        view.dispatchEvent(Object.assign(new Event('storage'), { key: null }));
        expect(service.isDismissed()).toBe(false);
        expect(service.shouldShowBanner()).toBe(false);
    });

    it('releases heartbeat and event listeners on destruction', async () => {
        start();
        stable.next(true);
        TestBed.resetTestingModule();
        const calls = storage.setJson.mock.calls.length;
        await vi.advanceTimersByTimeAsync(HOUR);
        documentRef.dispatchEvent(new Event('visibilitychange'));
        expect(storage.setJson).toHaveBeenCalledTimes(calls);
    });

    it('skips browser state during SSR', () => {
        TestBed.overrideProvider(PLATFORM_ID, { useValue: 'server' });
        const service = start();
        stable.next(true);
        expect(service.shouldShowBanner()).toBe(false);
        expect(storage.getJson).not.toHaveBeenCalled();
        expect(storage.setJson).not.toHaveBeenCalled();
    });
});
