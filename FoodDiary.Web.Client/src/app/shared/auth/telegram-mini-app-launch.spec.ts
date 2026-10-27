import { DOCUMENT } from '@angular/common';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { BrowserWindowService } from '../platform/browser-window.service';
import { TelegramWebAppService } from './telegram-web-app.service';
const SHA256_BYTES = 32;
const key = 'fooddiary.telegram.logged-out-launch';
const browser = { getTelegramInitData: vi.fn(), getHref: vi.fn() };
let service: TelegramWebAppService;
let crypto: Crypto;
let originalSubtle: PropertyDescriptor | undefined;
const digest = vi.fn();
const freshService = (): TelegramWebAppService => TestBed.runInInjectionContext(() => new TelegramWebAppService());

beforeEach(() => {
    vi.resetAllMocks();
    sessionStorage.clear();
    browser.getTelegramInitData.mockReturnValue(null);
    browser.getHref.mockReturnValue('https://fooddiary.example/#tgWebAppData=signed-proof');
    TestBed.configureTestingModule({ providers: [{ provide: BrowserWindowService, useValue: browser }] });
    const view = TestBed.inject(DOCUMENT).defaultView;
    if (view === null) {
        throw new Error('Missing browser test window');
    }
    crypto = view.crypto;
    originalSubtle = Object.getOwnPropertyDescriptor(crypto, 'subtle');
    digest.mockResolvedValue(new Uint8Array(SHA256_BYTES).fill(1).buffer);
    Object.defineProperty(crypto, 'subtle', { configurable: true, value: { digest } });
    service = TestBed.inject(TelegramWebAppService);
});

afterEach(() => {
    if (originalSubtle === undefined) {
        Reflect.deleteProperty(crypto, 'subtle');
    } else {
        Object.defineProperty(crypto, 'subtle', originalSubtle);
    }
    sessionStorage.clear();
});

describe('Mini App automatic sign-in launch policy', () => {
    it('allows one automatic attempt for a real Telegram launch', async () => {
        expect(await service.isAutomaticSignInAllowedAsync()).toBe(true);
        expect(await Promise.all([service.claimAutomaticSignInAsync(), service.claimAutomaticSignInAsync()])).toEqual([true, false]);
        expect(await service.isAutomaticSignInAllowedAsync()).toBe(false);
        expect(await service.claimAutomaticSignInAsync()).toBe(false);
    });

    it('recognizes an SDK proof even after the router clears the launch fragment', async () => {
        browser.getHref.mockReturnValue('https://fooddiary.example/dashboard');
        browser.getTelegramInitData.mockReturnValue('signed-proof');
        expect(await service.claimAutomaticSignInAsync()).toBe(true);
    });

    it.each(['https://fooddiary.example/', 'https://fooddiary.example/?tgWebAppData=unsigned', null])(
        'does not authenticate ordinary visits, query hints or SSR: %s',
        async href => {
            browser.getHref.mockReturnValue(href);
            expect(await service.claimAutomaticSignInAsync()).toBe(false);
        },
    );
});
describe('Mini App explicit logout across launches', () => {
    it('keeps explicit logout across a reload of the same launch without storing Telegram proof', async () => {
        await service.suppressAutomaticSignInAsync();
        expect(sessionStorage.getItem(key)).toBe('01'.repeat(SHA256_BYTES));
        expect(digest).toHaveBeenCalledWith('SHA-256', new TextEncoder().encode('signed-proof'));
        expect(sessionStorage.getItem(key)).not.toContain('signed-proof');
        expect(await service.isAutomaticSignInAllowedAsync()).toBe(false);
        expect(await freshService().claimAutomaticSignInAsync()).toBe(false);
    });

    it('allows a newly opened Telegram launch after a previous explicit logout', async () => {
        await service.suppressAutomaticSignInAsync();
        browser.getHref.mockReturnValue('https://fooddiary.example/#tgWebAppData=new-proof');
        digest.mockResolvedValue(new Uint8Array(SHA256_BYTES).fill(2).buffer);
        expect(await freshService().claimAutomaticSignInAsync()).toBe(true);
    });

    it('uses the SDK proof when logout occurs on an authenticated application route', async () => {
        browser.getHref.mockReturnValue('https://fooddiary.example/dashboard');
        browser.getTelegramInitData.mockReturnValue('signed-proof');
        await service.suppressAutomaticSignInAsync();
        expect(digest).toHaveBeenCalledWith('SHA-256', new TextEncoder().encode('signed-proof'));
        expect(await freshService().claimAutomaticSignInAsync()).toBe(false);
    });

    it('does not create a Mini App logout marker for ordinary website sign-out', async () => {
        browser.getHref.mockReturnValue('https://fooddiary.example/');
        await service.suppressAutomaticSignInAsync();
        expect(sessionStorage.getItem(key)).toBeNull();
        expect(digest).not.toHaveBeenCalled();
    });

    it('preserves logout when crypto is unavailable or rejects the digest', async () => {
        digest.mockRejectedValue(new Error('Crypto unavailable'));
        await service.suppressAutomaticSignInAsync();
        expect(await freshService().claimAutomaticSignInAsync()).toBe(false);
        Object.defineProperty(crypto, 'subtle', { configurable: true, value: undefined });
        expect(await freshService().claimAutomaticSignInAsync()).toBe(false);
    });
});
