import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogRef } from 'fd-ui-kit';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { firstValueFrom } from 'rxjs';
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

    it('updates the policy link and meal-save action to Russian while preserving explicit checkbox consent', async () => {
        const close = vi.fn();
        await TestBed.configureTestingModule({
            imports: [HealthConsentDialogComponent],
            providers: [
                provideTranslateTesting(),
                { provide: FdUiDialogRef, useValue: { close } },
                { provide: FD_UI_DIALOG_DATA, useValue: { acceptLabelKey: 'PRIVACY_CHOICES.ACCEPT_AND_SAVE' } },
            ],
        }).compileComponents();
        const translate = TestBed.inject(TranslateService);
        translate.setTranslation('en', { PRIVACY_CHOICES: { ACCEPT_AND_SAVE: 'Accept and save' } });
        translate.setTranslation('ru', { PRIVACY_CHOICES: { ACCEPT_AND_SAVE: 'Принять и сохранить' } });
        await firstValueFrom(translate.use('en'));
        const fixture = TestBed.createComponent(HealthConsentDialogComponent);
        fixture.detectChanges();
        const root = fixture.nativeElement as HTMLElement;
        const policy = root.querySelector('a');
        const agreement = root.querySelector<HTMLInputElement>('input[type="checkbox"]');
        const accept = [...root.querySelectorAll('button')].find(button => button.textContent.trim() === 'Accept and save');
        if (policy === null || agreement === null || accept === undefined) {
            throw new Error('Expected the rendered health consent controls');
        }
        expect(policy.getAttribute('href')).toBe('/privacy-policy');

        await firstValueFrom(translate.use('ru'));
        fixture.detectChanges();

        expect(policy.getAttribute('href')).toBe('/ru/privacy-policy');
        expect(policy.getAttribute('target')).toBe('_blank');
        expect(policy.getAttribute('rel')).toBe('noopener noreferrer');
        expect(accept.textContent.trim()).toBe('Принять и сохранить');

        agreement.click();
        fixture.detectChanges();
        expect(accept.disabled).toBe(false);
        agreement.click();
        fixture.detectChanges();
        expect(accept.disabled).toBe(true);
        accept.click();
        expect(close).not.toHaveBeenCalled();

        agreement.click();
        fixture.detectChanges();
        accept.click();
        expect(close).toHaveBeenCalledExactlyOnceWith(true);
    });
});
