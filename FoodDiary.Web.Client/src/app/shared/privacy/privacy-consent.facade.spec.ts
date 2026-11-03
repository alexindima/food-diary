import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { FdUiDialogService } from 'fd-ui-kit';
import { type Observable, of, Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from '../../services/auth.service';
import { ExportService } from '../api/export.service';
import { type UserPrivacyState, UserService } from '../api/user.service';
import { SessionEventsService } from '../auth/session-events.service';
import { MarketingConsentService } from '../marketing/marketing-consent.service';
import { BrowserStorageService } from '../platform/browser-storage.service';
import { PrivacyConsentFacade } from './privacy-consent.facade';
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
