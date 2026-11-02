import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import type { UserPrivacyState } from '../../../../shared/api/user.service';
import { MarketingConsentService } from '../../../../shared/marketing/marketing-consent.service';
import { PrivacyConsentFacade } from '../../../../shared/privacy/privacy-consent.facade';
import { PRIVACY_NOTICE_VERSION } from '../../../../shared/privacy/privacy-notice-version';
import { AccountDeletionOfferComponent } from '../../components/account-deletion-offer/account-deletion-offer';
import { AccountDeletionFacade } from '../../lib/account-deletion.facade';
import { PrivacySettingsComponent } from './privacy-settings';

const state = signal<UserPrivacyState>({
    noticeVersion: PRIVACY_NOTICE_VERSION,
    healthAcceptedAtUtc: '2026-11-02T10:00:00Z',
    healthDeclinedAtUtc: null,
    aiAcceptedAtUtc: null,
    marketingAcceptedAtUtc: null,
});
const privacy = {
    state,
    busy: signal(false),
    failed: signal(false),
    needsReload: signal(false),
    loadAsync: vi.fn(),
    chooseAsync: vi.fn(),
    downloadAsync: vi.fn(),
    ensureHealthConsentAsync: vi.fn(),
};
const deletion = { busy: signal(false), failed: signal(false), confirmAsync: vi.fn() };
let fixture: ComponentFixture<PrivacySettingsComponent>;

beforeEach(async () => {
    vi.resetAllMocks();
    state.update(value => ({ ...value, healthAcceptedAtUtc: '2026-11-02T10:00:00Z', healthDeclinedAtUtc: null }));
    privacy.loadAsync.mockResolvedValue(state());
    privacy.chooseAsync.mockResolvedValue(true);
    await TestBed.configureTestingModule({
        imports: [PrivacySettingsComponent],
        providers: [
            provideRouter([]),
            provideTranslateTesting(),
            { provide: PrivacyConsentFacade, useValue: privacy },
            { provide: MarketingConsentService, useValue: { accountSyncFailed: signal(false), decide: vi.fn() } },
        ],
    })
        .overrideComponent(AccountDeletionOfferComponent, {
            set: { providers: [{ provide: AccountDeletionFacade, useValue: deletion }] },
        })
        .compileComponents();
    fixture = TestBed.createComponent(PrivacySettingsComponent);
    fixture.detectChanges();
});

function click(label: string): void {
    const buttons = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll<HTMLButtonElement>('button'));
    const button = buttons.find(value => value.textContent.includes(label));
    expect(button, label).toBeDefined();
    button?.click();
    fixture.detectChanges();
}

describe('PrivacySettingsComponent health withdrawal', () => {
    it('offers a copy and full account deletion after withdrawal without deleting automatically', () => {
        expect((fixture.nativeElement as HTMLElement).textContent).not.toContain('PRIVACY_CHOICES.DELETE_OFFER_TITLE');
        click('PRIVACY_CHOICES.WITHDRAW');
        expect(privacy.chooseAsync).toHaveBeenCalledWith(1, false);
        state.update(value => ({ ...value, healthAcceptedAtUtc: null, healthDeclinedAtUtc: '2026-11-02T11:00:00Z' }));
        fixture.detectChanges();
        expect((fixture.nativeElement as HTMLElement).textContent).toContain('PRIVACY_CHOICES.DELETE_OFFER_TITLE');
        expect(deletion.confirmAsync).not.toHaveBeenCalled();
        click('PRIVACY_CHOICES.DOWNLOAD');
        expect(privacy.downloadAsync).toHaveBeenCalledOnce();
        click('PRIVACY_CHOICES.DELETE_ACCOUNT');
        expect(deletion.confirmAsync).toHaveBeenCalledOnce();
        expect(privacy.chooseAsync).toHaveBeenCalledExactlyOnceWith(1, false);
    });

    it('retains the deletion offer when reopening settings after a previous refusal', () => {
        state.update(value => ({ ...value, healthAcceptedAtUtc: null, healthDeclinedAtUtc: '2026-11-02T11:00:00Z' }));
        fixture.detectChanges();
        expect((fixture.nativeElement as HTMLElement).textContent).toContain('PRIVACY_CHOICES.DELETE_OFFER_TITLE');
        expect(deletion.confirmAsync).not.toHaveBeenCalled();
    });
});
