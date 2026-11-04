import { TestBed } from '@angular/core/testing';
import { afterEach, describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import { MARKETING_ATTRIBUTION_ENABLED } from '../../../shared/marketing/marketing-attribution-enabled';
import { MarketingConsentService } from '../../../shared/marketing/marketing-consent.service';
import { BrowserStorageService } from '../../../shared/platform/browser-storage.service';
import { MarketingConsentComponent } from './marketing-consent';

describe('MarketingConsentComponent choices', () => {
    afterEach(() => TestBed.resetTestingModule());

    it.each([false, true])('removes the entire notice after choosing allowed=%s and preserves the answer when reopened', async allowed => {
        const values = new Map<string, unknown>();
        TestBed.configureTestingModule({
            imports: [MarketingConsentComponent],
            providers: [
                provideTranslateTesting(),
                { provide: MARKETING_ATTRIBUTION_ENABLED, useValue: true },
                {
                    provide: BrowserStorageService,
                    useValue: {
                        getJson: (_scope: string, key: string): unknown => values.get(key) ?? null,
                        setJson: (_scope: string, key: string, value: unknown): void => {
                            values.set(key, value);
                        },
                        removeItem: (_scope: string, key: string): void => {
                            values.delete(key);
                        },
                    },
                },
            ],
        });
        const fixture = TestBed.createComponent(MarketingConsentComponent);
        fixture.detectChanges();
        await fixture.whenStable();
        fixture.detectChanges();
        const root = fixture.nativeElement as HTMLElement;
        const buttons = root.querySelectorAll<HTMLButtonElement>('button');
        expect(buttons).toHaveLength(2);
        buttons[allowed ? 1 : 0].click();
        fixture.detectChanges();
        expect(TestBed.inject(MarketingConsentService).currentDecision()?.allowed).toBe(allowed);
        expect(root.querySelector('button'), 'A saved choice must leave no global consent settings button').toBeNull();
        fixture.destroy();
        const reopened = TestBed.createComponent(MarketingConsentComponent);
        reopened.detectChanges();
        await reopened.whenStable();
        reopened.detectChanges();
        expect((reopened.nativeElement as HTMLElement).querySelector('button')).toBeNull();
        expect(values.has('fd_marketing_consent')).toBe(true);
    });
});
