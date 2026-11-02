import { TestBed } from '@angular/core/testing';
import { FdUiDialogRef } from 'fd-ui-kit';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import { HealthConsentDialogComponent } from './health-consent-dialog';

describe('HealthConsentDialogComponent', () => {
    it('starts unchecked, requires an explicit check, and always permits refusal', async () => {
        const close = vi.fn();
        await TestBed.configureTestingModule({
            imports: [HealthConsentDialogComponent],
            providers: [provideTranslateTesting(), { provide: FdUiDialogRef, useValue: { close } }],
        }).compileComponents();
        const fixture = TestBed.createComponent(HealthConsentDialogComponent);
        fixture.detectChanges();
        const root = fixture.nativeElement as HTMLElement;
        const checkbox = root.querySelector<HTMLInputElement>('input[type="checkbox"]');
        const buttons = [...root.querySelectorAll('button')];
        const accept = buttons.at(-1);
        const decline = buttons.find(button => button !== accept && button.textContent.includes('PRIVACY_CHOICES.DECLINE'));
        if (checkbox === null || accept === undefined || decline === undefined) {
            throw new Error('Consent controls missing');
        }
        expect(checkbox.checked).toBe(false);
        expect(accept.disabled).toBe(true);
        accept.click();
        expect(close).not.toHaveBeenCalled();
        checkbox.click();
        fixture.detectChanges();
        expect(accept.disabled).toBe(false);
        accept.click();
        expect(close).toHaveBeenLastCalledWith(true);
        decline.click();
        expect(close).toHaveBeenLastCalledWith(false);
    });
});
