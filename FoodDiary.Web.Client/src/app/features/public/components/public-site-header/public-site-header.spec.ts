import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { FdUiDialogService } from 'fd-ui-kit';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { PwaInstallService } from '../../../../shared/service-worker/pwa-install.service';
import { PwaInstallPromotionService } from '../../../../shared/service-worker/pwa-install-promotion.service';
import { PublicSiteHeaderComponent } from './public-site-header';

describe('Public site header installation', () => {
    let fixture: ComponentFixture<PublicSiteHeaderComponent>;
    let root: HTMLElement;
    const pwa = {
        installMode: signal<'prompt' | 'ios' | null>(null),
        isInstalling: signal(false),
        installError: signal(false),
    };
    const installAsync = vi.fn<() => Promise<void>>();
    const open = vi.fn();

    beforeEach(async () => {
        pwa.installMode.set(null);
        pwa.isInstalling.set(false);
        pwa.installError.set(false);
        installAsync.mockReset().mockResolvedValue();
        open.mockReset();
        await TestBed.configureTestingModule({
            imports: [PublicSiteHeaderComponent],
            providers: [
                provideRouter([]),
                provideTranslateTesting(),
                { provide: PwaInstallService, useValue: pwa },
                { provide: PwaInstallPromotionService, useValue: { installAsync } },
                { provide: FdUiDialogService, useValue: { open } },
            ],
        }).compileComponents();
        fixture = TestBed.createComponent(PublicSiteHeaderComponent);
        fixture.detectChanges();
        const element: unknown = fixture.nativeElement;
        if (!(element instanceof HTMLElement)) {
            throw new Error('Expected the public header element');
        }
        root = element;
    });

    function installButton(): HTMLButtonElement | null {
        return root.querySelector('.public-header__install button');
    }

    it('hides installation when unavailable or already installed and responds to a later browser offer', () => {
        expect(installButton()).toBeNull();
        pwa.installMode.set('prompt');
        fixture.detectChanges();
        expect(installButton()?.getAttribute('aria-label')).toBe('PUBLIC_SITE.INSTALL');
        pwa.installMode.set(null);
        fixture.detectChanges();
        expect(installButton()).toBeNull();
    });

    it('starts native installation directly in the click handler', async () => {
        pwa.installMode.set('prompt');
        fixture.detectChanges();
        installButton()?.click();
        expect(installAsync).toHaveBeenCalledOnce();
        expect(open).not.toHaveBeenCalled();
        await fixture.whenStable();
    });

    it('opens iOS instructions as a dialog instead of requesting a native prompt', () => {
        pwa.installMode.set('ios');
        fixture.detectChanges();
        expect(installButton()?.getAttribute('aria-haspopup')).toBe('dialog');
        installButton()?.click();
        expect(open).toHaveBeenCalledWith(expect.anything(), { size: 'sm' });
        expect(installAsync).not.toHaveBeenCalled();
    });

    it('prevents repeat clicks while installing and shows a failed prompt message', () => {
        pwa.isInstalling.set(true);
        fixture.detectChanges();
        expect(installButton()?.disabled).toBe(true);
        pwa.isInstalling.set(false);
        pwa.installError.set(true);
        fixture.detectChanges();
        expect(installButton()).toBeNull();
        expect(root.textContent).toContain('PWA.INSTALL.ERROR');
    });
});
