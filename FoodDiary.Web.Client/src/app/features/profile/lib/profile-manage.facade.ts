import { computed, DestroyRef, inject, Injectable, linkedSignal, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { finalize, firstValueFrom, merge, type Subscription, take, tap } from 'rxjs';

import { AuthService } from '../../../services/auth.service';
import { type UserProfileOverview, UserService } from '../../../shared/api/user.service';
import { SessionEventsService } from '../../../shared/auth/session-events.service';
import { TelegramBackupEmailFlowService } from '../../../shared/auth/telegram-backup-email-flow.service';
import { TelegramUnlinkFlowService } from '../../../shared/auth/telegram-unlink-flow.service';
import { TelegramWebAppService } from '../../../shared/auth/telegram-web-app.service';
import { LocalizationService } from '../../../shared/i18n/localization.service';
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
    private lastNotificationPreferenceSave: NotificationPreferences | null = null;

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
        const address = email.trim();
        const sessionChange = merge(this.sessionEvents.authenticated$, this.sessionEvents.sessionEnded$).pipe(take(1)).subscribe();
        const ownsView = (): boolean => !this.destroyRef.destroyed && !sessionChange.closed;
        try {
            await this.telegram.initializeAsync();
            if (!ownsView()) {
                return;
            }

            const initData = this.telegram.getInitData();
            if (initData === null || initData.length === 0) {
                await this.backupEmailFlow.startAsync(address);
                return;
            }
            await this.sendMiniAppBackupEmailAsync(address, initData, sessionChange);
        } catch {
            if (ownsView()) {
                this.setGlobalError('USER_MANAGE.BACKUP_EMAIL_ERROR');
            }
        } finally {
            sessionChange.unsubscribe();
            this.isRequestingBackupEmail.set(false);
        }
    }

    private async sendMiniAppBackupEmailAsync(address: string, initData: string, sessionChange: Subscription): Promise<void> {
        await firstValueFrom(this.authService.requestTelegramBackupEmail(address, initData));
        if (sessionChange.closed) {
            return;
        }
        if (!this.destroyRef.destroyed) {
            this.backupEmailSentTo.set(address);
            this.backupEmailResendAt.set(Date.now() + BACKUP_EMAIL_RESEND_MS);
        }
        // Profile restores the receipt when this session returns to the page.
        this.backupEmailFlow.markSent(address);
    }

    public readonly isUpdatingNotifications = signal(false);
    private readonly dietologistRelationshipState = signal<DietologistRelationship | null>(null);
    public readonly dietologistRelationship = this.dietologistRelationshipState.asReadonly();
    private dietologistRelationshipVersion = 0;
    private readonly profileRequest = signal<{ relationshipVersion: number } | undefined>(undefined);
    private readonly profileResource = rxResource({
        params: this.profileRequest,
        stream: ({ params }) =>
            this.userService.getOverview().pipe(
                tap({
                    next: overview => {
                        if (this.profileRequest() === params) {
                            this.applyProfileOverview(overview, params.relationshipVersion);
                        }
                    },
                    error: () => {
                        if (this.profileRequest() === params) {
                            this.user.set(null);
                            this.webPushSubscriptionList.set([]);
                            this.applyOverviewDietologistRelationship(null, params.relationshipVersion);
                            this.setGlobalError('USER_MANAGE.LOAD_ERROR');
                        }
                    },
                }),
            ),
    });
    private readonly webPushRequest = signal<number | undefined>(undefined);
    private readonly webPushResource = rxResource({
        params: this.webPushRequest,
        stream: () => this.notificationService.getWebPushSubscriptions(),
    });
    private readonly webPushSubscriptionList = linkedSignal({
        source: this.webPushResource.snapshot,
        computation: (snapshot, previous): WebPushSubscriptionItem[] =>
            snapshot.status === 'resolved' ? (snapshot.value ?? []) : (previous?.value ?? []),
    });
    public readonly webPushSubscriptions = this.webPushSubscriptionList.asReadonly();
    public readonly isLoadingWebPushSubscriptions = this.webPushResource.isLoading;
    public readonly hasWebPushSubscriptionsLoadError = computed(() => this.webPushResource.error() !== undefined);
    public readonly removingWebPushSubscriptionEndpoint = signal<string | null>(null);
    private readonly measurementsRequest = signal<number | undefined>(undefined);
    private readonly weightResource = rxResource({
        params: this.measurementsRequest,
        stream: () => this.profileMeasurementsService.getLatestWeight(),
    });
    private readonly waistResource = rxResource({
        params: this.measurementsRequest,
        stream: () => this.profileMeasurementsService.getLatestWaist(),
    });
    public readonly currentWeight = computed(() => (this.weightResource.hasValue() ? this.weightResource.value() : null));
    public readonly currentWaist = computed(() => (this.waistResource.hasValue() ? this.waistResource.value() : null));
    public readonly hasWeightLoadError = computed(() => this.weightResource.error() !== undefined);
    public readonly hasWaistLoadError = computed(() => this.waistResource.error() !== undefined);

    public retryWeight(): void {
        this.weightResource.reload();
    }

    public retryWaist(): void {
        this.waistResource.reload();
    }

    public initialize(): void {
        this.telegramUnlinkError.set(this.unlinkFlow.consumeError());
        const pendingEmail = this.backupEmailFlow.read();
        if (pendingEmail !== null && pendingEmail.sentAt !== null) {
            this.backupEmailSentTo.set(pendingEmail.email);
            this.backupEmailResendAt.set(pendingEmail.sentAt + BACKUP_EMAIL_RESEND_MS);
        }
        this.profileRequest.set({ relationshipVersion: this.dietologistRelationshipVersion });
        this.measurementsRequest.update(version => (version ?? 0) + 1);
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
                    hasPassword: hasProfilePassword(user),
                    ownerDestroyRef: this.destroyRef,
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
                    if (this.destroyRef.destroyed) {
                        return;
                    }
                    this.user.set(user);
                    this.clearGlobalError();
                    this.toastService.success(this.translateService.instant('USER_MANAGE.GOOGLE_LINK_SUCCESS'));
                },
                error: () => {
                    if (this.destroyRef.destroyed) {
                        return;
                    }
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
            this.lastNotificationPreferenceSave = notificationPreferences;
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

        this.profileRequest.set(undefined);
        this.profileResource.set(undefined);
        this.isSavingProfile.set(true);
        const preferencesAtRequest = this.lastNotificationPreferenceSave;
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

            this.user.set(this.mergeNotificationChangesDuringSave(user, preferencesAtRequest));
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

    public setDietologistRelationship(relationship: DietologistRelationship | null): void {
        this.dietologistRelationshipVersion++;
        this.dietologistRelationshipState.set(relationship);
    }

    private mergeNotificationChangesDuringSave(user: User, preferencesAtRequest: NotificationPreferences | null): User {
        const latestPreferences = this.lastNotificationPreferenceSave;
        return latestPreferences !== null && latestPreferences !== preferencesAtRequest
            ? withNotificationPreferences(user, latestPreferences)
            : user;
    }

    private applyProfileOverview(overview: UserProfileOverview | null, relationshipVersion: number): void {
        if (overview === null) {
            this.setGlobalError('USER_MANAGE.LOAD_ERROR');
            return;
        }

        this.user.set(overview.user);
        this.applyNotificationPreferences(overview.notificationPreferences);
        if (this.hasWebPushSubscriptionsLoadError()) {
            this.webPushResource.set(undefined);
        }
        this.webPushSubscriptionList.set(overview.webPushSubscriptions);
        this.applyOverviewDietologistRelationship(overview.dietologistRelationship, relationshipVersion);
        this.clearGlobalError();
        if (this.backupEmailFlow.read()?.failed === true) {
            this.setGlobalError('USER_MANAGE.BACKUP_EMAIL_ERROR');
            this.backupEmailFlow.clear();
        }
        void this.localizationService.applyLanguagePreferenceAsync(overview.user.language ?? null);
        this.themeService.syncWithUserPreferences(overview.user.theme, overview.user.uiStyle, overview.user.surfaceStyle);
    }

    private applyOverviewDietologistRelationship(relationship: DietologistRelationship | null, version: number): void {
        if (version === this.dietologistRelationshipVersion) {
            this.dietologistRelationshipState.set(relationship);
        }
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

        this.user.set(withNotificationPreferences(current, preferences));
    }

    public refreshWebPushSubscriptions(): void {
        // Preserve the latest successful snapshot even if the device list hasn't read it yet.
        this.webPushSubscriptionList();
        this.webPushRequest.update(version => (version ?? 0) + 1);
    }

    public async removeWebPushSubscriptionAsync(endpoint: string): Promise<boolean> {
        if (endpoint.length === 0 || this.removingWebPushSubscriptionEndpoint() !== null) {
            return false;
        }

        this.removingWebPushSubscriptionEndpoint.set(endpoint);

        try {
            await firstValueFrom(this.notificationService.removeWebPushSubscription(endpoint));
            this.webPushSubscriptionList.update(items => items.filter(item => item.endpoint !== endpoint));
            return true;
        } catch {
            return false;
        } finally {
            this.removingWebPushSubscriptionEndpoint.set(null);
        }
    }
}

export function hasProfilePassword(user: User | null): boolean {
    // Require a current password until a loaded profile explicitly reports otherwise.
    return user?.hasPassword ?? true;
}

function withNotificationPreferences(user: User, preferences: NotificationPreferences): User {
    return {
        ...user,
        pushNotificationsEnabled: preferences.pushNotificationsEnabled,
        fastingPushNotificationsEnabled: preferences.fastingPushNotificationsEnabled,
        socialPushNotificationsEnabled: preferences.socialPushNotificationsEnabled,
        fastingCheckInReminderHours: preferences.fastingCheckInReminderHours,
        fastingCheckInFollowUpReminderHours: preferences.fastingCheckInFollowUpReminderHours,
    };
}
