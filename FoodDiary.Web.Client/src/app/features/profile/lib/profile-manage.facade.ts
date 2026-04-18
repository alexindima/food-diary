import { Injectable, inject, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { finalize, firstValueFrom } from 'rxjs';
import {
    ConfirmDeleteDialogComponent,
    ConfirmDeleteDialogData,
} from '../../../components/shared/confirm-delete-dialog/confirm-delete-dialog.component';
import { AuthService } from '../../../services/auth.service';
import { LocalizationService } from '../../../services/localization.service';
import { NavigationService } from '../../../services/navigation.service';
import { NotificationPreferences, NotificationService, WebPushSubscriptionItem } from '../../../services/notification.service';
import { ThemeService } from '../../../services/theme.service';
import { UserService } from '../../../shared/api/user.service';
import { DietologistRelationship } from '../../dietologist/models/dietologist.data';
import { UpdateUserDto, User } from '../../../shared/models/user.data';
import { AutosaveQueue, createAutosaveQueue } from '../../../shared/lib/autosave-queue';
import { ChangePasswordDialogComponent } from '../dialogs/change-password-dialog/change-password-dialog.component';
import { PasswordSuccessDialogComponent } from '../dialogs/password-success-dialog/password-success-dialog.component';
import { UpdateSuccessDialogComponent } from '../dialogs/update-success-dialog/update-success-dialog.component';

@Injectable()
export class ProfileManageFacade {
    private readonly userService = inject(UserService);
    private readonly translateService = inject(TranslateService);
    private readonly dialogService = inject(FdUiDialogService);
    private readonly navigationService = inject(NavigationService);
    private readonly authService = inject(AuthService);
    private readonly localizationService = inject(LocalizationService);
    private readonly notificationService = inject(NotificationService);
    private readonly themeService = inject(ThemeService);
    private readonly profileAutosaveQueue: AutosaveQueue<UpdateUserDto> = createAutosaveQueue({
        debounceMs: 700,
        isBusy: () => this.isSavingProfile(),
        persist: updateData => this.persistProfileUpdate(updateData),
    });

    public readonly user = signal<User | null>(null);
    public readonly globalError = signal<string | null>(null);
    public readonly isDeleting = signal(false);
    public readonly isSavingProfile = signal(false);
    public readonly isUpdatingNotifications = signal(false);
    public readonly webPushSubscriptions = signal<WebPushSubscriptionItem[]>([]);
    public readonly dietologistRelationship = signal<DietologistRelationship | null>(null);
    public readonly isLoadingWebPushSubscriptions = signal(false);
    public readonly removingWebPushSubscriptionEndpoint = signal<string | null>(null);

    public constructor() {}

    public initialize(): void {
        this.loadUser();
    }

    public submitUpdate(updateData: UpdateUserDto): void {
        this.userService.update(updateData).subscribe({
            next: user => {
                if (!user) {
                    this.setGlobalError('USER_MANAGE.UPDATE_ERROR');
                    return;
                }

                this.user.set(user);
                void this.localizationService.applyLanguagePreference(user.language ?? null);
                this.themeService.syncWithUserPreferences(user.theme, user.uiStyle);
                this.clearGlobalError();
                this.showSuccessDialog();
            },
            error: () => {
                this.setGlobalError('USER_MANAGE.UPDATE_ERROR');
            },
        });
    }

    public queueProfileAutosave(updateData: UpdateUserDto): void {
        this.profileAutosaveQueue.schedule(updateData);
    }

    public openChangePasswordDialog(): void {
        this.dialogService
            .open(ChangePasswordDialogComponent, { size: 'sm' })
            .afterClosed()
            .subscribe(success => {
                if (success) {
                    this.openPasswordSuccessDialog();
                }
            });
    }

    public deleteAccount(): void {
        if (this.isDeleting()) {
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
                size: 'sm',
                data,
            })
            .afterClosed()
            .subscribe(confirmed => {
                if (!confirmed || this.isDeleting()) {
                    return;
                }

                this.isDeleting.set(true);
                this.userService
                    .deleteCurrentUser()
                    .pipe(finalize(() => this.isDeleting.set(false)))
                    .subscribe({
                        next: success => {
                            if (!success) {
                                this.setGlobalError('USER_MANAGE.DELETE_ACCOUNT_ERROR');
                                return;
                            }

                            this.user.set(null);
                            this.clearGlobalError();
                            void this.authService.onLogout(true);
                        },
                        error: () => {
                            this.setGlobalError('USER_MANAGE.DELETE_ACCOUNT_ERROR');
                        },
                    });
            });
    }

    public revokeAiConsent(): void {
        this.userService.revokeAiConsent().subscribe({
            next: () => {
                const current = this.user();
                if (current) {
                    this.user.set({ ...current, aiConsentAcceptedAt: null });
                }
            },
            error: () => {
                this.setGlobalError('USER_MANAGE.REVOKE_AI_CONSENT_ERROR');
            },
        });
    }

    public async updateNotificationPreferences(preferences: {
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
            if (!notificationPreferences) {
                this.setGlobalError('USER_MANAGE.UPDATE_ERROR');
                return null;
            }

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
        this.profileAutosaveQueue.flushNow(updateData);
    }

    private loadUser(): void {
        this.userService.getOverview().subscribe({
            next: overview => {
                if (!overview) {
                    this.setGlobalError('USER_MANAGE.LOAD_ERROR');
                    return;
                }

                this.user.set(overview.user);
                this.applyNotificationPreferences(overview.notificationPreferences);
                this.webPushSubscriptions.set(overview.webPushSubscriptions);
                this.dietologistRelationship.set(overview.dietologistRelationship);
                this.clearGlobalError();
                void this.localizationService.applyLanguagePreference(overview.user.language ?? null);
                this.themeService.syncWithUserPreferences(overview.user.theme, overview.user.uiStyle);
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
                if (goToHome) {
                    void this.navigationService.navigateToHome();
                }
            });
    }

    private persistProfileUpdate(updateData: UpdateUserDto): void {
        this.isSavingProfile.set(true);
        this.userService
            .update(updateData)
            .pipe(finalize(() => this.isSavingProfile.set(false)))
            .subscribe({
                next: user => {
                    if (!user) {
                        this.setGlobalError('USER_MANAGE.UPDATE_ERROR');
                    } else {
                        this.user.set(user);
                        void this.localizationService.applyLanguagePreference(user.language ?? null);
                        this.themeService.syncWithUserPreferences(user.theme, user.uiStyle);
                        this.clearGlobalError();
                    }

                    this.profileAutosaveQueue.scheduleIfPending();
                },
                error: () => {
                    this.setGlobalError('USER_MANAGE.UPDATE_ERROR');
                    const hasQueuedUpdate = this.profileAutosaveQueue.hasPending();
                    if (!hasQueuedUpdate) {
                        this.profileAutosaveQueue.restore(updateData);
                        return;
                    }

                    this.profileAutosaveQueue.scheduleIfPending();
                },
            });
    }

    private openPasswordSuccessDialog(): void {
        this.dialogService.open(PasswordSuccessDialogComponent, { size: 'sm' }).afterClosed().subscribe();
    }

    private setGlobalError(errorKey: string): void {
        this.globalError.set(this.translateService.instant(errorKey));
    }

    private applyNotificationPreferences(preferences: NotificationPreferences): void {
        const current = this.user();
        if (!current) {
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

    public async removeWebPushSubscription(endpoint: string): Promise<boolean> {
        if (!endpoint || this.removingWebPushSubscriptionEndpoint()) {
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
        this.isLoadingWebPushSubscriptions.set(true);

        this.notificationService
            .getWebPushSubscriptions()
            .pipe(finalize(() => this.isLoadingWebPushSubscriptions.set(false)))
            .subscribe({
                next: subscriptions => {
                    this.webPushSubscriptions.set(subscriptions);
                },
                error: () => {
                    this.webPushSubscriptions.set([]);
                },
            });
    }
}
