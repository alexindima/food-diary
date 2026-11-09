import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { AiConsentDialogComponent } from '../../../../components/shared/ai-consent-dialog/ai-consent-dialog';
import type { UserPrivacyState } from '../../../../shared/api/user.service';
import { MARKETING_ATTRIBUTION_ENABLED } from '../../../../shared/marketing/marketing-attribution-enabled';
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
const dialogs = { open: vi.fn() };
const marketing = {
    currentDecision: signal<{ allowed: boolean } | null>(null),
    accountSyncFailed: signal(false),
    decide: vi.fn(),
};
let fixture: ComponentFixture<PrivacySettingsComponent>;

async function setupAsync(marketingAttributionEnabled = true): Promise<void> {
    vi.resetAllMocks();
    state.update(value => ({
        ...value,
        healthAcceptedAtUtc: '2026-11-02T10:00:00Z',
        healthDeclinedAtUtc: null,
        aiAcceptedAtUtc: null,
        marketingAcceptedAtUtc: null,
    }));
    privacy.loadAsync.mockResolvedValue(state());
    privacy.chooseAsync.mockResolvedValue(true);
    marketing.currentDecision.set(null);
    marketing.accountSyncFailed.set(false);
    await TestBed.configureTestingModule({
        imports: [PrivacySettingsComponent],
        providers: [
            provideRouter([]),
            provideTranslateTesting(),
            { provide: FdUiDialogService, useValue: dialogs },
            { provide: MARKETING_ATTRIBUTION_ENABLED, useValue: marketingAttributionEnabled },
            { provide: PrivacyConsentFacade, useValue: privacy },
            { provide: MarketingConsentService, useValue: marketing },
        ],
    })
        .overrideComponent(AccountDeletionOfferComponent, {
            set: { providers: [{ provide: AccountDeletionFacade, useValue: deletion }] },
        })
        .compileComponents();
    fixture = TestBed.createComponent(PrivacySettingsComponent);
    fixture.detectChanges();
}

describe('PrivacySettingsComponent marketing choices', () => {
    beforeEach(async () => setupAsync());

    it('allows marketing after a previous refusal using the existing device and account synchronization entrypoint', () => {
        marketing.currentDecision.set({ allowed: false });
        fixture.detectChanges();
        expect((fixture.nativeElement as HTMLElement).textContent).toContain('MARKETING_CONSENT.DEVICE_DECLINED');
        click('MARKETING_CONSENT.ALLOW');
        expect(marketing.decide).toHaveBeenCalledExactlyOnceWith(true);
    });

    it('withdraws marketing without changing health consent', () => {
        marketing.currentDecision.set({ allowed: true });
        fixture.detectChanges();
        expect((fixture.nativeElement as HTMLElement).textContent).toContain('MARKETING_CONSENT.DEVICE_ALLOWED');
        click('MARKETING_CONSENT.REJECT');
        expect(marketing.decide).toHaveBeenCalledExactlyOnceWith(false);
        expect(privacy.chooseAsync).not.toHaveBeenCalled();
    });

    it('keeps the account sync failure visible with both choices available for retry', () => {
        marketing.accountSyncFailed.set(true);
        fixture.detectChanges();
        const root = fixture.nativeElement as HTMLElement;
        expect(root.textContent).toContain('MARKETING_CONSENT.SYNC_ERROR');
        expect(Array.from(root.querySelectorAll('button')).some(button => button.textContent.includes('MARKETING_CONSENT.ALLOW'))).toBe(
            true,
        );
    });
});

it.each([true, false])('only saves AI consent after explicit confirmation (%s)', async accepted => {
    await setupAsync();
    dialogs.open.mockReturnValue({ afterClosed: () => of(accepted) });

    click('PRIVACY_CHOICES.REVIEW');
    await fixture.whenStable();

    expect(dialogs.open).toHaveBeenCalledWith(AiConsentDialogComponent, { size: 'lg' });
    if (accepted) {
        expect(privacy.chooseAsync).toHaveBeenCalledExactlyOnceWith(2, true);
    } else {
        expect(privacy.chooseAsync).not.toHaveBeenCalled();
    }
});

it('allows withdrawal of existing marketing consent while the feature is disabled', async () => {
    await setupAsync(false);
    const root = fixture.nativeElement as HTMLElement;
    expect(root.textContent).not.toContain('MARKETING_CONSENT.TITLE');

    state.update(value => ({ ...value, marketingAcceptedAtUtc: '2026-11-02T10:00:00Z' }));
    fixture.detectChanges();

    expect(root.textContent).not.toContain('MARKETING_CONSENT.ALLOW');
    click('MARKETING_CONSENT.REJECT');
    expect(marketing.decide).toHaveBeenCalledExactlyOnceWith(false);
});

function click(label: string): void {
    const buttons = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll<HTMLButtonElement>('button'));
    const button = buttons.find(value => value.textContent.includes(label));
    expect(button, label).toBeDefined();
    button?.click();
    fixture.detectChanges();
}

describe('PrivacySettingsComponent health withdrawal', () => {
    beforeEach(async () => setupAsync());

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
