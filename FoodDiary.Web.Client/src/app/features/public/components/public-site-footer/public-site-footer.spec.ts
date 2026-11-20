import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { describe, expect, it, vi } from 'vitest';

import { environment } from '../../../../../environments/environment';
import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { MARKETING_ATTRIBUTION_ENABLED } from '../../../../shared/marketing/marketing-attribution-enabled';
import { PublicSiteFooterComponent } from './public-site-footer';

type FooterFixture = {
    fixture: ComponentFixture<PublicSiteFooterComponent>;
    root: HTMLElement;
};

describe('Public site footer', () => {
    it('keeps public links in the current language and provides the support address', async () => {
        const { fixture, root } = await renderFooterAsync(false, '/ru/pricing');
        expect(legalLinks(root)).toEqual([
            '/ru/about',
            '/ru/pricing',
            '/ru/contact',
            '/ru/privacy-policy',
            '/ru/terms-of-service',
            '/ru/guides/how-to-keep-a-food-diary',
            '/ru/guides/calories-in-homemade-meals',
        ]);
        const supportLink = root.querySelector('a[href^="mailto:"]');
        expect(supportLink?.getAttribute('href')).toBe(`mailto:${environment.supportEmail}`);
        expect(supportLink?.textContent.trim()).toBe(environment.supportEmail);
        expect(root.querySelector('nav button')).toBeNull();

        fixture.componentRef.setInput('path', '/about');
        fixture.detectChanges();
        expect(legalLinks(root)).toContain('/privacy-policy');
        expect(legalLinks(root)).toContain('/guides/how-to-keep-a-food-diary');
    });

    it('requests consent settings and restores keyboard focus when they close', async () => {
        const { fixture, root } = await renderFooterAsync(true, '/');
        const requested = vi.fn();
        fixture.componentInstance.marketingSettingsRequested.subscribe(requested);
        const consentButton = root.querySelector('nav button');
        if (!(consentButton instanceof HTMLButtonElement)) {
            throw new Error('Expected to find the marketing settings button');
        }

        expect(consentButton.textContent.trim()).toBe('MARKETING_CONSENT.SETTINGS');
        consentButton.click();
        expect(requested).toHaveBeenCalledOnce();
        fixture.componentInstance.focusMarketingSettings();
        expect(document.activeElement).toBe(consentButton);
    });
});

async function renderFooterAsync(marketingAttributionEnabled: boolean, path: string): Promise<FooterFixture> {
    await TestBed.configureTestingModule({
        imports: [PublicSiteFooterComponent],
        providers: [
            provideRouter([]),
            provideTranslateTesting(),
            { provide: MARKETING_ATTRIBUTION_ENABLED, useValue: marketingAttributionEnabled },
        ],
    }).compileComponents();
    const fixture = TestBed.createComponent(PublicSiteFooterComponent);
    fixture.componentRef.setInput('path', path);
    fixture.detectChanges();
    const root: unknown = fixture.nativeElement;
    if (!(root instanceof HTMLElement)) {
        throw new Error('Expected the public footer to render');
    }
    return { fixture, root };
}

function legalLinks(root: HTMLElement): Array<string | null> {
    return Array.from(root.querySelectorAll('nav a'), link => link.getAttribute('href'));
}
