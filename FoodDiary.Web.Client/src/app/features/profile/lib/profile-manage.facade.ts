import { inject, Injectable, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { FdUiConfirmDialogComponent } from 'fd-ui-kit/dialog/fd-ui-confirm-dialog';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { filter, finalize, firstValueFrom, switchMap, tap } from 'rxjs';

import {
    ConfirmDeleteDialogComponent,
    type ConfirmDeleteDialogData,
} from '../../../components/shared/confirm-delete-dialog/confirm-delete-dialog';
import { AuthService } from '../../../services/auth.service';
import { NavigationService } from '../../../services/navigation.service';
import { UserService } from '../../../shared/api/user.service';
import { TelegramBackupEmailFlowService } from '../../../shared/auth/telegram-backup-email-flow.service';
import { TelegramWebAppService } from '../../../shared/auth/telegram-web-app.service';
import { LocalizationService } from '../../../shared/i18n/localization.service';
import type { DietologistRelationship } from '../../../shared/models/dietologist.data';
import type { UpdateUserDto, User } from '../../../shared/models/user.data';
import {
    type NotificationPreferences,
    NotificationService,
    type WebPushSubscriptionItem,
} from '../../../shared/notifications/notification.service';
import { BrowserWindowService } from '../../../shared/platform/browser-window.service';
import { ThemeService } from '../../../shared/theme/theme.service';
import { ProfileMeasurementsService } from '../api/profile-measurements.service';
import { ChangePasswordDialogComponent } from '../dialogs/change-password-dialog/change-password-dialog';
import { PasswordSuccessDialogComponent } from '../dialogs/password-success-dialog/password-success-dialog';
import { UpdateSuccessDialogComponent } from '../dialogs/update-success-dialog/update-success-dialog';

const BACKUP_EMAIL_RESEND_MS = 60_000;

@Injectable()
export class ProfileManageFacade {
    private readonly userService = inject(UserService);
    private readonly translateService = inject(TranslateService);
    private readonly dialogService = inject(FdUiDialogService);
    private readonly toastService = inject(FdUiToastService);
    private readonly navigationService = inject(NavigationService);
    private readonly authService = inject(AuthService);
    private readonly localizationService = inject(LocalizationService);
    private readonly notificationService = inject(NotificationService);
    private readonly themeService = inject(ThemeService);
    private readonly profileMeasurementsService = inject(ProfileMeasurementsService);
    private readonly telegram = inject(TelegramWebAppService);
    private readonly browser = inject(BrowserWindowService);
    private readonly backupEmailFlow = inject(TelegramBackupEmailFlowService);

    public readonly user = signal<User | null>(null);
    public readonly globalError = signal<string | null>(null);
    public readonly isDeleting = signal(false);
    public readonly isSavingProfile = signal(false);
    public readonly profileSavedVersion = signal(0);
    public readonly isRevokingAiConsent = signal(false);
    public readonly isLinkingGoogle = signal(false);
    public readonly isUnlinkingTelegram = signal(false);
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
            const initData = this.browser.getTelegramInitData();
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

    public constructor() {}

    public initialize(): void {
        const pendingEmail = this.backupEmailFlow.read();
        if (pendingEmail !== null && pendingEmail.sentAt !== null) {
            this.backupEmailSentTo.set(pendingEmail.email);
            this.backupEmailResendAt.set(pendingEmail.sentAt + BACKUP_EMAIL_RESEND_MS);
        }
        this.loadUser();
        this.loadLatestMeasurements();
    }

    private loadLatestMeasurements(): void {
        this.profileMeasurementsService.getLatest().subscribe(summary => {
            this.currentWeight.set(summary.weightKg);
            this.currentWaist.set(summary.waistCm);
        });
    }

    public submitUpdate(updateData: UpdateUserDto): void {
        this.userService.update(updateData).subscribe({
            next: user => {
                if (user === null) {
                    this.setGlobalError('USER_MANAGE.UPDATE_ERROR');
                    return;
                }

                this.user.set(user);
                void this.localizationService.applyLanguagePreferenceAsync(user.language ?? null);
                this.themeService.syncWithUserPreferences(user.theme, user.uiStyle, user.surfaceStyle);
                this.clearGlobalError();
                this.showSuccessDialog();
            },
            error: () => {
                this.setGlobalError('USER_MANAGE.UPDATE_ERROR');
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

    public deleteAccount(): void {
        if (this.isDeleting() || this.isSavingProfile()) {
            return;
        }

        const data: ConfirmDeleteDialogData = {
            title: this.translateService.instant('USER_MANAGE.DELETE_ACCOUNT_CONFIRM_TITLE'),
            message: this.translateService.instant('USER_MANAGE.DELETE_ACCOUNT_CONFIRM_MESSAGE'),
            confirmLabel: this.translateService.instant('USER_MANAGE.DELETE_ACCOUNT_CONFIRM'),
            cancelLabel: this.translateService.instant('COMMON.CANCEL'),
        };

        this.dialogService
            .open(ConfirmDeleteDialogComponent, {
                preset: 'confirm',
                data,
            })
            .afterClosed()
            .pipe(
                filter((confirmed): confirmed is true => confirmed === true),
                filter(() => !this.isDeleting() && !this.isSavingProfile()),
                tap(() => {
                    this.isDeleting.set(true);
                }),
                switchMap(() =>
                    this.userService.deleteCurrentUser().pipe(
                        finalize(() => {
                            this.isDeleting.set(false);
                        }),
                    ),
                ),
            )
            .subscribe({
                next: success => {
                    if (!success) {
                        this.setGlobalError('USER_MANAGE.DELETE_ACCOUNT_ERROR');
                        return;
                    }

                    this.user.set(null);
                    this.clearGlobalError();
                    void this.authService.onLogoutAsync(true);
                },
                error: () => {
                    this.setGlobalError('USER_MANAGE.DELETE_ACCOUNT_ERROR');
                },
            });
    }

    public revokeAiConsent(): void {
        if (this.isRevokingAiConsent()) {
            return;
        }

        this.isRevokingAiConsent.set(true);
        this.userService
            .revokeAiConsent()
            .pipe(
                finalize(() => {
                    this.isRevokingAiConsent.set(false);
                }),
            )
            .subscribe({
                next: () => {
                    const current = this.user();
                    if (current !== null) {
                        this.user.set({ ...current, aiConsentAcceptedAt: null });
                    }
                },
                error: () => {
                    this.setGlobalError('USER_MANAGE.REVOKE_AI_CONSENT_ERROR');
                },
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
        if (!this.hasBackupLogin(user)) {
            this.setGlobalError('USER_MANAGE.TELEGRAM_BACKUP_REQUIRED');
            return;
        }
        this.isUnlinkingTelegram.set(true);
        try {
            const confirmed = await firstValueFrom(
                this.dialogService
                    .open(FdUiConfirmDialogComponent, {
                        preset: 'confirm',
                        data: {
                            title: this.translateService.instant('USER_MANAGE.TELEGRAM_UNLINK'),
                            message: this.translateService.instant('USER_MANAGE.TELEGRAM_UNLINK_CONFIRM'),
                            confirmLabel: this.translateService.instant('USER_MANAGE.TELEGRAM_UNLINK'),
                            cancelLabel: this.translateService.instant('COMMON.CANCEL'),
                        },
                    })
                    .afterClosed(),
            );
            if (confirmed !== true) {
                return;
            }
            await this.telegram.initializeAsync();
            const initData = this.browser.getTelegramInitData();
            if (initData === null || initData.length === 0) {
                this.setGlobalError('USER_MANAGE.TELEGRAM_REOPEN');
                return;
            }
            await firstValueFrom(this.authService.unlinkTelegram(initData));
            this.user.set({ ...user, hasTelegramIdentity: false });
            await this.authService.onLogoutAsync(true);
        } catch {
            this.setGlobalError('USER_MANAGE.TELEGRAM_UNLINK_ERROR');
        } finally {
            this.isUnlinkingTelegram.set(false);
        }
    }

    private hasBackupLogin(user: User): boolean {
        return user.hasGoogleIdentity === true || (user.hasPassword && user.isEmailConfirmed && user.email !== null);
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

    public saveProfileNow(updateData: UpdateUserDto): void {
        if (this.isSavingProfile()) {
            return;
        }

        this.persistProfileUpdate(updateData);
    }

    private loadUser(): void {
        this.userService.getOverview().subscribe({
            next: overview => {
                if (overview === null) {
                    this.setGlobalError('USER_MANAGE.LOAD_ERROR');
                    return;
                }

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
                this.user.set(null);
                this.webPushSubscriptions.set([]);
                this.dietologistRelationship.set(null);
                this.setGlobalError('USER_MANAGE.LOAD_ERROR');
            },
        });
    }

    private showSuccessDialog(): void {
        this.dialogService
            .open(UpdateSuccessDialogComponent, { size: 'sm' })
            .afterClosed()
            .subscribe(goToHome => {
                if (goToHome === true) {
                    void this.navigationService.navigateToHomeAsync();
                }
            });
    }

    private persistProfileUpdate(updateData: UpdateUserDto): void {
        this.isSavingProfile.set(true);
        this.userService
            .update(updateData)
            .pipe(
                finalize(() => {
                    this.isSavingProfile.set(false);
                }),
            )
            .subscribe({
                next: user => {
                    if (user === null) {
                        this.setGlobalError('USER_MANAGE.UPDATE_ERROR');
                    } else {
                        this.user.set(user);
                        this.profileSavedVersion.update(version => version + 1);
                        void this.localizationService.applyLanguagePreferenceAsync(user.language ?? null);
                        this.themeService.syncWithUserPreferences(user.theme, user.uiStyle, user.surfaceStyle);
                        this.clearGlobalError();
                    }
                },
                error: () => {
                    this.setGlobalError('USER_MANAGE.UPDATE_ERROR');
                },
            });
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
