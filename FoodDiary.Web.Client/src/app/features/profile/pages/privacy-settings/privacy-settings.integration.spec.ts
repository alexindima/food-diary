import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { of, Subject, throwError } from 'rxjs';
import { afterEach, describe, expect, it, type Mock, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { AuthService } from '../../../../services/auth.service';
import { ExportService } from '../../../../shared/api/export.service';
import { type UserPrivacyState, UserService } from '../../../../shared/api/user.service';
import { MARKETING_ATTRIBUTION_ENABLED } from '../../../../shared/marketing/marketing-attribution-enabled';
import { MarketingConsentService } from '../../../../shared/marketing/marketing-consent.service';
import { BrowserStorageService } from '../../../../shared/platform/browser-storage.service';
import { PrivacyConsentPurpose } from '../../../../shared/privacy/privacy-consent-purpose';
import { PRIVACY_NOTICE_VERSION } from '../../../../shared/privacy/privacy-notice-version';
import { PrivacySettingsComponent } from './privacy-settings';

const ACCEPTED_AT = '2026-11-02T10:00:00Z';
const INITIAL_STATE: UserPrivacyState = {
    noticeVersion: PRIVACY_NOTICE_VERSION,
    healthAcceptedAtUtc: ACCEPTED_AT,
    healthDeclinedAtUtc: null,
    aiAcceptedAtUtc: null,
    marketingAcceptedAtUtc: null,
};
const HEALTH_CARD = '.privacy-settings__choices > fd-ui-card:first-of-type';
const AI_CARD = '.privacy-settings__choices > fd-ui-card:last-of-type';
const AI_DIALOG = 'fd-ai-consent-dialog';
const HEALTH_DIALOG = 'fd-health-consent-dialog';
const DELETE_DIALOG = 'fd-confirm-delete-dialog';
const ACCOUNT_ACTIONS = 'fd-account-deletion-offer';

afterEach(() => {
    TestBed.inject(FdUiDialogService).closeAll();
    vi.unstubAllGlobals();
});

type PrivacyFixture = {
    getPrivacy: Mock<UserService['getPrivacy']>;
    updatePrivacy: Mock<UserService['updatePrivacy']>;
    deleteCurrentUser: Mock<UserService['deleteCurrentUser']>;
    exportPersonalData: Mock<ExportService['exportPersonalData']>;
    onLogoutAsync: Mock<AuthService['onLogoutAsync']>;
};

function configurePrivacy(initialState = INITIAL_STATE, hasCurrentUser = true): PrivacyFixture {
    vi.stubGlobal(
        'matchMedia',
        vi.fn(() => ({ matches: false })),
    );
    let state = initialState;
    const getPrivacy = vi.fn<UserService['getPrivacy']>().mockImplementation(() => of(state));
    const updatePrivacy = vi.fn<UserService['updatePrivacy']>().mockImplementation((purpose, granted) => {
        if (purpose === PrivacyConsentPurpose.HealthTracking) {
            state = {
                ...state,
                healthAcceptedAtUtc: granted ? ACCEPTED_AT : null,
                healthDeclinedAtUtc: granted ? null : ACCEPTED_AT,
            };
        }
        if (purpose === PrivacyConsentPurpose.AiProcessing) {
            state = { ...state, aiAcceptedAtUtc: granted ? ACCEPTED_AT : null };
        }
        return of(undefined);
    });
    const deleteCurrentUser = vi.fn<UserService['deleteCurrentUser']>().mockReturnValue(of(true));
    const exportPersonalData = vi.fn<ExportService['exportPersonalData']>().mockReturnValue(of(undefined));
    const onLogoutAsync = vi.fn<AuthService['onLogoutAsync']>().mockResolvedValue(undefined);
    TestBed.configureTestingModule({
        providers: [
            provideRouter([{ path: 'privacy-settings', component: PrivacySettingsComponent }]),
            provideTranslateTesting(),
            {
                provide: UserService,
                useValue: { user: signal(hasCurrentUser ? { id: 'privacy-owner' } : null), getPrivacy, updatePrivacy, deleteCurrentUser },
            },
            { provide: AuthService, useValue: { onLogoutAsync, isAuthenticated: (): boolean => true } },
            { provide: ExportService, useValue: { exportPersonalData } },
            { provide: MARKETING_ATTRIBUTION_ENABLED, useValue: false },
            {
                provide: MarketingConsentService,
                useValue: { changed: new Subject<boolean>(), currentDecision: signal(null), accountSyncFailed: signal(false) },
            },
            { provide: BrowserStorageService, useValue: { getItem: (): null => null } },
        ],
    });
    return { getPrivacy, updatePrivacy, deleteCurrentUser, exportPersonalData, onLogoutAsync };
}

async function renderPrivacyAsync(): Promise<RouterTestingHarness> {
    const harness = await RouterTestingHarness.create('/privacy-settings');
    await redrawAsync(harness);
    return harness;
}

async function redrawAsync(harness: RouterTestingHarness): Promise<void> {
    await harness.fixture.whenStable();
    harness.detectChanges();
}

function renderedSurface(parent: ParentNode, selector: string): HTMLElement {
    const surface = parent.querySelector(selector);
    if (!(surface instanceof HTMLElement)) {
        throw new Error(`Expected rendered privacy surface ${selector}`);
    }
    return surface;
}

function renderedButton(parent: ParentNode, label: string): HTMLButtonElement {
    const match = Array.from(parent.querySelectorAll('button')).find(
        button => button.querySelector('.fd-ui-button__label')?.textContent.trim() === label,
    );
    if (match === undefined) {
        throw new Error(`Expected rendered privacy action ${label}`);
    }
    return match;
}

async function chooseDeleteActionAsync(harness: RouterTestingHarness, label: string): Promise<void> {
    const action = await vi.waitFor(() => {
        harness.detectChanges();
        return renderedButton(renderedSurface(document, DELETE_DIALOG), label);
    });
    action.click();
    await redrawAsync(harness);
}

function agree(dialog: HTMLElement, selector: string): void {
    const checkbox = dialog.querySelector(selector);
    if (!(checkbox instanceof HTMLInputElement)) {
        throw new Error(`Expected privacy agreement ${selector}`);
    }
    checkbox.click();
    expect(checkbox.checked).toBe(true);
}

describe('Routed privacy consent choices', () => {
    it('recovers from the initial read error and grants health consent after native review', async () => {
        const { getPrivacy, updatePrivacy } = configurePrivacy({ ...INITIAL_STATE, healthAcceptedAtUtc: null });
        getPrivacy.mockReturnValueOnce(throwError(() => new Error('Privacy settings unavailable')));
        const harness = await renderPrivacyAsync();
        const page = renderedSurface(document, 'fd-privacy-settings');
        expect(page.textContent).toContain('PRIVACY_CHOICES.ERROR');

        renderedButton(renderedSurface(page, HEALTH_CARD), 'PRIVACY_CHOICES.REVIEW').click();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(document.querySelector(HEALTH_DIALOG)).not.toBeNull();
        });
        const dialog = renderedSurface(document, HEALTH_DIALOG);
        const accept = renderedButton(dialog, 'PRIVACY_CHOICES.ACCEPT');
        expect(accept.disabled).toBe(true);
        accept.click();
        expect(updatePrivacy).not.toHaveBeenCalled();

        agree(dialog, '#health-consent-agreement');
        await redrawAsync(harness);
        expect(accept.disabled).toBe(false);
        accept.click();
        await redrawAsync(harness);

        expect(updatePrivacy).toHaveBeenCalledExactlyOnceWith(PrivacyConsentPurpose.HealthTracking, true, PRIVACY_NOTICE_VERSION);
        expect(page.textContent).not.toContain('PRIVACY_CHOICES.ERROR');
        expect(renderedSurface(page, HEALTH_CARD).textContent).toContain('PRIVACY_CHOICES.HEALTH_ALLOWED');
        expect(renderedSurface(page, AI_CARD).textContent).toContain('PRIVACY_CHOICES.AI_OPTIONAL');
    });

    it('keeps one AI review open during rapid repeated activation and allows a fresh review after canceling', async () => {
        const { updatePrivacy } = configurePrivacy();
        const harness = await renderPrivacyAsync();
        const page = renderedSurface(document, 'fd-privacy-settings');
        const review = renderedButton(renderedSurface(page, AI_CARD), 'PRIVACY_CHOICES.REVIEW');
        review.click();
        review.click();
        await redrawAsync(harness);

        expect(document.querySelectorAll(AI_DIALOG)).toHaveLength(1);
        renderedButton(renderedSurface(document, AI_DIALOG), 'AI_CONSENT.CANCEL').click();
        await redrawAsync(harness);
        expect(updatePrivacy).not.toHaveBeenCalled();
        expect(document.querySelector(AI_DIALOG)).toBeNull();

        review.click();
        await redrawAsync(harness);
        expect(document.querySelectorAll(AI_DIALOG)).toHaveLength(1);
    });

    it('applies a confirmed AI choice and allows withdrawal independently of the granted health choice', async () => {
        const { updatePrivacy } = configurePrivacy();
        const harness = await renderPrivacyAsync();
        const page = renderedSurface(document, 'fd-privacy-settings');
        renderedButton(renderedSurface(page, AI_CARD), 'PRIVACY_CHOICES.REVIEW').click();
        await redrawAsync(harness);
        const dialog = renderedSurface(document, AI_DIALOG);
        const accept = renderedButton(dialog, 'AI_CONSENT.ACCEPT');
        expect(accept.disabled).toBe(true);
        agree(dialog, '#ai-consent-agree');
        await redrawAsync(harness);
        expect(updatePrivacy).not.toHaveBeenCalled();
        accept.click();
        await redrawAsync(harness);

        expect(updatePrivacy).toHaveBeenCalledExactlyOnceWith(PrivacyConsentPurpose.AiProcessing, true, PRIVACY_NOTICE_VERSION);
        expect(renderedSurface(page, AI_CARD).textContent).toContain('PRIVACY_CHOICES.AI_ALLOWED');
        renderedButton(renderedSurface(page, AI_CARD), 'PRIVACY_CHOICES.WITHDRAW').click();
        await redrawAsync(harness);

        expect(updatePrivacy).toHaveBeenLastCalledWith(PrivacyConsentPurpose.AiProcessing, false, PRIVACY_NOTICE_VERSION);
        expect(renderedSurface(page, AI_CARD).textContent).toContain('PRIVACY_CHOICES.AI_OPTIONAL');
        expect(renderedSurface(page, HEALTH_CARD).textContent).toContain('PRIVACY_CHOICES.HEALTH_ALLOWED');
    });
});

describe('Routed privacy account actions', () => {
    it('blocks account deletion during an export and lets that export be retried after failure', async () => {
        const { deleteCurrentUser, exportPersonalData } = configurePrivacy();
        const pending = new Subject<void>();
        exportPersonalData.mockReturnValueOnce(pending);
        const harness = await renderPrivacyAsync();
        const page = renderedSurface(document, 'fd-privacy-settings');
        const actions = renderedSurface(page, ACCOUNT_ACTIONS);
        const download = renderedButton(actions, 'PRIVACY_CHOICES.DOWNLOAD');
        const remove = renderedButton(actions, 'PRIVACY_CHOICES.DELETE_ACCOUNT');
        download.click();
        await redrawAsync(harness);

        expect(download.getAttribute('aria-busy')).toBe('true');
        expect(remove.disabled).toBe(true);
        remove.click();
        expect(document.querySelector(DELETE_DIALOG)).toBeNull();
        expect(deleteCurrentUser).not.toHaveBeenCalled();

        pending.error(new Error('Export unavailable'));
        await redrawAsync(harness);
        expect(page.textContent).toContain('PRIVACY_CHOICES.ERROR');
        expect(download.disabled).toBe(false);
        expect(remove.disabled).toBe(false);
        download.click();
        await redrawAsync(harness);

        expect(exportPersonalData).toHaveBeenCalledTimes(2);
        expect(page.textContent).not.toContain('PRIVACY_CHOICES.ERROR');
    });

    it('cancels a delete prompt, exposes a failed confirmed delete, and logs out only after successful retry', async () => {
        const { deleteCurrentUser, onLogoutAsync } = configurePrivacy();
        const pending = new Subject<boolean>();
        deleteCurrentUser.mockReturnValueOnce(pending);
        const harness = await renderPrivacyAsync();
        const actions = renderedSurface(document, ACCOUNT_ACTIONS);
        const remove = renderedButton(actions, 'PRIVACY_CHOICES.DELETE_ACCOUNT');
        remove.click();
        await chooseDeleteActionAsync(harness, 'COMMON.CANCEL');
        expect(deleteCurrentUser).not.toHaveBeenCalled();
        expect(onLogoutAsync).not.toHaveBeenCalled();

        remove.click();
        await chooseDeleteActionAsync(harness, 'PRIVACY_CHOICES.DELETE_ACCOUNT');
        expect(remove.getAttribute('aria-busy')).toBe('true');
        expect(deleteCurrentUser).toHaveBeenCalledOnce();
        expect(onLogoutAsync).not.toHaveBeenCalled();

        pending.next(false);
        pending.complete();
        await redrawAsync(harness);
        expect(actions.textContent).toContain('USER_MANAGE.DELETE_ACCOUNT_ERROR');
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(remove.disabled).toBe(false);
        });
        expect(onLogoutAsync).not.toHaveBeenCalled();
        remove.click();
        await chooseDeleteActionAsync(harness, 'PRIVACY_CHOICES.DELETE_ACCOUNT');

        expect(deleteCurrentUser).toHaveBeenCalledTimes(2);
        expect(onLogoutAsync).toHaveBeenCalledExactlyOnceWith(true);
        expect(actions.textContent).not.toContain('USER_MANAGE.DELETE_ACCOUNT_ERROR');
    });

    it('shows a deletion error if there is no account identity and opens no confirmation', async () => {
        const { deleteCurrentUser, onLogoutAsync } = configurePrivacy(INITIAL_STATE, false);
        const harness = await renderPrivacyAsync();
        const actions = renderedSurface(document, ACCOUNT_ACTIONS);
        renderedButton(actions, 'PRIVACY_CHOICES.DELETE_ACCOUNT').click();
        await redrawAsync(harness);

        expect(actions.textContent).toContain('USER_MANAGE.DELETE_ACCOUNT_ERROR');
        expect(document.querySelector(DELETE_DIALOG)).toBeNull();
        expect(deleteCurrentUser).not.toHaveBeenCalled();
        expect(onLogoutAsync).not.toHaveBeenCalled();
    });
});
