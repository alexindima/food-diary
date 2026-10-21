import { DOCUMENT } from '@angular/common';
import { PLATFORM_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { BrowserWindowService } from '../platform/browser-window.service';
import { PwaInstallService } from './pwa-install.service';

const media = Object.assign(new EventTarget(), { matches: false });
const navigator = { userAgent: 'Chrome', platform: 'Linux', maxTouchPoints: 0, standalone: false };
const view = Object.assign(new EventTarget(), {
    isSecureContext: true,
    navigator,
    location: { hostname: 'localhost', protocol: 'http:' },
    matchMedia: vi.fn(() => media),
    Capacitor: { isNativePlatform: (): boolean => false },
});

beforeEach(() => {
    media.matches = false;
    Object.assign(navigator, { userAgent: 'Chrome', platform: 'Linux', maxTouchPoints: 0, standalone: false });
    view.isSecureContext = true;
    view.Capacitor.isNativePlatform = (): boolean => false;
    TestBed.configureTestingModule({
        providers: [
            PwaInstallService,
            { provide: DOCUMENT, useValue: { defaultView: view } },
            { provide: PLATFORM_ID, useValue: 'browser' },
            { provide: BrowserWindowService, useValue: { getTelegramInitData: (): string | null => null } },
        ],
    });
});

function installEvent(outcome: 'accepted' | 'dismissed' = 'dismissed'): Event & { prompt: ReturnType<typeof vi.fn> } {
    return Object.assign(new Event('beforeinstallprompt', { cancelable: true }), {
        prompt: vi.fn().mockResolvedValue({ outcome, platform: 'web' }),
    });
}

describe('PwaInstallService browser prompts', () => {
    it('waits for browser availability and consumes each prompt only once', async () => {
        const service = TestBed.inject(PwaInstallService);
        service.initialize();
        service.initialize();
        expect(service.showInstallCard()).toBe(false);
        const event = installEvent();
        view.dispatchEvent(event);
        expect(event.defaultPrevented).toBe(true);
        expect(service.installMode()).toBe('prompt');
        const installing = service.installAsync();
        expect(event.prompt).toHaveBeenCalledOnce();
        await installing;
        await service.installAsync();
        expect(event.prompt).toHaveBeenCalledOnce();
        expect(service.showInstallCard()).toBe(false);
        view.dispatchEvent(installEvent());
        expect(service.installMode()).toBe('prompt');
    });

    it('hides installation after acceptance and installation through browser UI', async () => {
        const service = TestBed.inject(PwaInstallService);
        service.initialize();
        view.dispatchEvent(installEvent('accepted'));
        await service.installAsync();
        expect(service.showInstallCard()).toBe(false);
        view.dispatchEvent(installEvent());
        view.dispatchEvent(new Event('appinstalled'));
        expect(service.showInstallCard()).toBe(false);
    });

    it('handles a failed native prompt without an unhandled rejection', async () => {
        const service = TestBed.inject(PwaInstallService);
        service.initialize();
        const event = installEvent();
        event.prompt.mockRejectedValue(new Error('NotAllowedError'));
        view.dispatchEvent(event);
        await service.installAsync();
        expect(service.installError()).toBe(true);
        expect(service.showInstallCard()).toBe(true);
        expect(service.installMode()).toBeNull();
    });
});

describe('PwaInstallService platform capabilities', () => {
    it.each([
        { userAgent: 'iPhone Safari', platform: 'iPhone', maxTouchPoints: 5 },
        { userAgent: 'Macintosh Safari', platform: 'MacIntel', maxTouchPoints: 5 },
    ])('offers manual iOS instructions for $platform', capability => {
        Object.assign(navigator, capability);
        const service = TestBed.inject(PwaInstallService);
        service.initialize();
        expect(service.installMode()).toBe('ios');
        expect(service.isMobileDevice()).toBe(true);
        media.matches = true;
        media.dispatchEvent(new Event('change'));
        expect(service.showInstallCard()).toBe(false);
    });

    it('recognizes the iOS standalone property', () => {
        Object.assign(navigator, { userAgent: 'iPhone Safari', standalone: true });
        const service = TestBed.inject(PwaInstallService);
        service.initialize();
        expect(service.isStandalone()).toBe(true);
        expect(service.showInstallCard()).toBe(false);
    });
});

describe('PwaInstallService mobile promotion capability', () => {
    it('keeps desktop installation available without treating the browser as a mobile device', () => {
        const service = TestBed.inject(PwaInstallService);
        service.initialize();
        view.dispatchEvent(installEvent());
        expect(service.installMode()).toBe('prompt');
        expect(service.isMobileDevice()).toBe(false);
    });

    it('recognizes Android installation capability', () => {
        navigator.userAgent = 'Android Chrome';
        const service = TestBed.inject(PwaInstallService);
        service.initialize();
        expect(service.isMobileDevice()).toBe(true);
    });
});

describe('PwaInstallService lifecycle', () => {
    it.each(['native', 'telegram', 'insecure', 'server'])('does not offer installation in %s contexts', context => {
        navigator.userAgent = 'iPhone Safari';
        if (context === 'native') {
            view.Capacitor.isNativePlatform = (): boolean => true;
        }
        if (context === 'telegram') {
            navigator.userAgent = 'iPhone Safari Telegram';
        }
        if (context === 'insecure') {
            view.isSecureContext = false;
        }
        if (context === 'server') {
            TestBed.overrideProvider(PLATFORM_ID, { useValue: 'server' });
        }
        const service = TestBed.inject(PwaInstallService);
        service.initialize();
        view.dispatchEvent(installEvent());
        expect(service.showInstallCard()).toBe(false);
    });

    it('releases event listeners when the application is destroyed', () => {
        const service = TestBed.inject(PwaInstallService);
        service.initialize();
        TestBed.resetTestingModule();
        view.dispatchEvent(installEvent());
        expect(service.showInstallCard()).toBe(false);
    });
});
