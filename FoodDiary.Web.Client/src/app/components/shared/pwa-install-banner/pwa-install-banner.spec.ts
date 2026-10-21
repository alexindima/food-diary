import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import { BrowserStorageService } from '../../../shared/platform/browser-storage.service';
import { AppUpdateService } from '../../../shared/service-worker/app-update.service';
import { type PwaInstallResult, PwaInstallService } from '../../../shared/service-worker/pwa-install.service';
import { PwaInstallPromotionService } from '../../../shared/service-worker/pwa-install-promotion.service';
import { PWA_PROMOTION_VISITS_STORAGE_KEY } from '../../../shared/service-worker/pwa-install-promotion-state';
import { PwaInstallCardComponent } from '../pwa-install-card/pwa-install-card';
import { PwaInstallBannerComponent } from './pwa-install-banner';

const IOS_STEPS = 3;
let fixture: ComponentFixture<PwaInstallBannerComponent>;
let root: HTMLElement;
let promotion: PwaInstallPromotionService;
let pwa: ReturnType<typeof createPwaStub>;
let updates: { hasPendingUpdate: ReturnType<typeof signal<boolean>> };

function createPwaStub(): {
    isMobileDevice: ReturnType<typeof signal<boolean>>;
    isStandalone: ReturnType<typeof signal<boolean>>;
    showInstallCard: ReturnType<typeof signal<boolean>>;
    installMode: ReturnType<typeof signal<'prompt' | 'ios' | null>>;
    isInstalling: ReturnType<typeof signal<boolean>>;
    installError: ReturnType<typeof signal<boolean>>;
    installAsync: ReturnType<typeof vi.fn<() => Promise<PwaInstallResult>>>;
} {
    return {
        isMobileDevice: signal(true),
        isStandalone: signal(false),
        showInstallCard: signal(true),
        installMode: signal<'prompt' | 'ios' | null>('prompt'),
        isInstalling: signal(false),
        installError: signal(false),
        installAsync: vi.fn<() => Promise<PwaInstallResult>>().mockResolvedValue('accepted'),
    };
}

beforeEach(async () => {
    pwa = createPwaStub();
    updates = { hasPendingUpdate: signal(false) };
    await TestBed.configureTestingModule({
        imports: [PwaInstallBannerComponent, PwaInstallCardComponent],
        providers: [
            provideTranslateTesting(),
            { provide: PwaInstallService, useValue: pwa },
            { provide: AppUpdateService, useValue: updates },
        ],
    }).compileComponents();
    TestBed.inject(BrowserStorageService).setJson('local', PWA_PROMOTION_VISITS_STORAGE_KEY, { visits: 3, lastSeenAt: Date.now() });
    promotion = TestBed.inject(PwaInstallPromotionService);
    promotion.initialize();
    fixture = TestBed.createComponent(PwaInstallBannerComponent);
    fixture.componentRef.setInput('visible', true);
    fixture.detectChanges();
    root = elementOf(fixture);
});

function elementOf<T>(component: ComponentFixture<T>): HTMLElement {
    const element: unknown = component.nativeElement;
    if (!(element instanceof HTMLElement)) {
        throw new Error('Expected a rendered HTML element');
    }
    return element;
}

function button(surface: HTMLElement, key: string): HTMLButtonElement {
    const match = Array.from(surface.querySelectorAll('button')).find(element => element.textContent.includes(key));
    if (match === undefined) {
        throw new Error(`Button ${key} not found`);
    }
    return match;
}

describe('PWA promotion and profile installation integration', () => {
    it('keeps profile installation available after permanent opt-out and restores promotion explicitly', async () => {
        button(root, 'PWA.PROMOTION.NEVER').click();
        fixture.detectChanges();
        expect(root.querySelector('section')).toBeNull();
        expect(promotion.isDismissed()).toBe(true);
        const profile = TestBed.createComponent(PwaInstallCardComponent);
        profile.detectChanges();
        button(elementOf(profile), 'PWA.PROMOTION.ALLOW').click();
        fixture.detectChanges();
        expect(promotion.isDismissed()).toBe(false);
        expect(root.querySelector('section')).not.toBeNull();
        button(root, 'PWA.PROMOTION.NEVER').click();
        profile.detectChanges();
        button(elementOf(profile), 'PWA.INSTALL.BUTTON').click();
        expect(pwa.installAsync).toHaveBeenCalledOnce();
        await profile.whenStable();
    });

    it('snoozes through the visible Not now action', () => {
        button(root, 'PWA.PROMOTION.NOT_NOW').click();
        fixture.detectChanges();
        expect(promotion.isDismissed()).toBe(true);
        expect(root.querySelector('section')).toBeNull();
    });

    it('keeps a direct user gesture through the shared installation action', () => {
        button(root, 'PWA.INSTALL.BUTTON').click();
        expect(pwa.installAsync).toHaveBeenCalledOnce();
    });
});

describe('PWA promotion presentation states', () => {
    it('hides offers while an update is pending and outside the allowed page context', () => {
        updates.hasPendingUpdate.set(true);
        fixture.detectChanges();
        expect(root.querySelector('section')).toBeNull();
        updates.hasPendingUpdate.set(false);
        fixture.componentRef.setInput('visible', false);
        fixture.detectChanges();
        expect(root.querySelector('section')).toBeNull();
    });

    it('does not show an automatic banner on desktop', () => {
        pwa.isMobileDevice.set(false);
        fixture.detectChanges();
        expect(root.querySelector('section')).toBeNull();
    });

    it('opens accessible iOS instructions within the banner', () => {
        pwa.installMode.set('ios');
        fixture.detectChanges();
        const help = button(root, 'PWA.INSTALL.IOS_BUTTON');
        expect(help.getAttribute('aria-expanded')).toBe('false');
        help.click();
        fixture.detectChanges();
        expect(help.getAttribute('aria-expanded')).toBe('true');
        expect(help.getAttribute('aria-controls')).toBe('pwa-banner-ios-instructions');
        expect(root.querySelectorAll('#pwa-banner-ios-instructions li')).toHaveLength(IOS_STEPS);
    });
});
