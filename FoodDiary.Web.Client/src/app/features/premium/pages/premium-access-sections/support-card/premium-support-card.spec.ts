import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import { PremiumSupportCardComponent } from './premium-support-card';

describe('Subscription terms disclosure', () => {
    it('keeps legal details out of the initial view and exposes them through a keyboard-accessible button', async () => {
        const fixture = await renderAsync();
        const host = fixture.nativeElement as HTMLElement;
        const toggle = requireElement(host, 'button');
        const content = requireElement(host, '#premium-subscription-terms');

        expect(toggle.getAttribute('aria-expanded')).toBe('false');
        expect(toggle.getAttribute('aria-controls')).toBe(content.id);
        expect(content.hidden).toBe(true);
        toggle.click();
        fixture.detectChanges();
        expect(toggle.getAttribute('aria-expanded')).toBe('true');
        expect(content.hidden).toBe(false);
        expect(content.textContent).toContain('PREMIUM_PAGE.STARS.TERMS_DETAIL');
        expect(content.querySelector('a')?.getAttribute('href')).toMatch(/^mailto:/u);
        toggle.click();
        fixture.detectChanges();
        expect(content.hidden).toBe(true);
    });

    it('opens the terms when requested from checkout and focuses their disclosure control', async () => {
        const fixture = await renderAsync();
        const host = fixture.nativeElement as HTMLElement;

        fixture.componentInstance.expanded.set(true);
        fixture.detectChanges();

        expect(requireElement(host, 'button').getAttribute('aria-expanded')).toBe('true');
        expect(host.querySelector<HTMLElement>('#premium-subscription-terms')?.hidden).toBe(false);
        expect(document.activeElement).toBe(host.querySelector('button'));
    });
});

async function renderAsync(): Promise<ComponentFixture<PremiumSupportCardComponent>> {
    await TestBed.configureTestingModule({
        imports: [PremiumSupportCardComponent],
        providers: [provideTranslateTesting()],
    }).compileComponents();
    const fixture = TestBed.createComponent(PremiumSupportCardComponent);
    fixture.detectChanges();
    return fixture;
}

function requireElement(host: HTMLElement, selector: string): HTMLElement {
    const element = host.querySelector<HTMLElement>(selector);
    if (element === null) {
        throw new Error(`Expected element: ${selector}`);
    }
    return element;
}
