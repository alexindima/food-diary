import { DestroyRef, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { finalize, firstValueFrom, merge, type Subscription, take } from 'rxjs';

import { AuthService } from '../../../services/auth.service';
import { UserService } from '../../../shared/api/user.service';
import { SessionEventsService } from '../../../shared/auth/session-events.service';
import { TelegramBackupEmailFlowService } from '../../../shared/auth/telegram-backup-email-flow.service';
import { TelegramUnlinkFlowService } from '../../../shared/auth/telegram-unlink-flow.service';
import { TelegramWebAppService } from '../../../shared/auth/telegram-web-app.service';
import { LocalizationService } from '../../../shared/i18n/localization.service';
import { RequestStateController } from '../../../shared/lib/request-state';
import type { DietologistRelationship } from '../../../shared/models/dietologist.data';
import type { UpdateUserDto, User } from '../../../shared/models/user.data';
import {
    type NotificationPreferences,
    NotificationService,
    type WebPushSubscriptionItem,
} from '../../../shared/notifications/notification.service';
import { ThemeService } from '../../../shared/theme/theme.service';
import { ProfileMeasurementsService } from '../api/profile-measurements.service';
import { ChangePasswordDialogComponent } from '../dialogs/change-password-dialog/change-password-dialog';
import { PasswordSuccessDialogComponent } from '../dialogs/password-success-dialog/password-success-dialog';
import {
    TelegramDisconnectDialogComponent,
    type TelegramDisconnectDialogData,
} from '../dialogs/telegram-disconnect-dialog/telegram-disconnect-dialog';

const BACKUP_EMAIL_RESEND_MS = 60_000;

@Injectable()
export class ProfileManageFacade {
    private readonly destroyRef = inject(DestroyRef);
    private readonly profileRequest = new RequestStateController<true>();
    private readonly measurementsRequest = new RequestStateController<true>();
    private profileRead: Subscription | undefined;
    private measurementsRead: Subscription | undefined;
    private readonly userService = inject(UserService);
    private readonly translateService = inject(TranslateService);
    private readonly dialogService = inject(FdUiDialogService);
    private readonly toastService = inject(FdUiToastService);
    private readonly authService = inject(AuthService);
    private readonly sessionEvents = inject(SessionEventsService);
    private readonly localizationService = inject(LocalizationService);
    private readonly notificationService = inject(NotificationService);
    private readonly themeService = inject(ThemeService);
    private readonly profileMeasurementsService = inject(ProfileMeasurementsService);
    private readonly telegram = inject(TelegramWebAppService);
    private readonly backupEmailFlow = inject(TelegramBackupEmailFlowService);
    private readonly unlinkFlow = inject(TelegramUnlinkFlowService);

    public readonly user = signal<User | null>(null);
    public readonly globalError = signal<string | null>(null);
    public readonly isSavingProfile = signal(false);
    public readonly profileSavedVersion = signal(0);
    public readonly isLinkingGoogle = signal(false);
    public readonly isUnlinkingTelegram = signal(false);
    public readonly telegramUnlinkError = signal<string | null>(null);
    public readonly isRequestingBackupEmail = signal(false);
    public readonly backupEmailSentTo = signal<string | null>(null);
    public readonly backupEmailResendAt = signal(0);

    public async requestBackupEmailAsync(email: string): Promise<void> {
        const user = this.user();
        if (user?.hasTelegramIdentity !== true || user.email !== null || this.isRequestingBackupEmail()) {
            return;
        }
        this.isRequestingBackupEmail.set(true);
        this.clearGlobalError();
        try {
            await this.telegram.initializeAsync();
            if (this.destroyRef.destroyed) {
                return;
            }

            const initData = this.telegram.getInitData();
            if (initData === null || initData.length === 0) {
                await this.backupEmailFlow.startAsync(email.trim());
                return;
            }
            await firstValueFrom(this.authService.requestTelegramBackupEmail(email.trim(), initData));
            this.backupEmailSentTo.set(email.trim());
            this.backupEmailResendAt.set(Date.now() + BACKUP_EMAIL_RESEND_MS);
            this.backupEmailFlow.markSent(email.trim());
        } catch {
            this.setGlobalError('USER_MANAGE.BACKUP_EMAIL_ERROR');
        } finally {
            this.isRequestingBackupEmail.set(false);
        }
    }
    public readonly isUpdatingNotifications = signal(false);
    public readonly webPushSubscriptions = signal<WebPushSubscriptionItem[]>([]);
    public readonly dietologistRelationship = signal<DietologistRelationship | null>(null);
    public readonly currentWeight = signal<number | null>(null);
    public readonly currentWaist = signal<number | null>(null);
    public readonly isLoadingWebPushSubscriptions = signal(false);
    public readonly removingWebPushSubscriptionEndpoint = signal<string | null>(null);
    private webPushSubscriptionsRequestId = 0;

    public constructor() {
        this.destroyRef.onDestroy(() => {
            this.profileRequest.reset();
            this.measurementsRequest.reset();
        });
    }

    public initialize(): void {
        this.telegramUnlinkError.set(this.unlinkFlow.consumeError());
        const pendingEmail = this.backupEmailFlow.read();
        if (pendingEmail !== null && pendingEmail.sentAt !== null) {
            this.backupEmailSentTo.set(pendingEmail.email);
            this.backupEmailResendAt.set(pendingEmail.sentAt + BACKUP_EMAIL_RESEND_MS);
        }
        this.loadUser();
        this.loadLatestMeasurements();
    }

    private loadLatestMeasurements(): void {
        this.measurementsRead?.unsubscribe();
        const requestId = this.measurementsRequest.begin();
        this.measurementsRead = this.profileMeasurementsService
            .getLatest()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: summary => {
                    if (this.measurementsRequest.succeed(requestId, true)) {
                        this.currentWeight.set(summary.weightKg);
                        this.currentWaist.set(summary.waistCm);
                    }
                },
                error: () => {
                    this.measurementsRequest.fail(requestId, 'USER_MANAGE.LOAD_ERROR');
                },
            });
    }

    public openChangePasswordDialog(): void {
        const user = this.user();
        if (user !== null && !user.hasPassword && (user.email === null || !user.isEmailConfirmed)) {
            this.setGlobalError('USER_MANAGE.BACKUP_EMAIL_PASSWORD_REQUIRED');
            return;
        }
        this.dialogService
            .open(ChangePasswordDialogComponent, {
                preset: 'form',
                data: {
                    hasPassword: this.user()?.hasPassword ?? true,
                },
            })
            .afterClosed()
            .subscribe(success => {
                if (success === true) {
                    const current = this.user();
                    if (current !== null && !current.hasPassword) {
                        this.user.set({ ...current, hasPassword: true });
                    }
                    this.openPasswordSuccessDialog();
                }
            });
    }

    public linkGoogle(credential: string): void {
        if (this.isLinkingGoogle() || credential.length === 0) {
            return;
        }

        this.isLinkingGoogle.set(true);
        this.authService
            .linkGoogle(credential)
            .pipe(
                finalize(() => {
                    this.isLinkingGoogle.set(false);
                }),
            )
            .subscribe({
                next: user => {
                    this.user.set(user);
                    this.clearGlobalError();
                    this.toastService.success(this.translateService.instant('USER_MANAGE.GOOGLE_LINK_SUCCESS'));
                },
                error: () => {
                    this.setGlobalError('USER_MANAGE.GOOGLE_LINK_ERROR');
                    this.toastService.error(this.translateService.instant('USER_MANAGE.GOOGLE_LINK_ERROR'));
                },
            });
    }

    public async unlinkTelegramAsync(): Promise<void> {
        const user = this.user();
        if (user?.hasTelegramIdentity !== true || this.isUnlinkingTelegram()) {
            return;
        }
        const { canUsePassword, canUseGoogle } = this.getTelegramDisconnectOptions(user);
        if (!canUsePassword && !canUseGoogle) {
            this.telegramUnlinkError.set('USER_MANAGE.TELEGRAM_BACKUP_REQUIRED');
            return;
        }
        this.isUnlinkingTelegram.set(true);
        this.telegramUnlinkError.set(null);
        try {
            const updated = await firstValueFrom(
                this.dialogService
                    .open<TelegramDisconnectDialogComponent, TelegramDisconnectDialogData, User>(TelegramDisconnectDialogComponent, {
                        data: { owner: user.id, canUsePassword, canUseGoogle },
                    })
                    .afterClosed(),
            );
            if (updated !== undefined && updated.id === this.user()?.id) {
                this.user.set(updated);
                this.toastService.success(this.translateService.instant('USER_MANAGE.TELEGRAM_DISCONNECT_SUCCESS'));
            }
        } catch {
            this.telegramUnlinkError.set('USER_MANAGE.TELEGRAM_UNLINK_ERROR');
        } finally {
            this.isUnlinkingTelegram.set(false);
        }
    }

    private getTelegramDisconnectOptions(user: User): Omit<TelegramDisconnectDialogData, 'owner'> {
        return {
            canUsePassword: user.hasPassword && user.isEmailConfirmed && user.email !== null,
            canUseGoogle: user.hasGoogleIdentity === true,
        };
    }

    public async updateNotificationPreferencesAsync(preferences: {
        pushNotificationsEnabled?: boolean;
        fastingPushNotificationsEnabled?: boolean;
        socialPushNotificationsEnabled?: boolean;
        fastingCheckInReminderHours?: number;
        fastingCheckInFollowUpReminderHours?: number;
    }): Promise<User | null> {
        if (this.isUpdatingNotifications()) {
            return this.user();
        }

        this.isUpdatingNotifications.set(true);

        try {
            const notificationPreferences = await firstValueFrom(this.notificationService.updateNotificationPreferences(preferences));
            this.applyNotificationPreferences(notificationPreferences);
            this.clearGlobalError();
            return this.user();
        } catch {
            this.setGlobalError('USER_MANAGE.UPDATE_ERROR');
            return null;
        } finally {
            this.isUpdatingNotifications.set(false);
        }
    }

    public clearGlobalError(): void {
        this.globalError.set(null);
    }

    public async saveProfileAsync(updateData: UpdateUserDto): Promise<boolean> {
        if (this.user() === null || this.isSavingProfile()) {
            return false;
        }

        this.cancelProfileRead();
        this.isSavingProfile.set(true);
        // A submitted update and its session check outlive ordinary Profile navigation.
        const sessionChange = merge(this.sessionEvents.authenticated$, this.sessionEvents.sessionEnded$).pipe(take(1)).subscribe();
        try {
            const user = await firstValueFrom(this.userService.update(updateData));
            if (sessionChange.closed) {
                return false;
            }
            if (user === null) {
                this.setGlobalError('USER_MANAGE.UPDATE_ERROR');
                return false;
            }

            this.user.set(user);
            this.profileSavedVersion.update(version => version + 1);
            void this.localizationService.applyLanguagePreferenceAsync(user.language ?? null);
            this.themeService.syncWithUserPreferences(user.theme, user.uiStyle, user.surfaceStyle);
            this.clearGlobalError();
            return true;
        } catch {
            if (!sessionChange.closed) {
                this.setGlobalError('USER_MANAGE.UPDATE_ERROR');
            }
            return false;
        } finally {
            sessionChange.unsubscribe();
            this.isSavingProfile.set(false);
        }
    }

    private loadUser(): void {
        this.profileRead?.unsubscribe();
        const requestId = this.profileRequest.begin();
        this.profileRead = this.userService
            .getOverview()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: overview => {
                    if (!this.profileRequest.isCurrent(requestId)) {
                        return;
                    }
                    if (overview === null) {
                        this.profileRequest.fail(requestId, 'USER_MANAGE.LOAD_ERROR');
                        this.setGlobalError('USER_MANAGE.LOAD_ERROR');
                        return;
                    }

                    this.profileRequest.succeed(requestId, true);
                    this.user.set(overview.user);
                    this.applyNotificationPreferences(overview.notificationPreferences);
                    this.webPushSubscriptions.set(overview.webPushSubscriptions);
                    this.dietologistRelationship.set(overview.dietologistRelationship);
                    this.clearGlobalError();
                    if (this.backupEmailFlow.read()?.failed === true) {
                        this.setGlobalError('USER_MANAGE.BACKUP_EMAIL_ERROR');
                        this.backupEmailFlow.clear();
                    }
                    void this.localizationService.applyLanguagePreferenceAsync(overview.user.language ?? null);
                    this.themeService.syncWithUserPreferences(overview.user.theme, overview.user.uiStyle, overview.user.surfaceStyle);
                },
                error: () => {
                    if (!this.profileRequest.fail(requestId, 'USER_MANAGE.LOAD_ERROR')) {
                        return;
                    }
                    this.user.set(null);
                    this.webPushSubscriptions.set([]);
                    this.dietologistRelationship.set(null);
                    this.setGlobalError('USER_MANAGE.LOAD_ERROR');
                },
            });
    }

    private cancelProfileRead(): void {
        this.profileRead?.unsubscribe();
        this.profileRequest.reset();
    }

    private openPasswordSuccessDialog(): void {
        this.dialogService
            .open(PasswordSuccessDialogComponent, { size: 'sm' })
            .afterClosed()
            .subscribe(() => {
                void this.authService.onLogoutAsync(true);
            });
    }

    private setGlobalError(errorKey: string): void {
        this.globalError.set(this.translateService.instant(errorKey));
    }

    private applyNotificationPreferences(preferences: NotificationPreferences): void {
        const current = this.user();
        if (current === null) {
            return;
        }

        this.user.set({
            ...current,
            pushNotificationsEnabled: preferences.pushNotificationsEnabled,
            fastingPushNotificationsEnabled: preferences.fastingPushNotificationsEnabled,
            socialPushNotificationsEnabled: preferences.socialPushNotificationsEnabled,
            fastingCheckInReminderHours: preferences.fastingCheckInReminderHours,
            fastingCheckInFollowUpReminderHours: preferences.fastingCheckInFollowUpReminderHours,
        });
    }

    public refreshWebPushSubscriptions(): void {
        this.loadWebPushSubscriptions();
    }

    public async removeWebPushSubscriptionAsync(endpoint: string): Promise<boolean> {
        if (endpoint.length === 0 || this.removingWebPushSubscriptionEndpoint() !== null) {
            return false;
        }

        this.removingWebPushSubscriptionEndpoint.set(endpoint);

        try {
            await firstValueFrom(this.notificationService.removeWebPushSubscription(endpoint));
            this.webPushSubscriptions.update(items => items.filter(item => item.endpoint !== endpoint));
            return true;
        } catch {
            return false;
        } finally {
            this.removingWebPushSubscriptionEndpoint.set(null);
        }
    }

    private loadWebPushSubscriptions(): void {
        const requestId = ++this.webPushSubscriptionsRequestId;
        this.isLoadingWebPushSubscriptions.set(true);

        this.notificationService
            .getWebPushSubscriptions()
            .pipe(
                finalize(() => {
                    if (requestId === this.webPushSubscriptionsRequestId) {
                        this.isLoadingWebPushSubscriptions.set(false);
                    }
                }),
            )
            .subscribe({
                next: subscriptions => {
                    if (requestId === this.webPushSubscriptionsRequestId) {
                        this.webPushSubscriptions.set(subscriptions);
                    }
                },
                error: () => {},
            });
    }
}
