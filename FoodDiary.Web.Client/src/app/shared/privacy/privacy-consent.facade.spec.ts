import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { FdUiDialogService } from 'fd-ui-kit';
import { type Observable, of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../testing/async-testing';
import { AuthService } from '../../services/auth.service';
import { ExportService } from '../api/export.service';
import { type UserPrivacyState, UserService } from '../api/user.service';
import { SessionEventsService } from '../auth/session-events.service';
import { MarketingConsentService } from '../marketing/marketing-consent.service';
import { BrowserStorageService } from '../platform/browser-storage.service';
import { isHealthConsentRequiredError } from './health-consent-error';
import { PrivacyConsentFacade } from './privacy-consent.facade';
import { PrivacyConsentPurpose } from './privacy-consent-purpose';
import { PRIVACY_NOTICE_VERSION } from './privacy-notice-version';

const emptyState: UserPrivacyState = {
    noticeVersion: PRIVACY_NOTICE_VERSION,
    healthAcceptedAtUtc: null,
    healthDeclinedAtUtc: null,
    aiAcceptedAtUtc: null,
    marketingAcceptedAtUtc: null,
};

const users = {
    getPrivacy: vi.fn<() => Observable<UserPrivacyState>>(),
    updatePrivacy: vi.fn<(purpose: number, granted: boolean, noticeVersion: string) => Observable<void>>(),
    user: vi.fn<() => { id: string } | null>(),
};
const dialogs = { open: vi.fn() };
let service: PrivacyConsentFacade;
let sessions: SessionEventsService;
let marketingChanges: Subject<boolean>;
const authenticated = vi.fn<() => boolean>();

beforeEach(() => {
    vi.resetAllMocks();
    authenticated.mockReturnValue(false);
    marketingChanges = new Subject<boolean>();
    users.getPrivacy.mockReturnValue(of(emptyState));
    users.updatePrivacy.mockImplementation((_purpose: number, granted: boolean) => {
        users.getPrivacy.mockReturnValue(of({ ...emptyState, healthAcceptedAtUtc: granted ? '2026-11-02T10:00:00Z' : null }));
        return of(undefined);
    });
    users.user.mockReturnValue(null);
    dialogs.open.mockReturnValue({ afterClosed: (): Observable<boolean> => of(true) });
    TestBed.configureTestingModule({
        providers: [
            { provide: UserService, useValue: users },
            { provide: ExportService, useValue: { exportPersonalData: (): Observable<undefined> => of(undefined) } },
            { provide: FdUiDialogService, useValue: dialogs },
            { provide: AuthService, useValue: { isAuthenticated: authenticated } },
            { provide: MarketingConsentService, useValue: { changed: marketingChanges, accountSyncFailed: signal(false) } },
            { provide: BrowserStorageService, useValue: { getItem: (): null => null, setItem: vi.fn(), removeItem: vi.fn() } },
        ],
    });
    service = TestBed.inject(PrivacyConsentFacade);
    sessions = TestBed.inject(SessionEventsService);
});

describe('PrivacyConsentFacade', () => {
    it('shares one dialog and one recorded consent across simultaneous health actions', async () => {
        const first = service.ensureHealthConsentAsync(true);
        const second = service.ensureHealthConsentAsync(true);

        expect(await Promise.all([first, second])).toEqual([true, true]);
        expect(dialogs.open).toHaveBeenCalledOnce();
        expect(users.updatePrivacy).toHaveBeenCalledOnce();
    });

    it('never records consent to a server notice the client has not shown', async () => {
        users.getPrivacy.mockReturnValue(of({ ...emptyState, noticeVersion: 'future-notice' }));
        expect(await service.ensureHealthConsentAsync()).toBe(false);
        expect(dialogs.open).not.toHaveBeenCalled();
        expect(users.updatePrivacy).not.toHaveBeenCalled();
    });

    it('permits withdrawal when the server notice is newer', async () => {
        users.getPrivacy.mockReturnValue(of({ ...emptyState, noticeVersion: 'future-notice' }));
        expect(await service.chooseAsync(1, false)).toBe(true);
        expect(users.updatePrivacy).toHaveBeenCalledWith(1, false, PRIVACY_NOTICE_VERSION);
    });

    it('does not reopen the dialog after a previous refusal until explicitly requested', async () => {
        users.getPrivacy.mockReturnValue(of({ ...emptyState, healthDeclinedAtUtc: '2026-11-02T10:00:00Z' }));
        expect(await service.ensureHealthConsentAsync()).toBe(false);
        expect(dialogs.open).not.toHaveBeenCalled();
        expect(await service.ensureHealthConsentAsync(true)).toBe(true);
        expect(dialogs.open).toHaveBeenCalledOnce();
        expect(users.updatePrivacy).toHaveBeenCalledWith(1, true, PRIVACY_NOTICE_VERSION);
    });

    it('does not resume the old consent action after an account switch', async () => {
        const response = new Subject<void>();
        users.updatePrivacy.mockReturnValue(response);
        const pending = service.chooseAsync(1, true);
        await vi.waitFor(() => {
            expect(users.updatePrivacy).toHaveBeenCalled();
        });
        sessions.notifyAuthenticated();
        response.next();
        response.complete();
        expect(await pending).toBe(false);
        expect(service.state()).toBeNull();
    });

    it('discards queued marketing choices from the previous account', async () => {
        authenticated.mockReturnValue(true);
        const response = new Subject<void>();
        users.updatePrivacy.mockReturnValueOnce(response);
        marketingChanges.next(true);
        await vi.waitFor(() => {
            expect(users.updatePrivacy).toHaveBeenCalledTimes(1);
        });
        marketingChanges.next(false);
        sessions.notifyAuthenticated();
        response.next();
        response.complete();
        // A choice made in the replacement session must follow the discarded one.
        marketingChanges.next(true);
        await vi.waitFor(() => {
            expect(users.updatePrivacy).toHaveBeenCalledTimes(2);
        });
        expect(users.updatePrivacy.mock.calls.map(call => call[1])).toEqual([true, true]);
    });
});

describe('privacy export and pending withdrawal lifecycle', () => {
    it('reports a failed private export and lets the same account retry', async () => {
        const exportData = vi.spyOn(TestBed.inject(ExportService), 'exportPersonalData');
        const result = new Subject<undefined>();
        exportData.mockReturnValueOnce(result);

        const pending = service.downloadAsync();
        expect(service.busy()).toBe(true);
        result.error(new Error('Export unavailable'));
        await pending;
        expect(service.failed()).toBe(true);
        expect(service.busy()).toBe(false);

        await service.downloadAsync();
        expect(exportData).toHaveBeenCalledTimes(2);
        expect(service.failed()).toBe(false);
        expect(service.busy()).toBe(false);
    });

    it('does not surface an old export failure after signing in again', async () => {
        const result = new Subject<undefined>();
        vi.spyOn(TestBed.inject(ExportService), 'exportPersonalData').mockReturnValueOnce(result);

        const pending = service.downloadAsync();
        sessions.notifyAuthenticated();
        result.error(new Error('Old export unavailable'));
        await pending;

        expect(service.failed()).toBe(false);
        expect(service.busy()).toBe(false);
    });

    it('retains an offline withdrawal and clears the exact account marker after retry', async () => {
        users.user.mockReturnValue({ id: 'marketing-owner' });
        const storage = TestBed.inject(BrowserStorageService);
        const key = 'fd_marketing_withdrawal:marketing-owner';
        const read = vi.spyOn(storage, 'getItem').mockImplementation((_area, candidate) => (candidate === key ? 'pending' : null));
        const pending = vi.spyOn(storage, 'setItem');
        const clear = vi.spyOn(storage, 'removeItem');
        users.updatePrivacy.mockReturnValueOnce(throwError(() => new Error('Consent endpoint unavailable')));
        const marketing = TestBed.inject(MarketingConsentService);

        await service.flushPendingWithdrawalAsync();
        expect(marketing.accountSyncFailed()).toBe(true);
        expect(pending).toHaveBeenCalledWith('local', key, 'pending');
        expect(clear).not.toHaveBeenCalled();

        await service.flushPendingWithdrawalAsync();

        expect(read).toHaveBeenCalledWith('local', key);
        expect(users.updatePrivacy).toHaveBeenLastCalledWith(PrivacyConsentPurpose.MarketingAttribution, false, PRIVACY_NOTICE_VERSION);
        expect(clear).toHaveBeenCalledExactlyOnceWith('local', key);
        expect(marketing.accountSyncFailed()).toBe(false);
    });

    it('does not mark the new account as failed when an old withdrawal returns', async () => {
        users.user.mockReturnValue({ id: 'old-account' });
        vi.spyOn(TestBed.inject(BrowserStorageService), 'getItem').mockReturnValueOnce('pending');
        const result = new Subject<void>();
        users.updatePrivacy.mockReturnValueOnce(result);
        const marketing = TestBed.inject(MarketingConsentService);
        const pending = service.flushPendingWithdrawalAsync();
        await vi.waitFor(() => {
            expect(users.updatePrivacy).toHaveBeenCalledOnce();
        });

        users.user.mockReturnValue({ id: 'new-account' });
        sessions.notifyAuthenticated();
        result.next();
        result.complete();
        await pending;

        expect(marketing.accountSyncFailed()).toBe(false);
        expect(service.state()).toBeNull();
    });
});

describe('privacy review ownership', () => {
    it('closes the old health notice on an account switch and ignores the old response', async () => {
        const result = new Subject<boolean>();
        const close = vi.fn();
        dialogs.open.mockReturnValueOnce({ close, afterClosed: (): Observable<boolean> => result });
        const pending = service.ensureHealthConsentAsync();
        await vi.waitFor(() => {
            expect(dialogs.open).toHaveBeenCalledOnce();
        });

        sessions.notifyAuthenticated();
        result.next(true);
        result.complete();

        await expect(pending).resolves.toBe(false);
        expect(close).toHaveBeenCalledOnce();
        expect(users.updatePrivacy).not.toHaveBeenCalled();
    });

    it('does not show a new health notice while the accepted version is still current', async () => {
        users.getPrivacy.mockReturnValueOnce(of({ ...emptyState, healthAcceptedAtUtc: '2026-11-02T10:00:00Z' }));

        await expect(service.ensureHealthConsentAsync()).resolves.toBe(true);

        expect(dialogs.open).not.toHaveBeenCalled();
        expect(users.updatePrivacy).not.toHaveBeenCalled();
    });

    it('records a refusal without resuming the requested health action', async () => {
        dialogs.open.mockReturnValueOnce({ afterClosed: (): Observable<boolean> => of(false) });

        await expect(service.ensureHealthConsentAsync()).resolves.toBe(false);

        expect(users.updatePrivacy).toHaveBeenCalledExactlyOnceWith(PrivacyConsentPurpose.HealthTracking, false, PRIVACY_NOTICE_VERSION);
    });

    it('keeps failed privacy loads recoverable on the next health action', async () => {
        users.getPrivacy.mockReturnValueOnce(throwError(() => new Error('Privacy settings unavailable')));

        await expect(service.ensureHealthConsentAsync()).resolves.toBe(false);
        expect(service.failed()).toBe(true);
        expect(dialogs.open).not.toHaveBeenCalled();

        await expect(service.ensureHealthConsentAsync()).resolves.toBe(true);
        expect(service.failed()).toBe(false);
        expect(dialogs.open).toHaveBeenCalledOnce();
    });
});

describe('privacy state confirmation', () => {
    it('does not apply or display a stale notice after logout during the privacy read', async () => {
        const result = new Subject<UserPrivacyState>();
        users.getPrivacy.mockReturnValueOnce(result);
        const pending = service.ensureHealthConsentAsync();

        sessions.notifySessionEnded();
        result.next(emptyState);
        result.complete();

        await expect(pending).resolves.toBe(false);
        expect(service.state()).toBeNull();
        expect(dialogs.open).not.toHaveBeenCalled();
    });

    it.each([false, true])('requires the settings read to confirm AI consent, confirmation=%s', async confirmed => {
        const aiAcceptedAtUtc = confirmed ? '2026-11-02T10:00:00Z' : null;
        users.getPrivacy.mockReturnValueOnce(of(emptyState)).mockReturnValueOnce(of({ ...emptyState, aiAcceptedAtUtc }));
        users.updatePrivacy.mockReturnValueOnce(of(undefined));

        await expect(service.chooseAsync(PrivacyConsentPurpose.AiProcessing, true)).resolves.toBe(confirmed);

        expect(users.updatePrivacy).toHaveBeenCalledExactlyOnceWith(PrivacyConsentPurpose.AiProcessing, true, PRIVACY_NOTICE_VERSION);
        expect(service.state()?.aiAcceptedAtUtc).toBe(aiAcceptedAtUtc);
    });

    it('does not save a previously granted choice if the settings read belongs to an ended session', async () => {
        const response = new Subject<UserPrivacyState>();
        users.getPrivacy.mockReturnValueOnce(response);
        const chosen = service.chooseAsync(PrivacyConsentPurpose.AiProcessing, true);

        sessions.notifySessionEnded();
        response.next(emptyState);

        await expect(chosen).resolves.toBe(false);
        expect(service.state()).toBeNull();
        expect(users.updatePrivacy).not.toHaveBeenCalled();
    });

    it('does not show the former health notice if the account changes during its lazy load', async () => {
        const response = new Subject<UserPrivacyState>();
        users.getPrivacy.mockReturnValueOnce(response);
        const review = service.ensureHealthConsentAsync();

        response.next(emptyState);
        await waitForAsyncTasksAsync();
        expect(service.state()).toEqual(emptyState);
        sessions.notifyAuthenticated();

        await expect(review).resolves.toBe(false);
        expect(dialogs.open).not.toHaveBeenCalled();
    });

    it('does not apply an old save failure to the replacement account', async () => {
        const response = new Subject<void>();
        users.updatePrivacy.mockReturnValueOnce(response);
        const chosen = service.chooseAsync(PrivacyConsentPurpose.AiProcessing, true);
        await vi.waitFor(() => {
            expect(users.updatePrivacy).toHaveBeenCalledOnce();
        });

        sessions.notifyAuthenticated();
        response.error(new Error('Previous account request failed'));

        await expect(chosen).resolves.toBe(false);
        expect(service.failed()).toBe(false);
        expect(service.state()).toBeNull();
    });
});

describe('health consent transport errors', () => {
    it.each([
        [new HttpErrorResponse({ status: HttpStatusCode.Forbidden, error: { error: 'Privacy.HealthConsentRequired' } }), true],
        [new HttpErrorResponse({ status: HttpStatusCode.Forbidden, error: { error: 'Authentication.Forbidden' } }), false],
        [new HttpErrorResponse({ status: HttpStatusCode.BadRequest, error: { error: 'Privacy.HealthConsentRequired' } }), false],
        [new Error('Offline'), false],
    ])('recognizes only the specific forbidden consent response', (error, requiresConsent) => {
        expect(isHealthConsentRequiredError(error)).toBe(requiresConsent);
    });
});

describe('privacy reconciliation without current results', () => {
    it('does not retry consent when the current account has no recorded withdrawal', async () => {
        users.user.mockReturnValue({ id: 'current-account' });
        const read = vi.spyOn(TestBed.inject(BrowserStorageService), 'getItem');

        await service.flushPendingWithdrawalAsync();

        expect(read).toHaveBeenCalledExactlyOnceWith('local', 'fd_marketing_withdrawal:current-account');
        expect(users.getPrivacy).not.toHaveBeenCalled();
        expect(users.updatePrivacy).not.toHaveBeenCalled();
    });

    it('does not display a privacy load error after the requesting account has left', async () => {
        const response = new Subject<UserPrivacyState>();
        users.getPrivacy.mockReturnValueOnce(response);
        const review = service.ensureHealthConsentAsync();

        sessions.notifySessionEnded();
        response.error(new Error('Previous account read failed'));

        await expect(review).resolves.toBe(false);
        expect(service.failed()).toBe(false);
        expect(dialogs.open).not.toHaveBeenCalled();
    });
});
