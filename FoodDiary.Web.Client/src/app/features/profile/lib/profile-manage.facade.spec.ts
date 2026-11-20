import { createEnvironmentInjector, EnvironmentInjector } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { of, Subject, throwError } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, onTestFinished, vi } from 'vitest';

import { AuthService } from '../../../services/auth.service';
import { NavigationService } from '../../../services/navigation.service';
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
    startTelegramUnlink: ReturnType<typeof vi.fn>;
    requestTelegramBackupEmail: ReturnType<typeof vi.fn>;
    onLogoutAsync: ReturnType<typeof vi.fn>;
    startAdminSso: ReturnType<typeof vi.fn>;
    linkGoogle: ReturnType<typeof vi.fn>;
};
let toastService: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn> };
let localizationService: { applyLanguagePreferenceAsync: ReturnType<typeof vi.fn> };
let navigationService: { navigateToHomeAsync: ReturnType<typeof vi.fn> };
const telegram = { initializeAsync: vi.fn(), getInitData: vi.fn() };
const backupEmailFlow = { startAsync: vi.fn(), markSent: vi.fn(), read: vi.fn().mockReturnValue(null), clear: vi.fn() };
const browser = { open: vi.fn() };
const syncProfileTheme = vi.fn<ThemeService['syncWithUserPreferences']>();
const STATE_LENGTH = 43;
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
        startTelegramUnlink: vi
            .fn()
            .mockReturnValue(of({ authorizationUrl: `https://oauth.telegram.org/auth?state=${'s'.repeat(STATE_LENGTH)}` })),
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
    navigationService = {
        navigateToHomeAsync: vi.fn().mockResolvedValue(void 0),
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
            { provide: NavigationService, useValue: navigationService },
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
        facade.initialize();

        facade.linkGoogle('google-credential');

        expect(authService.linkGoogle).toHaveBeenCalledWith('google-credential');
        expect(facade.user()?.hasGoogleIdentity).toBe(true);
        expect(facade.isLinkingGoogle()).toBe(false);
        expect(toastService.success).toHaveBeenCalledWith('USER_MANAGE.GOOGLE_LINK_SUCCESS');
    });

    it('keeps the account unlinked and reports a failed linking attempt', () => {
        facade.initialize();
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

describe('ProfileManageFacade loading and submit', () => {
    it('keeps a saved profile when an older overview completes afterward', () => {
        const pending = new Subject<UserProfileOverview>();
        userService.getOverview.mockReturnValueOnce(pending);
        facade.initialize();
        userService.update.mockReturnValueOnce(of({ ...user, username: 'saved' }));

        facade.submitUpdate(new UpdateUserDto({ username: 'saved' }));
        pending.next({ ...overview, user: { ...user, username: 'stale' } });

        expect(facade.user()?.username).toBe('saved');
        expect(pending.observed).toBe(false);
    });

    it('cancels an overview read when the profile owner is destroyed', () => {
        const pending = new Subject<UserProfileOverview>();
        userService.getOverview.mockReturnValueOnce(pending);
        facade.initialize();
        expect(pending.observed).toBe(true);

        TestBed.resetTestingModule();
        pending.next(overview);

        expect(pending.observed).toBe(false);
        expect(facade.user()).toBeNull();
    });

    it('loads user and applies language on initialize', () => {
        facade.initialize();

        expect(userService.getOverview).toHaveBeenCalledTimes(1);
        expect(facade.user()).toEqual(expect.objectContaining(user));
        expect(localizationService.applyLanguagePreferenceAsync).toHaveBeenCalledWith('ru');
        expect(facade.user()?.pushNotificationsEnabled).toBe(true);
        expect(facade.webPushSubscriptions()).toHaveLength(1);
        expect(facade.globalError()).toBeNull();
    });

    it('sets global error when update returns null', () => {
        userService.update.mockReturnValueOnce(of(null));

        facade.submitUpdate(new UpdateUserDto({ username: 'alex' }));

        expect(facade.globalError()).toBe('USER_MANAGE.UPDATE_ERROR');
    });

    it('shows success dialog and navigates home after successful update', () => {
        dialogService.open.mockReturnValueOnce({ afterClosed: () => of(true) });

        facade.submitUpdate(new UpdateUserDto({ username: 'alex' }));

        expect(userService.update).toHaveBeenCalledTimes(1);
        expect(dialogService.open).toHaveBeenCalled();
        expect(navigationService.navigateToHomeAsync).toHaveBeenCalled();
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

        facade.initialize();

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

        facade.initialize();
        expect(facade.globalError()).toBe('USER_MANAGE.BACKUP_EMAIL_ERROR');
        expect(facade.backupEmailSentTo()).toBeNull();
        expect(facade.user()).toEqual(user);
        expect(backupEmailFlow.clear).toHaveBeenCalledOnce();
        facade.initialize();
        expect(facade.globalError()).toBeNull();
        expect(facade.user()).toEqual(user);
    });
});

describe('Profile overview read recovery', () => {
    it.each(['empty', 'unavailable'] as const)('reports an %s overview and clears it after the account loads', failure => {
        userService.getOverview.mockReturnValueOnce(
            failure === 'empty' ? of(null) : throwError(() => new Error('Profile temporarily unavailable')),
        );

        facade.initialize();
        expect(facade.user()).toBeNull();
        expect(facade.globalError()).toBe('USER_MANAGE.LOAD_ERROR');
        expect(localizationService.applyLanguagePreferenceAsync).not.toHaveBeenCalled();
        facade.initialize();
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
});

describe('ProfileManageFacade password and deletion actions', () => {
    it('opens password success dialog after successful password dialog close', () => {
        dialogService.open.mockReturnValueOnce({ afterClosed: () => of(true) }).mockReturnValueOnce({ afterClosed: () => of(void 0) });
        facade.user.set(user);

        facade.openChangePasswordDialog();

        expect(dialogService.open).toHaveBeenCalledTimes(2);
        expect(dialogService.open.mock.calls[0][1]).toEqual(
            expect.objectContaining({
                data: { hasPassword: false },
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
        facade.initialize();
        dialogService.open.mockReturnValueOnce({ afterClosed: () => of(changed) });

        facade.openChangePasswordDialog();

        expect(dialogService.open).toHaveBeenCalledTimes(dialogCount);
        expect(dialogService.open.mock.calls[0][1]).toEqual(expect.objectContaining({ data: { hasPassword: true } }));
        expect(authService.onLogoutAsync).toHaveBeenCalledTimes(logoutCount);
        expect(facade.user()).toEqual(existing);
        expect(facade.globalError()).toBeNull();
    });
});

describe('ProfileManageFacade notification preferences', () => {
    it('updates notification preferences through notification endpoint', async () => {
        facade.initialize();

        const updatedUser = await facade.updateNotificationPreferencesAsync({ pushNotificationsEnabled: false });

        expect(notificationService.updateNotificationPreferences).toHaveBeenCalledWith({ pushNotificationsEnabled: false });
        expect(updatedUser?.pushNotificationsEnabled).toBe(false);
        expect(updatedUser?.fastingPushNotificationsEnabled).toBe(true);
        expect(facade.globalError()).toBeNull();
    });

    it('removes web push subscription and updates local device list', async () => {
        facade.initialize();

        const removed = await facade.removeWebPushSubscriptionAsync('https://push.example.com/subscriptions/current');

        expect(removed).toBe(true);
        expect(notificationService.removeWebPushSubscription).toHaveBeenCalledWith('https://push.example.com/subscriptions/current');
        expect(facade.webPushSubscriptions()).toHaveLength(0);
    });

    it('keeps latest web push device list when refresh responses complete out of order', () => {
        facade.initialize();
        const firstRefresh = new Subject<typeof overview.webPushSubscriptions>();
        const secondRefresh = new Subject<typeof overview.webPushSubscriptions>();
        notificationService.getWebPushSubscriptions
            .mockReturnValueOnce(firstRefresh.asObservable())
            .mockReturnValueOnce(secondRefresh.asObservable());

        facade.refreshWebPushSubscriptions();
        facade.refreshWebPushSubscriptions();

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

        expect(facade.webPushSubscriptions()[0].endpoint).toBe('https://push.example.com/subscriptions/latest');
        expect(facade.isLoadingWebPushSubscriptions()).toBe(false);
    });

    it('sets update error when notification preferences request fails', async () => {
        facade.initialize();
        notificationService.updateNotificationPreferences.mockReturnValueOnce(throwError(() => new Error('preferences failed')));

        const updatedUser = await facade.updateNotificationPreferencesAsync({ socialPushNotificationsEnabled: false });

        expect(updatedUser).toBeNull();
        expect(facade.globalError()).toBe('USER_MANAGE.UPDATE_ERROR');
    });
});

describe('ProfileManageFacade saved language defaults', () => {
    it('applies the default language preference when the saved profile omits a language', async () => {
        const savedProfile: User = { ...user, firstName: 'Mira', language: undefined };
        facade.initialize();
        userService.update.mockReturnValueOnce(of(savedProfile));

        const saved = await facade.saveProfileAsync(new UpdateUserDto({ firstName: 'Mira' }));

        expect(saved).toBe(true);
        expect(facade.user()).toEqual(savedProfile);
        expect(localizationService.applyLanguagePreferenceAsync.mock.calls).toEqual([['ru'], [null]]);
        expect(facade.profileSavedVersion()).toBe(1);
    });
});

describe('ProfileManageFacade save session ownership', () => {
    it('keeps the new account preferences when the previous account save is accepted late', async () => {
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
        nextPage.get(ProfileManageFacade).initialize();
        expect(localizationService.applyLanguagePreferenceAsync).toHaveBeenLastCalledWith('en');
        expect(syncProfileTheme).toHaveBeenLastCalledWith('light', undefined, undefined);

        expect(pending.observed).toBe(true);
        pending.next({ ...user, theme: 'dark' });
        pending.complete();
        const saved = await saving;

        expect(localizationService.applyLanguagePreferenceAsync).toHaveBeenLastCalledWith('en');
        expect(syncProfileTheme).toHaveBeenLastCalledWith('light', undefined, undefined);
        expect(firstProfile.profileSavedVersion()).toBe(0);
        expect(saved).toBe(false);
    });

    it('applies an accepted save after ordinary navigation within the same session', async () => {
        const page = createEnvironmentInjector([ProfileManageFacade], TestBed.inject(EnvironmentInjector));
        onTestFinished(() => {
            if (!page.destroyed) {
                page.destroy();
            }
        });
        const profile = page.get(ProfileManageFacade);
        profile.initialize();
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
        facade.initialize();
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
        facade.initialize();
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
        facade.initialize();

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
        facade.initialize();
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
        facade.initialize();
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
        facade.initialize();

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
        facade.initialize();

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
        facade.initialize();

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
    it('preserves the current profile while preferences save and accepts a later change', async () => {
        const saved = { ...overview.notificationPreferences, pushNotificationsEnabled: false };
        const saving = new Subject<typeof saved>();
        notificationService.updateNotificationPreferences
            .mockReturnValueOnce(saving)
            .mockReturnValueOnce(of({ ...saved, socialPushNotificationsEnabled: false }));
        facade.initialize();

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
        facade.initialize();

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
