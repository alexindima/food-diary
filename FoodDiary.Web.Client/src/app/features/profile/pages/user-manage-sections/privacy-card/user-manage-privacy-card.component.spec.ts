import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import { UserManagePrivacyCardComponent } from './user-manage-privacy-card.component';

describe('UserManagePrivacyCardComponent', () => {
    it('emits privacy actions', async () => {
        const fixture = await createComponentAsync({ hasAiConsent: true });
        const component = fixture.componentInstance;
        const aiConsentRevoke = vi.fn();
        const accountDelete = vi.fn();
        component.aiConsentRevoke.subscribe(aiConsentRevoke);
        component.accountDelete.subscribe(accountDelete);

        component.aiConsentRevoke.emit();
        component.accountDelete.emit();

        expect(aiConsentRevoke).toHaveBeenCalledOnce();
        expect(accountDelete).toHaveBeenCalledOnce();
    });

    it('renders inactive AI consent state without revoke action', async () => {
        const fixture = await createComponentAsync({ hasAiConsent: false });
        const element = fixture.nativeElement as HTMLElement;

        expect(element.textContent).toContain('USER_MANAGE.AI_CONSENT_INACTIVE');
        expect(element.textContent).not.toContain('USER_MANAGE.AI_CONSENT_REVOKE');
    });
});

async function createComponentAsync(overrides: Partial<PrivacyCardInputs> = {}): Promise<ComponentFixture<UserManagePrivacyCardComponent>> {
    await TestBed.configureTestingModule({
        imports: [UserManagePrivacyCardComponent, TranslateModule.forRoot()],
    }).compileComponents();

    const fixture = TestBed.createComponent(UserManagePrivacyCardComponent);
    fixture.componentRef.setInput('hasAiConsent', overrides.hasAiConsent ?? true);
    fixture.componentRef.setInput('isRevokingAiConsent', overrides.isRevokingAiConsent ?? false);
    fixture.componentRef.setInput('isDeleting', overrides.isDeleting ?? false);
    fixture.detectChanges();
    return fixture;
}

type PrivacyCardInputs = {
    hasAiConsent: boolean;
    isRevokingAiConsent: boolean;
    isDeleting: boolean;
};
