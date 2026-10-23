import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import { UserManagePrivacyCardComponent } from './user-manage-privacy-card';

describe('UserManagePrivacyCardComponent', () => {
    it('opens the privacy policy in the current interface language after a language change', async () => {
        const fixture = await createComponentAsync();
        const translate = TestBed.inject(TranslateService);
        const policyLink = (fixture.nativeElement as HTMLElement).querySelector<HTMLAnchorElement>('.user-manage__privacy-link');

        await firstValueFrom(translate.use('ru'));
        fixture.detectChanges();

        expect(policyLink?.getAttribute('href')).toBe('/ru/privacy-policy');

        await firstValueFrom(translate.use('en'));
        fixture.detectChanges();

        expect(policyLink?.getAttribute('href')).toBe('/privacy-policy');
        expect(policyLink?.target).toBe('_blank');
        expect(policyLink?.rel).toContain('noopener');
    });

    it('emits privacy actions', async () => {
        const fixture = await createComponentAsync({ hasAiConsent: true });
        const component = fixture.componentInstance;
        const aiConsentRevoke = vi.fn();
        const accountDelete = vi.fn();
        component['aiConsentRevoke'].subscribe(aiConsentRevoke);
        component['accountDelete'].subscribe(accountDelete);

        component['aiConsentRevoke'].emit();
        component['accountDelete'].emit();

        expect(aiConsentRevoke).toHaveBeenCalledOnce();
        expect(accountDelete).toHaveBeenCalledOnce();
    });

    it('renders inactive AI consent state without revoke action', async () => {
        const fixture = await createComponentAsync({ hasAiConsent: false });
        const element = fixture.nativeElement as HTMLElement;

        expect(element.textContent).toContain('USER_MANAGE.AI_CONSENT_INACTIVE');
        expect(element.textContent).not.toContain('USER_MANAGE.AI_CONSENT_REVOKE');
    });

    it('disables destructive privacy actions while account surface is busy', async () => {
        const fixture = await createComponentAsync({ isAccountActionBusy: true });
        const buttons = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll<HTMLButtonElement>('button'));

        expect(buttons.some(button => button.textContent.includes('USER_MANAGE.AI_CONSENT_REVOKE') && button.disabled)).toBe(true);
        expect(buttons.some(button => button.textContent.includes('USER_MANAGE.DELETE_ACCOUNT_BUTTON') && button.disabled)).toBe(true);
    });
});

async function createComponentAsync(overrides: Partial<PrivacyCardInputs> = {}): Promise<ComponentFixture<UserManagePrivacyCardComponent>> {
    await TestBed.configureTestingModule({
        imports: [UserManagePrivacyCardComponent],
        providers: [provideTranslateTesting()],
    }).compileComponents();

    const fixture = TestBed.createComponent(UserManagePrivacyCardComponent);
    fixture.componentRef.setInput('hasAiConsent', overrides.hasAiConsent ?? true);
    fixture.componentRef.setInput('isRevokingAiConsent', overrides.isRevokingAiConsent ?? false);
    fixture.componentRef.setInput('isDeleting', overrides.isDeleting ?? false);
    fixture.componentRef.setInput('isAccountActionBusy', overrides.isAccountActionBusy ?? false);
    fixture.detectChanges();
    return fixture;
}

type PrivacyCardInputs = {
    hasAiConsent: boolean;
    isRevokingAiConsent: boolean;
    isDeleting: boolean;
    isAccountActionBusy: boolean;
};
