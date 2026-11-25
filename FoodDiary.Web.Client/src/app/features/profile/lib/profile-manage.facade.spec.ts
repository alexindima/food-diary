import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { createEnvironmentInjector, DestroyRef, EnvironmentInjector } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { of, Subject, throwError } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, onTestFinished, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../../testing/async-testing';
import { AuthService } from '../../../services/auth.service';
import { type UserProfileOverview, UserService } from '../../../shared/api/user.service';
import { SessionEventsService } from '../../../shared/auth/session-events.service';
import { TelegramBackupEmailFlowService } from '../../../shared/auth/telegram-backup-email-flow.service';
import { TelegramWebAppService } from '../../../shared/auth/telegram-web-app.service';
import { LocalizationService } from '../../../shared/i18n/localization.service';
import { entityId } from '../../../shared/models/semantics/entity-id';
import { UpdateUserDto, type User } from '../../../shared/models/user.data';
import { NotificationService } from '../../../shared/notifications/notification.service';
import { BrowserStorageService } from '../../../shared/platform/browser-storage.service';
import { BrowserWindowService } from '../../../shared/platform/browser-window.service';
import { ThemeService } from '../../../shared/theme/theme.service';
import { ProfileMeasurementsService } from '../api/profile-measurements.service';
import { TelegramDisconnectDialogComponent } from '../dialogs/telegram-disconnect-dialog/telegram-disconnect-dialog';
import { ProfileManageFacade } from './profile-manage.facade';

const user: User = {
    id: entityId<'user'>('u1'),
    email: 'test@example.com',
    hasPassword: false,
    language: 'ru',
    isActive: true,
    isEmailConfirmed: true,
    pushNotificationsEnabled: true,
    fastingPushNotificationsEnabled: false,
    socialPushNotificationsEnabled: true,
    fastingCheckInReminderHours: 12,
    fastingCheckInFollowUpReminderHours: 20,
};

const overview: UserProfileOverview = {
    user,
    notificationPreferences: {
        pushNotificationsEnabled: true,
        fastingPushNotificationsEnabled: false,
        socialPushNotificationsEnabled: true,
        fastingCheckInReminderHours: 12,
        fastingCheckInFollowUpReminderHours: 20,
    },
    webPushSubscriptions: [
        {
            endpoint: 'https://push.example.com/subscriptions/current',
            endpointHost: 'push.example.com',
            expirationTimeUtc: null,
            locale: 'en',
            userAgent: 'Chrome',
            createdAtUtc: '2026-04-10T10:00:00Z',
            updatedAtUtc: null,
        },
    ],
    dietologistRelationship: null,
};

let facade: ProfileManageFacade;
let userService: {
    getOverview: ReturnType<typeof vi.fn>;
    update: ReturnType<typeof vi.fn>;
};
let notificationService: {
    updateNotificationPreferences: ReturnType<typeof vi.fn>;
    getWebPushSubscriptions: ReturnType<typeof vi.fn>;
    removeWebPushSubscription: ReturnType<typeof vi.fn>;
};
let dialogService: { open: ReturnType<typeof vi.fn> };
let authService: {
    getUserId: ReturnType<typeof vi.fn>;
    requestTelegramBackupEmail: ReturnType<typeof vi.fn>;
    onLogoutAsync: ReturnType<typeof vi.fn>;
    startAdminSso: ReturnType<typeof vi.fn>;
    linkGoogle: ReturnType<typeof vi.fn>;
};
let toastService: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn> };
let localizationService: { applyLanguagePreferenceAsync: ReturnType<typeof vi.fn> };
const telegram = { initializeAsync: vi.fn(), getInitData: vi.fn() };
const backupEmailFlow = { startAsync: vi.fn(), markSent: vi.fn(), read: vi.fn().mockReturnValue(null), clear: vi.fn() };
const browser = { open: vi.fn() };
const syncProfileTheme = vi.fn<ThemeService['syncWithUserPreferences']>();
const BACKUP_EMAIL_RECORD_TTL_MS = 900_000;

function createStorageMock(session: Map<string, unknown>): Pick<BrowserStorageService, 'getJson' | 'setJson' | 'removeItem'> {
    return {
        getJson: (_scope, key): unknown => session.get(key) ?? null,
        setJson: (_scope, key, value): void => {
            session.set(key, value);
        },
        removeItem: (_scope, key): void => {
            session.delete(key);
        },
    };
}

beforeEach(() => {
    vi.useFakeTimers();
    syncProfileTheme.mockClear();
    const session = new Map<string, unknown>();
    userService = {
        getOverview: vi.fn().mockReturnValue(of(overview)),
        update: vi.fn().mockReturnValue(of(user)),
    };
    notificationService = {
        updateNotificationPreferences: vi.fn().mockReturnValue(
            of({
                pushNotificationsEnabled: false,
                fastingPushNotificationsEnabled: true,
                socialPushNotificationsEnabled: true,
                fastingCheckInReminderHours: 12,
                fastingCheckInFollowUpReminderHours: 20,
            }),
        ),
        getWebPushSubscriptions: vi.fn().mockReturnValue(of(overview.webPushSubscriptions)),
        removeWebPushSubscription: vi.fn().mockReturnValue(of(void 0)),
    };
    dialogService = { open: vi.fn() };
    authService = {
        getUserId: vi.fn().mockReturnValue(user.id),
        requestTelegramBackupEmail: vi.fn().mockReturnValue(of(void 0)),
        onLogoutAsync: vi.fn().mockResolvedValue(void 0),
        startAdminSso: vi.fn().mockReturnValue(of({ code: 'abc123', expiresAtUtc: '2026-04-02T00:00:00Z' })),
        linkGoogle: vi.fn().mockReturnValue(of({ ...user, hasGoogleIdentity: true })),
    };
    toastService = {
        success: vi.fn(),
        error: vi.fn(),
    };
    localizationService = {
        applyLanguagePreferenceAsync: vi.fn().mockResolvedValue(void 0),
    };
    dialogService.open.mockReturnValue({ afterClosed: () => of(false) });
    telegram.initializeAsync.mockResolvedValue(void 0);
    telegram.getInitData.mockReturnValue('signed-proof');
    backupEmailFlow.startAsync.mockResolvedValue(void 0);

    TestBed.configureTestingModule({
        providers: [
            { provide: BrowserWindowService, useValue: browser },
            {
                provide: BrowserStorageService,
                useValue: createStorageMock(session),
            },
            ProfileManageFacade,
            { provide: TelegramBackupEmailFlowService, useValue: backupEmailFlow },
            { provide: ThemeService, useValue: { syncWithUserPreferences: syncProfileTheme } },
            {
                provide: ProfileMeasurementsService,
                useValue: { getLatest: vi.fn().mockReturnValue(of({ weightKg: null, waistCm: null })) },
            },
            { provide: TelegramWebAppService, useValue: telegram },
            { provide: UserService, useValue: userService },
            { provide: NotificationService, useValue: notificationService },
            { provide: FdUiDialogService, useValue: dialogService },
            { provide: FdUiToastService, useValue: toastService },
            { provide: AuthService, useValue: authService },
            { provide: LocalizationService, useValue: localizationService },
            {
                provide: TranslateService,
                useValue: {
                    instant: vi.fn((key: string) => key),
                },
            },
        ],
    });

    facade = TestBed.inject(ProfileManageFacade);
});

describe('ProfileManageFacade Google linking', () => {
    it('updates the current user and confirms successful linking', () => {
        initializeFacade();

        facade.linkGoogle('google-credential');

        expect(authService.linkGoogle).toHaveBeenCalledWith('google-credential');
        expect(facade.user()?.hasGoogleIdentity).toBe(true);
        expect(facade.isLinkingGoogle()).toBe(false);
        expect(toastService.success).toHaveBeenCalledWith('USER_MANAGE.GOOGLE_LINK_SUCCESS');
    });

    it('keeps the account unlinked and reports a failed linking attempt', () => {
        initializeFacade();
        authService.linkGoogle.mockReturnValue(throwError(() => new Error('failed')));

        facade.linkGoogle('google-credential');

        expect(facade.user()?.hasGoogleIdentity).not.toBe(true);
        expect(facade.globalError()).toBe('USER_MANAGE.GOOGLE_LINK_ERROR');
        expect(toastService.error).toHaveBeenCalledWith('USER_MANAGE.GOOGLE_LINK_ERROR');
    });
});

afterEach(() => {
    vi.useRealTimers();
});

describe('ProfileManageFacade overview loading', () => {
    it('keeps a saved profile when an older overview completes afterward', async () => {
        initializeFacade();
        const pending = new Subject<UserProfileOverview>();
        userService.getOverview.mockReturnValueOnce(pending);
        initializeFacade();
        expect(pending.observed).toBe(true);
        userService.update.mockReturnValueOnce(of({ ...user, username: 'saved' }));

        expect(await facade.saveProfileAsync(new UpdateUserDto({ username: 'saved' }))).toBe(true);
        pending.next({ ...overview, user: { ...user, username: 'stale' } });

        expect(facade.user()?.username).toBe('saved');
        expect(pending.observed).toBe(false);
    });

    it('cancels an overview read when the profile owner is destroyed', () => {
        const pending = new Subject<UserProfileOverview>();
        userService.getOverview.mockReturnValueOnce(pending);
        initializeFacade();
        expect(pending.observed).toBe(true);

        TestBed.resetTestingModule();
        pending.next(overview);

        expect(pending.observed).toBe(false);
        expect(facade.user()).toBeNull();
    });

    it('loads user and applies language on initialize', () => {
        initializeFacade();

        expect(userService.getOverview).toHaveBeenCalledTimes(1);
        expect(facade.user()).toEqual(expect.objectContaining(user));
        expect(localizationService.applyLanguagePreferenceAsync).toHaveBeenCalledWith('ru');
        expect(facade.user()?.pushNotificationsEnabled).toBe(true);
        expect(facade.webPushSubscriptions()).toHaveLength(1);
        expect(facade.globalError()).toBeNull();
    });
});

describe('ProfileManageFacade backup email', () => {
    it('requests backup email without changing the current account address', async () => {
        facade.user.set({ ...user, email: null, hasTelegramIdentity: true });
        await facade.requestBackupEmailAsync(' backup@example.com ');
        expect(authService.requestTelegramBackupEmail).toHaveBeenCalledWith('backup@example.com', 'signed-proof');
        expect(facade.backupEmailSentTo()).toBe('backup@example.com');
        expect(facade.user()?.email).toBeNull();
        expect(facade.isRequestingBackupEmail()).toBe(false);
    });

    it('starts browser reauthentication when Mini App proof is unavailable', async () => {
        facade.user.set({ ...user, email: null, hasTelegramIdentity: true });
        telegram.getInitData.mockReturnValue(null);
        await facade.requestBackupEmailAsync('backup@example.com');
        expect(authService.requestTelegramBackupEmail).not.toHaveBeenCalled();
        expect(facade.backupEmailSentTo()).toBeNull();
        expect(backupEmailFlow.startAsync).toHaveBeenCalledWith('backup@example.com');
        expect(facade.globalError()).toBeNull();
    });

    it('reports a failed backup email request without claiming delivery', async () => {
        facade.user.set({ ...user, email: null, hasTelegramIdentity: true });
        authService.requestTelegramBackupEmail.mockReturnValue(throwError(() => new Error('expired proof')));
        await facade.requestBackupEmailAsync('backup@example.com');
        expect(facade.backupEmailSentTo()).toBeNull();
        expect(facade.user()?.email).toBeNull();
        expect(facade.globalError()).toBe('USER_MANAGE.BACKUP_EMAIL_ERROR');
    });

    it('requires confirmed email before showing password setup', () => {
        facade.user.set({ ...user, email: null, hasTelegramIdentity: true });
        facade.openChangePasswordDialog();
        expect(dialogService.open).not.toHaveBeenCalled();
        expect(facade.globalError()).toBe('USER_MANAGE.BACKUP_EMAIL_PASSWORD_REQUIRED');
    });
});

describe('Profile bootstrap after a backup-email return', () => {
    it('restores the previously sent address and its resend window without replacing the account email', () => {
        userService.getOverview.mockReturnValueOnce(of({ ...overview, user: { ...user, email: null, hasTelegramIdentity: true } }));
        const sentAt = Date.now();
        const address = 'pending@example.test';
        const pending: NonNullable<ReturnType<TelegramBackupEmailFlowService['read']>> = {
            userId: user.id,
            email: address,
            state: null,
            expiresAt: sentAt + BACKUP_EMAIL_RECORD_TTL_MS,
            sentAt,
            failed: false,
        };
        backupEmailFlow.read.mockReturnValueOnce(pending);

        initializeFacade();

        expect(facade.backupEmailSentTo()).toBe(address);
        expect(facade.backupEmailResendAt()).toBeGreaterThan(sentAt);
        expect(facade.user()?.email).toBeNull();
        expect(authService.requestTelegramBackupEmail).not.toHaveBeenCalled();
    });

    it('shows a failed verification return once and allows reopening with the account intact', () => {
        const failed: NonNullable<ReturnType<TelegramBackupEmailFlowService['read']>> = {
            userId: user.id,
            email: 'pending@example.test',
            state: null,
            expiresAt: Date.now() + BACKUP_EMAIL_RECORD_TTL_MS,
            sentAt: null,
            failed: true,
        };
        backupEmailFlow.clear.mockClear();
        backupEmailFlow.read.mockReturnValueOnce(failed).mockReturnValueOnce(failed);

        initializeFacade();
        expect(facade.globalError()).toBe('USER_MANAGE.BACKUP_EMAIL_ERROR');
        expect(facade.backupEmailSentTo()).toBeNull();
        expect(facade.user()).toEqual(user);
        expect(backupEmailFlow.clear).toHaveBeenCalledOnce();
        initializeFacade();
        expect(facade.globalError()).toBeNull();
        expect(facade.user()).toEqual(user);
    });
});

describe('Profile overview read recovery', () => {
    it.each(['empty', 'unavailable'] as const)('reports an %s overview and clears it after the account loads', failure => {
        userService.getOverview.mockReturnValueOnce(
            failure === 'empty' ? of(null) : throwError(() => new Error('Profile temporarily unavailable')),
        );

        initializeFacade();
        expect(facade.user()).toBeNull();
        expect(facade.globalError()).toBe('USER_MANAGE.LOAD_ERROR');
        expect(localizationService.applyLanguagePreferenceAsync).not.toHaveBeenCalled();
        initializeFacade();
        expect(facade.user()).toEqual(user);
        expect(facade.globalError()).toBeNull();
        expect(localizationService.applyLanguagePreferenceAsync).toHaveBeenCalledOnce();
    });
});

describe('ProfileManageFacade account actions', () => {
    it('offers the configured confirmations and keeps the user in their profile', async () => {
        const linked = { ...user, hasTelegramIdentity: true, hasGoogleIdentity: true, hasPassword: true };
        const updated = { ...linked, hasTelegramIdentity: false };
        facade.user.set(linked);
        dialogService.open.mockReturnValue({ afterClosed: () => of(updated) });

        await facade.unlinkTelegramAsync();

        expect(dialogService.open).toHaveBeenCalledWith(TelegramDisconnectDialogComponent, {
            data: { owner: user.id, canUsePassword: true, canUseGoogle: true },
        });
        expect(facade.user()).toEqual(updated);
        expect(authService.onLogoutAsync).not.toHaveBeenCalled();
        expect(toastService.success).toHaveBeenCalledWith('USER_MANAGE.TELEGRAM_DISCONNECT_SUCCESS');
        expect(browser.open).not.toHaveBeenCalled();
    });

    it('does not offer to unlink the last sign-in method', async () => {
        facade.user.set({ ...user, email: null, hasTelegramIdentity: true, hasGoogleIdentity: false });
        await facade.unlinkTelegramAsync();
        expect(facade.telegramUnlinkError()).toBe('USER_MANAGE.TELEGRAM_BACKUP_REQUIRED');
        expect(dialogService.open).not.toHaveBeenCalled();
    });

    it('keeps Telegram linked when confirmation is cancelled', async () => {
        facade.user.set({ ...user, hasTelegramIdentity: true, hasGoogleIdentity: true });
        dialogService.open.mockReturnValue({ afterClosed: () => of(undefined) });

        await facade.unlinkTelegramAsync();

        expect(facade.user()?.hasTelegramIdentity).toBe(true);
        expect(facade.isUnlinkingTelegram()).toBe(false);
        expect(toastService.success).not.toHaveBeenCalled();
    });

    it('keeps the sign-in method and allows retry when the confirmation dialog cannot open', async () => {
        const linked = { ...user, hasTelegramIdentity: true, hasGoogleIdentity: true };
        userService.getOverview.mockReturnValueOnce(of({ ...overview, user: linked }));
        dialogService.open
            .mockImplementationOnce(() => {
                throw new Error('Unable to create the dialog overlay');
            })
            .mockReturnValueOnce({ afterClosed: () => of(undefined) });
        initializeFacade();

        await facade.unlinkTelegramAsync();

        expect(facade.user()).toEqual(linked);
        expect(facade.telegramUnlinkError()).toBe('USER_MANAGE.TELEGRAM_UNLINK_ERROR');
        expect(facade.isUnlinkingTelegram()).toBe(false);
        expect(authService.onLogoutAsync).not.toHaveBeenCalled();

        await facade.unlinkTelegramAsync();
        expect(dialogService.open).toHaveBeenCalledTimes(2);
        expect(facade.telegramUnlinkError()).toBeNull();
        expect(facade.user()).toEqual(linked);
    });
});

describe('ProfileManageFacade password and deletion actions', () => {
    it('opens password success dialog after successful password dialog close', () => {
        dialogService.open.mockReturnValueOnce({ afterClosed: () => of(true) }).mockReturnValueOnce({ afterClosed: () => of(void 0) });
        facade.user.set(user);

        facade.openChangePasswordDialog();

        expect(dialogService.open).toHaveBeenCalledTimes(2);
        expect(dialogService.open.mock.calls[0][1]).toEqual(
            expect.objectContaining({
                data: { hasPassword: false, ownerDestroyRef: TestBed.inject(DestroyRef) },
            }),
        );
        expect(facade.user()?.hasPassword).toBe(true);
        expect(authService.onLogoutAsync).toHaveBeenCalledWith(true);
    });
});

describe('ProfileManageFacade existing password lifecycle', () => {
    it.each([
        { action: 'cancelling', changed: false, dialogCount: 1, logoutCount: 0 },
        { action: 'changing', changed: true, dialogCount: 2, logoutCount: 1 },
    ])('keeps the existing account state when $action its password', ({ changed, dialogCount, logoutCount }) => {
        const existing = { ...user, hasPassword: true };
        userService.getOverview.mockReturnValueOnce(of({ ...overview, user: existing }));
        initializeFacade();
        dialogService.open.mockReturnValueOnce({ afterClosed: () => of(changed) });

        facade.openChangePasswordDialog();

        expect(dialogService.open).toHaveBeenCalledTimes(dialogCount);
        expect(dialogService.open.mock.calls[0][1]).toEqual(
            expect.objectContaining({ data: { hasPassword: true, ownerDestroyRef: TestBed.inject(DestroyRef) } }),
        );
        expect(authService.onLogoutAsync).toHaveBeenCalledTimes(logoutCount);
        expect(facade.user()).toEqual(existing);
        expect(facade.globalError()).toBeNull();
    });
});

describe('ProfileManageFacade notification preferences', () => {
    it('updates notification preferences through notification endpoint', async () => {
        initializeFacade();

        const updatedUser = await facade.updateNotificationPreferencesAsync({ pushNotificationsEnabled: false });

        expect(notificationService.updateNotificationPreferences).toHaveBeenCalledWith({ pushNotificationsEnabled: false });
        expect(updatedUser?.pushNotificationsEnabled).toBe(false);
        expect(updatedUser?.fastingPushNotificationsEnabled).toBe(true);
        expect(facade.globalError()).toBeNull();
    });

    it('removes web push subscription and updates local device list', async () => {
        initializeFacade();

        const removed = await facade.removeWebPushSubscriptionAsync('https://push.example.com/subscriptions/current');

        expect(removed).toBe(true);
        expect(notificationService.removeWebPushSubscription).toHaveBeenCalledWith('https://push.example.com/subscriptions/current');
        expect(facade.webPushSubscriptions()).toHaveLength(0);
    });

    it('cancels an older web push refresh and keeps the latest device list', async () => {
        initializeFacade();
        const firstRefresh = new Subject<typeof overview.webPushSubscriptions>();
        const secondRefresh = new Subject<typeof overview.webPushSubscriptions>();
        notificationService.getWebPushSubscriptions
            .mockReturnValueOnce(firstRefresh.asObservable())
            .mockReturnValueOnce(secondRefresh.asObservable());

        facade.refreshWebPushSubscriptions();
        TestBed.tick();
        expect(firstRefresh.observed).toBe(true);
        facade.refreshWebPushSubscriptions();
        TestBed.tick();
        expect(firstRefresh.observed).toBe(false);

        firstRefresh.next([
            {
                ...overview.webPushSubscriptions[0],
                endpoint: 'https://push.example.com/subscriptions/stale',
            },
        ]);
        firstRefresh.complete();
        expect(facade.webPushSubscriptions()[0].endpoint).toBe('https://push.example.com/subscriptions/current');

        secondRefresh.next([
            {
                ...overview.webPushSubscriptions[0],
                endpoint: 'https://push.example.com/subscriptions/latest',
            },
        ]);
        secondRefresh.complete();
        await waitForAsyncTasksAsync();

        expect(facade.webPushSubscriptions()[0].endpoint).toBe('https://push.example.com/subscriptions/latest');
        expect(facade.isLoadingWebPushSubscriptions()).toBe(false);
    });

    it('sets update error when notification preferences request fails', async () => {
        initializeFacade();
        notificationService.updateNotificationPreferences.mockReturnValueOnce(throwError(() => new Error('preferences failed')));

        const updatedUser = await facade.updateNotificationPreferencesAsync({ socialPushNotificationsEnabled: false });

        expect(updatedUser).toBeNull();
        expect(facade.globalError()).toBe('USER_MANAGE.UPDATE_ERROR');
    });
});

describe('ProfileManageFacade saved language defaults', () => {
    it('applies the default language preference when the saved profile omits a language', async () => {
        const savedProfile: User = { ...user, firstName: 'Mira', language: undefined };
        initializeFacade();
        userService.update.mockReturnValueOnce(of(savedProfile));

        const saved = await facade.saveProfileAsync(new UpdateUserDto({ firstName: 'Mira' }));

        expect(saved).toBe(true);
        expect(facade.user()).toEqual(savedProfile);
        expect(localizationService.applyLanguagePreferenceAsync.mock.calls).toEqual([['ru'], [null]]);
        expect(facade.profileSavedVersion()).toBe(1);
    });
});

describe('ProfileManageFacade save session ownership', () => {
    it.each(['accepted', 'rejected', 'unavailable'] as const)(
        'keeps the new account state when the previous account save is %s late',
        async result => {
            const app = TestBed.inject(EnvironmentInjector);
            const firstPage = createEnvironmentInjector([ProfileManageFacade], app);
            const nextPage = createEnvironmentInjector([ProfileManageFacade], app);
            onTestFinished(() => {
                if (!firstPage.destroyed) {
                    firstPage.destroy();
                }
                nextPage.destroy();
            });
            const sessions = TestBed.inject(SessionEventsService);
            const firstProfile = firstPage.get(ProfileManageFacade);
            firstProfile.initialize();
            TestBed.tick();

            const pending = new Subject<User | null>();
            userService.update.mockReturnValueOnce(pending);
            const saving = firstProfile.saveProfileAsync(new UpdateUserDto({ theme: 'dark' }));
            expect(pending.observed).toBe(true);

            sessions.notifySessionEnded();
            firstPage.destroy();
            const newUser = { ...user, id: entityId<'user'>('new-account'), language: 'en', theme: 'light' };
            authService.getUserId.mockReturnValue(newUser.id);
            sessions.notifyAuthenticated();
            userService.getOverview.mockReturnValueOnce(of({ ...overview, user: newUser }));
            const nextProfile = nextPage.get(ProfileManageFacade);
            nextProfile.initialize();
            TestBed.tick();
            expect(localizationService.applyLanguagePreferenceAsync).toHaveBeenLastCalledWith('en');
            expect(syncProfileTheme).toHaveBeenLastCalledWith('light', undefined, undefined);

            expect(pending.observed).toBe(true);
            if (result === 'unavailable') {
                pending.error(new HttpErrorResponse({ status: HttpStatusCode.ServiceUnavailable, statusText: 'Service Unavailable' }));
            } else {
                pending.next(result === 'accepted' ? { ...user, theme: 'dark' } : null);
                pending.complete();
            }
            const saved = await saving;

            expect(localizationService.applyLanguagePreferenceAsync).toHaveBeenLastCalledWith('en');
            expect(syncProfileTheme).toHaveBeenLastCalledWith('light', undefined, undefined);
            expect(nextProfile.user()?.id).toBe(newUser.id);
            expect(nextProfile.globalError()).toBeNull();
            expect(firstProfile.globalError()).toBeNull();
            expect(firstProfile.profileSavedVersion()).toBe(0);
            expect(firstProfile.isSavingProfile()).toBe(false);
            expect(saved).toBe(false);
        },
    );

    it('applies an accepted save after ordinary navigation within the same session', async () => {
        const page = createEnvironmentInjector([ProfileManageFacade], TestBed.inject(EnvironmentInjector));
        onTestFinished(() => {
            if (!page.destroyed) {
                page.destroy();
            }
        });
        const profile = page.get(ProfileManageFacade);
        profile.initialize();
        TestBed.tick();
        const pending = new Subject<User | null>();
        userService.update.mockReturnValueOnce(pending);
        const saving = profile.saveProfileAsync(new UpdateUserDto({ language: 'en', theme: 'dark' }));

        page.destroy();
        expect(pending.observed).toBe(true);
        pending.next({ ...user, language: 'en', theme: 'dark' });
        pending.complete();

        expect(await saving).toBe(true);
        expect(localizationService.applyLanguagePreferenceAsync).toHaveBeenLastCalledWith('en');
        expect(syncProfileTheme).toHaveBeenLastCalledWith('dark', undefined, undefined);
    });
});

describe('ProfileManageFacade explicit profile save', () => {
    it('reports a successful save after updating the user without a success dialog', async () => {
        initializeFacade();
        userService.update.mockReturnValueOnce(of({ ...user, firstName: 'Alexa' }));

        const saved = await facade.saveProfileAsync(new UpdateUserDto({ firstName: 'Alexa' }));

        expect(saved).toBe(true);
        expect(userService.update).toHaveBeenCalledTimes(1);
        expect(userService.update.mock.calls[0][0]).toEqual(expect.objectContaining({ firstName: 'Alexa' }));
        expect(dialogService.open).not.toHaveBeenCalled();
        expect(facade.user()?.firstName).toBe('Alexa');
        expect(facade.profileSavedVersion()).toBe(1);
        expect(facade.isSavingProfile()).toBe(false);
    });

    it('waits for the HTTP response before reporting a successful save', async () => {
        initializeFacade();
        const response = new Subject<User | null>();
        userService.update.mockReturnValueOnce(response);
        let settled = false;

        const saving = facade.saveProfileAsync(new UpdateUserDto({ firstName: 'Alexa' }));
        void saving.then(() => {
            settled = true;
        });
        await vi.waitFor(() => {
            expect(facade.isSavingProfile()).toBe(true);
        });

        expect(settled).toBe(false);
        expect(facade.isSavingProfile()).toBe(true);
        expect(facade.profileSavedVersion()).toBe(0);
        expect(facade.user()).toEqual(user);

        response.next({ ...user, firstName: 'Alexa' });
        expect(await saving).toBe(true);
        expect(facade.user()?.firstName).toBe('Alexa');
        expect(facade.profileSavedVersion()).toBe(1);
        expect(facade.isSavingProfile()).toBe(false);
    });

    it('rejects another save while a profile save is in flight', async () => {
        initializeFacade();

        const inFlightUpdate = new Subject<User | null>();
        userService.update.mockReturnValueOnce(inFlightUpdate.asObservable());

        const saving = facade.saveProfileAsync(new UpdateUserDto({ firstName: 'Alex' }));
        expect(userService.update).toHaveBeenCalledTimes(1);

        expect(await facade.saveProfileAsync(new UpdateUserDto({ firstName: 'Alexa' }))).toBe(false);
        expect(userService.update).toHaveBeenCalledTimes(1);
        expect(facade.isSavingProfile()).toBe(true);

        inFlightUpdate.next({ ...user, firstName: 'Alex' });
        expect(await saving).toBe(true);
    });

    it('preserves the saved profile on request failure and allows a retry', async () => {
        initializeFacade();
        const response = new Subject<User | null>();
        userService.update.mockReturnValueOnce(response);

        const saving = facade.saveProfileAsync(new UpdateUserDto({ firstName: 'Alexa' }));
        response.error(new Error('save unavailable'));

        expect(await saving).toBe(false);
        expect(facade.user()).toEqual(user);
        expect(facade.profileSavedVersion()).toBe(0);
        expect(facade.isSavingProfile()).toBe(false);
        expect(facade.globalError()).toBe('USER_MANAGE.UPDATE_ERROR');

        expect(await facade.saveProfileAsync(new UpdateUserDto({ firstName: 'Alexa' }))).toBe(true);
        expect(facade.profileSavedVersion()).toBe(1);
        expect(facade.globalError()).toBeNull();
    });

    it('does not report success or discard the saved profile when the response is null', async () => {
        initializeFacade();
        userService.update.mockReturnValueOnce(of(null));

        expect(await facade.saveProfileAsync(new UpdateUserDto({ firstName: 'Alexa' }))).toBe(false);
        expect(facade.user()).toEqual(user);
        expect(facade.profileSavedVersion()).toBe(0);
        expect(facade.isSavingProfile()).toBe(false);
        expect(facade.globalError()).toBe('USER_MANAGE.UPDATE_ERROR');
    });
});

describe('ProfileManageFacade pending overview', () => {
    it('leaves the initial read running when asked to save without an account baseline', async () => {
        const result = new Subject<UserProfileOverview | null>();
        userService.getOverview.mockReturnValueOnce(result);
        initializeFacade();

        expect(await facade.saveProfileAsync(new UpdateUserDto({ firstName: 'Incomplete profile' }))).toBe(false);
        expect(userService.update).not.toHaveBeenCalled();
        expect(result.observed).toBe(true);
        result.next(overview);
        result.complete();
        expect(facade.user()).toEqual(user);
    });
});

describe('ProfileManageFacade pending account actions', () => {
    it('keeps the first backup email request when another address is submitted while it is pending', async () => {
        const linked = { ...user, email: null, hasTelegramIdentity: true };
        userService.getOverview.mockReturnValueOnce(of({ ...overview, user: linked }));
        const delivery = new Subject<void>();
        authService.requestTelegramBackupEmail.mockReturnValueOnce(delivery);
        initializeFacade();

        const firstRequest = facade.requestBackupEmailAsync('first@example.test');
        await facade.requestBackupEmailAsync('second@example.test');
        expect(authService.requestTelegramBackupEmail).toHaveBeenCalledExactlyOnceWith('first@example.test', 'signed-proof');
        expect(facade.isRequestingBackupEmail()).toBe(true);
        expect(facade.backupEmailSentTo()).toBeNull();

        delivery.next();
        delivery.complete();
        await firstRequest;
        expect(facade.isRequestingBackupEmail()).toBe(false);
        expect(facade.backupEmailSentTo()).toBe('first@example.test');
        expect(facade.user()?.email).toBeNull();
    });

    it('opens one Telegram disconnect confirmation at a time and allows another attempt after dismissal', async () => {
        const linked = { ...user, hasTelegramIdentity: true, hasGoogleIdentity: true };
        const unlinked = { ...linked, hasTelegramIdentity: false };
        userService.getOverview.mockReturnValueOnce(of({ ...overview, user: linked }));
        const confirmation = new Subject<User | undefined>();
        dialogService.open
            .mockReturnValueOnce({ afterClosed: () => confirmation })
            .mockReturnValueOnce({ afterClosed: () => of(unlinked) });
        initializeFacade();

        const firstAttempt = facade.unlinkTelegramAsync();
        await facade.unlinkTelegramAsync();
        expect(dialogService.open).toHaveBeenCalledOnce();
        expect(facade.isUnlinkingTelegram()).toBe(true);

        confirmation.next(undefined);
        confirmation.complete();
        await firstAttempt;
        expect(facade.isUnlinkingTelegram()).toBe(false);
        expect(facade.user()?.hasTelegramIdentity).toBe(true);
        expect(toastService.success).not.toHaveBeenCalled();

        await facade.unlinkTelegramAsync();
        expect(dialogService.open).toHaveBeenCalledTimes(2);
        expect(facade.user()).toEqual(unlinked);
        expect(toastService.success).toHaveBeenCalledExactlyOnceWith('USER_MANAGE.TELEGRAM_DISCONNECT_SUCCESS');
    });
});

describe('ProfileManageFacade pending notification changes', () => {
    it('saves account preferences while a separate profile read is still pending', async () => {
        const pendingOverview = new Subject<UserProfileOverview>();
        const change = { pushNotificationsEnabled: false };
        const saved = { ...overview.notificationPreferences, ...change };
        userService.getOverview.mockReturnValueOnce(pendingOverview);
        notificationService.updateNotificationPreferences.mockReturnValueOnce(of(saved));
        initializeFacade();

        await expect(facade.updateNotificationPreferencesAsync(change)).resolves.toBeNull();

        expect(notificationService.updateNotificationPreferences).toHaveBeenCalledExactlyOnceWith(change);
        expect(facade.user()).toBeNull();
        expect(facade.isUpdatingNotifications()).toBe(false);

        pendingOverview.next({ ...overview, notificationPreferences: saved });
        pendingOverview.complete();

        expect(facade.user()).toEqual({ ...user, ...saved });
        expect(facade.globalError()).toBeNull();
    });

    it('preserves the current profile while preferences save and accepts a later change', async () => {
        const saved = { ...overview.notificationPreferences, pushNotificationsEnabled: false };
        const saving = new Subject<typeof saved>();
        notificationService.updateNotificationPreferences
            .mockReturnValueOnce(saving)
            .mockReturnValueOnce(of({ ...saved, socialPushNotificationsEnabled: false }));
        initializeFacade();

        const firstUpdate = facade.updateNotificationPreferencesAsync({ pushNotificationsEnabled: false });
        expect(await facade.updateNotificationPreferencesAsync({ socialPushNotificationsEnabled: false })).toEqual(user);
        expect(notificationService.updateNotificationPreferences).toHaveBeenCalledExactlyOnceWith({ pushNotificationsEnabled: false });
        expect(facade.isUpdatingNotifications()).toBe(true);

        saving.next(saved);
        saving.complete();
        await firstUpdate;
        expect(facade.isUpdatingNotifications()).toBe(false);
        expect(facade.user()?.pushNotificationsEnabled).toBe(false);
        expect(facade.user()?.socialPushNotificationsEnabled).toBe(true);

        await facade.updateNotificationPreferencesAsync({ socialPushNotificationsEnabled: false });
        expect(notificationService.updateNotificationPreferences).toHaveBeenLastCalledWith({ socialPushNotificationsEnabled: false });
        expect(facade.user()?.socialPushNotificationsEnabled).toBe(false);
    });

    it('keeps both devices after a pending removal fails and can remove the other device afterward', async () => {
        const first = overview.webPushSubscriptions[0];
        const second = { ...first, endpoint: 'https://push.example.com/subscriptions/other' };
        userService.getOverview.mockReturnValueOnce(of({ ...overview, webPushSubscriptions: [first, second] }));
        const removal = new Subject<void>();
        notificationService.removeWebPushSubscription.mockReturnValueOnce(removal);
        initializeFacade();

        const firstRemoval = facade.removeWebPushSubscriptionAsync(first.endpoint);
        expect(await facade.removeWebPushSubscriptionAsync(second.endpoint)).toBe(false);
        expect(notificationService.removeWebPushSubscription).toHaveBeenCalledExactlyOnceWith(first.endpoint);
        expect(facade.removingWebPushSubscriptionEndpoint()).toBe(first.endpoint);

        removal.error(new Error('Device removal unavailable'));
        expect(await firstRemoval).toBe(false);
        expect(facade.removingWebPushSubscriptionEndpoint()).toBeNull();
        expect(facade.webPushSubscriptions()).toEqual([first, second]);

        expect(await facade.removeWebPushSubscriptionAsync(second.endpoint)).toBe(true);
        expect(notificationService.removeWebPushSubscription).toHaveBeenLastCalledWith(second.endpoint);
        expect(facade.webPushSubscriptions()).toEqual([first]);
    });
});

function initializeFacade(): void {
    facade.initialize();
    TestBed.tick();
}
