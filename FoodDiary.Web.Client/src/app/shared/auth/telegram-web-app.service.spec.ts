import { DOCUMENT } from '@angular/common';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { BrowserWindowService } from '../platform/browser-window.service';
import { TelegramWebAppService } from './telegram-web-app.service';

describe('TelegramWebAppService', () => {
    const browser = {
        getTelegramInitData: vi.fn(),
        getHref: vi.fn(),
        setTimeout: vi.fn(),
        clearTimeout: vi.fn(),
    };
    let service: TelegramWebAppService;
    let document: Document;

    beforeEach(() => {
        vi.resetAllMocks();
        browser.getTelegramInitData.mockReturnValue(null);
        browser.getHref.mockReturnValue('https://fooddiary.example/auth/telegram');
        TestBed.configureTestingModule({ providers: [{ provide: BrowserWindowService, useValue: browser }] });
        service = TestBed.inject(TelegramWebAppService);
        document = TestBed.inject(DOCUMENT);
        document.head.querySelectorAll('script[src*="telegram-web-app"]').forEach(script => {
            script.remove();
        });
    });

    it('does not load the provider script for ordinary website visits', async () => {
        await service.initializeAsync();
        expect(document.head.querySelector('script[src*="telegram-web-app"]')).toBeNull();
    });

    it('preserves the original nested launch data when routing changes the fragment and the SDK reads a truncated value', () => {
        const proof = 'user=%7B%22id%22%3A1%7D&auth_date=1793664000&hash=synthetic-signature';
        browser.getHref.mockReturnValue(`https://fooddiary.example/#tgWebAppData=${encodeURIComponent(proof)}`);
        const launchService = TestBed.runInInjectionContext(() => new TelegramWebAppService());
        browser.getHref.mockReturnValue('https://fooddiary.example/auth/telegram#tgWebAppData=user=truncated&hash=outer-field');
        browser.getTelegramInitData.mockReturnValue('user=truncated');
        expect(launchService.getInitData()).toBe(proof);
        browser.getHref.mockReturnValue('https://fooddiary.example/dashboard');
        expect(launchService.getInitData()).toBe(proof);
    });

    it('uses an SDK-only proof when the initial document has no launch data', () => {
        browser.getTelegramInitData.mockReturnValue('sdk-only-proof');
        expect(service.getInitData()).toBe('sdk-only-proof');
    });

    it('does not capture empty launch data over a later SDK proof', () => {
        browser.getHref.mockReturnValue('https://fooddiary.example/#tgWebAppData=');
        const launchService = TestBed.runInInjectionContext(() => new TelegramWebAppService());
        browser.getTelegramInitData.mockReturnValue('sdk-only-proof');
        expect(launchService.getInitData()).toBe('sdk-only-proof');
    });

    it('loads one SDK for concurrent Mini App initialization calls', async () => {
        browser.getHref.mockReturnValue('https://fooddiary.example/auth/telegram#tgWebAppData=test-proof');
        const first = service.initializeAsync();
        const second = service.initializeAsync();
        const scripts = document.head.querySelectorAll('script[src*="telegram-web-app"]');
        expect(scripts).toHaveLength(1);
        browser.getTelegramInitData.mockReturnValue('test-proof');
        scripts[0].dispatchEvent(new Event('load'));
        await Promise.all([first, second]);
    });

    it('rejects initialization when the SDK loads without a Telegram proof', async () => {
        browser.getHref.mockReturnValue('https://fooddiary.example/auth/telegram#tgWebAppData=test-proof');
        const initialization = service.initializeAsync();
        const assertion = expect(initialization).rejects.toThrow('Telegram Mini App initialization failed');
        document.head.querySelector('script[src*="telegram-web-app"]')?.dispatchEvent(new Event('load'));
        await assertion;
    });
});
