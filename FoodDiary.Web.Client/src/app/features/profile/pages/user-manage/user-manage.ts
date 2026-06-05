import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, PLATFORM_ID, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { disabled, email, form, required } from '@angular/forms/signals';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiConfirmDialogComponent } from 'fd-ui-kit/dialog/fd-ui-confirm-dialog';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import {
    FD_VALIDATION_ERRORS,
    FdUiFormErrorComponent,
    type FdValidationErrors,
    resolveSignalFormFieldError,
} from 'fd-ui-kit/form-error/fd-ui-form-error';
import type { FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { finalize } from 'rxjs';

import { PageBodyComponent } from '../../../../components/shared/page-body/page-body';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header';
import type { DietologistPermissions, DietologistRelationship } from '../../../../shared/models/dietologist.data';
import { type ActivityLevelOption, type Gender, UpdateUserDto } from '../../../../shared/models/user.data';
import { FdPageContainerDirective } from '../../../../shared/ui/layout/page-container.directive';
import type { AppThemeName, AppUiStyleName } from '../../../../theme/app-theme.config';
import { DietologistFacade } from '../../../dietologist/lib/dietologist.facade';
import { PremiumBillingFacade } from '../../../premium/lib/premium-billing.facade';
import type { BillingOverview } from '../../../premium/models/billing.models';
import { ProfileManageFacade } from '../../lib/profile-manage.facade';
import { UserManageAccountCardComponent } from '../user-manage-sections/account-card/user-manage-account-card';
import { UserManageBillingCardComponent } from '../user-manage-sections/billing-card/user-manage-billing-card';
import { UserManageBodyCardComponent } from '../user-manage-sections/body-card/user-manage-body-card';
import { UserManageDietologistCardComponent } from '../user-manage-sections/dietologist-card/user-manage-dietologist-card';
import { UserManageNotificationsCardComponent } from '../user-manage-sections/notifications-card/user-manage-notifications-card';
import { UserManagePrivacyCardComponent } from '../user-manage-sections/privacy-card/user-manage-privacy-card';
import { DEFAULT_DIETOLOGIST_PERMISSIONS } from './user-manage-lib/user-manage.config';
import type {
    BillingViewModel,
    DietologistFormValues,
    DietologistPermissionChange,
    DietologistPermissionControlName,
    PasswordActionState,
    ProfileStatusViewModel,
    UserFormValues,
} from './user-manage-lib/user-manage.types';
import { buildBillingView } from './user-manage-lib/user-manage-billing.mapper';
import { getDietologistPermissions, mapDietologistRelationshipToForm } from './user-manage-lib/user-manage-dietologist-form.mapper';
import {
    buildUserManageSelectOptions,
    createDietologistFormModel,
    createUserManageFormModel,
    mapUserToForm,
} from './user-manage-lib/user-manage-form.mapper';
import { UserManageNotificationsFacade } from './user-manage-lib/user-manage-notifications.facade';
import { buildProfileStatus } from './user-manage-lib/user-manage-profile-status.mapper';

@Component({
    selector: 'fd-user-manage',
    imports: [
        TranslatePipe,
        FdUiFormErrorComponent,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        UserManageAccountCardComponent,
        UserManageBillingCardComponent,
        UserManageBodyCardComponent,
        UserManageDietologistCardComponent,
        UserManageNotificationsCardComponent,
        UserManagePrivacyCardComponent,
    ],
    templateUrl: './user-manage.html',
    styleUrl: './user-manage.scss',
    providers: [ProfileManageFacade, UserManageNotificationsFacade],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserManageComponent {
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly facade = inject(ProfileManageFacade);
    protected readonly notifications = inject(UserManageNotificationsFacade);
    private readonly dialogService = inject(FdUiDialogService);
    private readonly dietologistFacade = inject(DietologistFacade);
    private readonly billingFacade = inject(PremiumBillingFacade);
    private readonly toastService = inject(FdUiToastService);
    private readonly document = inject(DOCUMENT);
    private readonly platformId = inject(PLATFORM_ID);
    private readonly validationErrors = inject<FdValidationErrors>(FD_VALIDATION_ERRORS, { optional: true });
    private readonly isBrowser = isPlatformBrowser(this.platformId);
    private lastNotificationSyncVersion = -1;
    private readonly pendingPasswordSetupIntent = signal(false);

    protected genderOptions: Array<FdUiSelectOption<Gender | null>> = [];
    protected activityLevelOptions: Array<FdUiSelectOption<ActivityLevelOption | null>> = [];
    protected languageOptions: Array<FdUiSelectOption<string | null>> = [];
    protected themeOptions: Array<FdUiSelectOption<AppThemeName | null>> = [];
    protected uiStyleOptions: Array<FdUiSelectOption<AppUiStyleName | null>> = [];
    protected readonly userFormModel = signal<UserFormValues>(createUserManageFormModel());
    protected readonly userForm = form(this.userFormModel);
    protected readonly dietologistFormModel = signal<DietologistFormValues>(createDietologistFormModel());
    protected readonly dietologistForm = form(this.dietologistFormModel, path => {
        required(path.email);
        email(path.email);
        disabled(path.email, { when: () => this.hasDietologistRelationship() });
    });
    protected readonly globalError = this.facade.globalError;
    protected readonly dietologistRelationship = this.facade.dietologistRelationship;
    protected readonly dietologistError = signal<string | null>(null);
    protected readonly dietologistPermissions = signal<DietologistPermissions>(DEFAULT_DIETOLOGIST_PERMISSIONS);
    protected readonly isLoadingDietologist = signal(false);
    protected readonly isSavingDietologist = signal(false);
    protected readonly billingOverview = signal<BillingOverview | null>(null);
    protected readonly isLoadingBilling = signal(false);
    protected readonly isOpeningBillingPortal = signal(false);
    protected readonly billingError = signal<string | null>(null);
    protected readonly isDeleting = this.facade.isDeleting;
    protected readonly isSavingProfile = this.facade.isSavingProfile;
    protected readonly isRevokingAiConsent = this.facade.isRevokingAiConsent;
    protected readonly isSurfaceBusy = computed(() => this.surfaceBusySignals().some(isBusy => isBusy));
    protected readonly hasAiConsent = computed(() => {
        const acceptedAt = this.facade.user()?.aiConsentAcceptedAt;
        return acceptedAt !== null && acceptedAt !== undefined && acceptedAt.length > 0;
    });
    protected readonly hasPassword = computed(() => this.facade.user()?.hasPassword ?? true);
    protected readonly passwordActionState = computed<PasswordActionState>(() => {
        const hasPassword = this.hasPassword();

        return {
            buttonLabelKey: hasPassword ? 'USER_MANAGE.CHANGE_PASSWORD' : 'USER_MANAGE.SET_PASSWORD',
            descriptionKey: hasPassword ? 'USER_MANAGE.CHANGE_PASSWORD_DESCRIPTION' : 'USER_MANAGE.SET_PASSWORD_DESCRIPTION',
        };
    });
    protected readonly hasDietologistRelationship = computed(() => this.dietologistRelationship() !== null);
    protected readonly isDietologistPending = computed(() => this.dietologistRelationship()?.status === 'Pending');
    protected readonly isDietologistConnected = computed(() => this.dietologistRelationship()?.status === 'Accepted');
    protected readonly profileStatus = signal<ProfileStatusViewModel>({
        key: 'USER_MANAGE.PROFILE_STATUS_SAVED',
        tone: 'success',
    });
    protected readonly dietologistInviteEmailError = signal<string | null>(null);
    protected readonly billingView = computed<BillingViewModel | null>(() => buildBillingView(this.billingOverview()));

    public constructor() {
        this.buildSelectOptions();
        this.watchLanguageChanges();
        this.watchPasswordSetupIntent();
        this.watchUserProfile();
        this.watchPasswordSetupDialog();
        this.watchDietologistRelationship();
        this.watchProfileStatusSignals();
        this.watchNotificationRelationshipRefresh();
        this.watchUserFormChanges();
        this.watchDietologistFormChanges();
        this.updateDietologistInviteEmailError();

        this.facade.initialize();
        this.loadBillingOverview();
    }

    private watchLanguageChanges(): void {
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.buildSelectOptions();
            this.updateDietologistInviteEmailError();
        });
    }

    private watchPasswordSetupIntent(): void {
        this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(params => {
            this.pendingPasswordSetupIntent.set(params.get('intent') === 'set-password');
        });
    }

    private watchUserProfile(): void {
        effect(() => {
            const user = this.facade.user();
            if (user === null) {
                return;
            }

            const userData = mapUserToForm(user);
            this.applyUserData(userData);
            this.notifications.syncFromUser(user);
        });
    }

    private watchPasswordSetupDialog(): void {
        effect(() => {
            const user = this.facade.user();
            if (user === null || !this.pendingPasswordSetupIntent()) {
                return;
            }

            void this.clearProfileIntentQueryParamAsync();
            this.pendingPasswordSetupIntent.set(false);

            if (!user.hasPassword) {
                this.facade.openChangePasswordDialog();
            }
        });
    }

    private watchDietologistRelationship(): void {
        effect(() => {
            this.syncDietologistFormFromRelationship(this.facade.dietologistRelationship());
        });
    }

    private watchProfileStatusSignals(): void {
        effect(() => {
            this.globalError();
            this.isSavingProfile();
            this.updateProfileStatus();
        });
    }

    private watchNotificationRelationshipRefresh(): void {
        effect(() => {
            const version = this.notifications.notificationsChangedVersion();
            if (version === this.lastNotificationSyncVersion) {
                return;
            }

            this.lastNotificationSyncVersion = version;
            if (version === 0) {
                return;
            }

            this.loadDietologistRelationship();
        });
    }

    private watchUserFormChanges(): void {
        effect(() => {
            this.userFormModel();
            this.facade.clearGlobalError();
            this.queueUserAutosave();
            this.updateProfileStatus();
        });
    }

    private watchDietologistFormChanges(): void {
        effect(() => {
            this.dietologistFormModel();
            this.dietologistError.set(null);
            this.updateDietologistPermissionsState();
            this.updateDietologistInviteEmailError();
        });
    }

    protected onSubmit(): void {
        this.userForm().markAsTouched();
        this.facade.saveProfileNow(this.buildUserUpdateDto());
    }

    protected openChangePasswordDialog(): void {
        this.facade.openChangePasswordDialog();
    }

    protected onRevokeAiConsent(): void {
        this.facade.revokeAiConsent();
    }

    protected onDeleteAccount(): void {
        this.facade.deleteAccount();
    }

    protected inviteDietologist(): void {
        if (this.isSavingDietologist()) {
            return;
        }

        this.dietologistForm.email().markAsTouched();
        this.updateDietologistInviteEmailError();
        if (this.dietologistForm().invalid()) {
            return;
        }

        this.isSavingDietologist.set(true);
        this.dietologistFacade
            .invite({
                dietologistEmail: this.dietologistFormModel().email,
                permissions: getDietologistPermissions(this.dietologistFormModel()),
            })
            .pipe(
                finalize(() => {
                    this.isSavingDietologist.set(false);
                }),
            )
            .subscribe({
                next: () => {
                    this.toastService.success(this.translateService.instant('USER_MANAGE.DIETOLOGIST_INVITE_SUCCESS'));
                    this.loadDietologistRelationship();
                },
                error: () => {
                    this.setDietologistError('USER_MANAGE.DIETOLOGIST_INVITE_ERROR');
                },
            });
    }

    protected updateDietologistPermission(controlName: DietologistPermissionControlName, nextValue: boolean): void {
        if (!this.hasDietologistRelationship() || this.isSavingDietologist()) {
            return;
        }

        const previousPermissions = getDietologistPermissions(this.dietologistFormModel());
        this.dietologistForm[controlName]().value.set(nextValue);
        this.persistDietologistPermissions(previousPermissions);
    }

    protected onDietologistPermissionChangeRequest(change: DietologistPermissionChange): void {
        this.updateDietologistPermission(change.controlName, change.value);
    }

    protected persistDietologistPermissions(previousPermissions?: DietologistPermissions): void {
        if (!this.hasDietologistRelationship() || this.isSavingDietologist()) {
            return;
        }

        this.isSavingDietologist.set(true);
        this.dietologistFacade
            .updatePermissions(getDietologistPermissions(this.dietologistFormModel()))
            .pipe(
                finalize(() => {
                    this.isSavingDietologist.set(false);
                }),
            )
            .subscribe({
                next: () => {
                    this.dietologistError.set(null);
                    this.loadDietologistRelationship();
                },
                error: () => {
                    if (previousPermissions !== undefined) {
                        this.dietologistForm().reset({
                            ...this.dietologistFormModel(),
                            ...previousPermissions,
                        });
                        this.updateDietologistPermissionsState();
                    }

                    this.setDietologistError('USER_MANAGE.DIETOLOGIST_PERMISSIONS_ERROR');
                },
            });
    }

    protected revokeDietologistRelationship(): void {
        if (!this.hasDietologistRelationship() || this.isSavingDietologist()) {
            return;
        }

        if (this.isDietologistConnected()) {
            this.dialogService
                .open(FdUiConfirmDialogComponent, {
                    preset: 'confirm',
                    data: {
                        title: this.translateService.instant('USER_MANAGE.DIETOLOGIST_DISCONNECT_TITLE'),
                        message: this.translateService.instant('USER_MANAGE.DIETOLOGIST_DISCONNECT_MESSAGE'),
                        confirmLabel: this.translateService.instant('USER_MANAGE.DIETOLOGIST_DISCONNECT_CONFIRM'),
                        cancelLabel: this.translateService.instant('COMMON.CANCEL'),
                    },
                })
                .afterClosed()
                .subscribe(confirmed => {
                    if (confirmed === true) {
                        this.executeDietologistRevoke();
                    }
                });
            return;
        }

        this.executeDietologistRevoke();
    }

    protected onDietologistProfileToggle(nextValue: boolean): void {
        if (this.isSavingDietologist()) {
            return;
        }

        if (nextValue) {
            this.dietologistForm.shareProfile().value.set(nextValue);
            if (this.hasDietologistRelationship()) {
                this.persistDietologistPermissions({
                    ...getDietologistPermissions(this.dietologistFormModel()),
                    shareProfile: !nextValue,
                });
            }
            return;
        }

        this.dialogService
            .open(FdUiConfirmDialogComponent, {
                preset: 'confirm',
                data: {
                    title: this.translateService.instant('USER_MANAGE.DIETOLOGIST_PROFILE_DISABLE_TITLE'),
                    message: this.translateService.instant('USER_MANAGE.DIETOLOGIST_PROFILE_DISABLE_MESSAGE'),
                    confirmLabel: this.translateService.instant('USER_MANAGE.DIETOLOGIST_PROFILE_DISABLE_CONFIRM'),
                    cancelLabel: this.translateService.instant('USER_MANAGE.DIETOLOGIST_PROFILE_DISABLE_CANCEL'),
                },
            })
            .afterClosed()
            .subscribe(confirmed => {
                if (confirmed === true) {
                    const previousPermissions = getDietologistPermissions(this.dietologistFormModel());
                    this.dietologistForm.shareProfile().value.set(false);
                    if (this.hasDietologistRelationship()) {
                        this.persistDietologistPermissions(previousPermissions);
                    }
                }
            });
    }

    private updateDietologistInviteEmailError(): void {
        this.dietologistInviteEmailError.set(
            resolveSignalFormFieldError(this.dietologistForm.email, this.validationErrors, this.translateService),
        );
    }

    private updateProfileStatus(): void {
        this.profileStatus.set(this.buildProfileStatus());
    }

    private buildProfileStatus(): ProfileStatusViewModel {
        return buildProfileStatus({
            globalError: this.globalError(),
            isSaving: this.isSavingProfile(),
            isDirty: this.userForm().dirty(),
            isValid: true,
        });
    }

    protected reloadBillingOverview(): void {
        this.loadBillingOverview();
    }

    protected openPremiumPage(): void {
        void this.router.navigate(['/premium']);
    }

    protected openBillingPortal(): void {
        if (!this.isBrowser || this.isOpeningBillingPortal()) {
            return;
        }

        this.billingError.set(null);
        this.isOpeningBillingPortal.set(true);
        this.billingFacade
            .createPortalSession()
            .pipe(
                finalize(() => {
                    this.isOpeningBillingPortal.set(false);
                }),
            )
            .subscribe({
                next: session => {
                    if (session.url.length === 0) {
                        this.billingError.set('USER_MANAGE.BILLING_PORTAL_ERROR');
                        this.toastService.error(this.translateService.instant('USER_MANAGE.BILLING_PORTAL_ERROR'));
                        return;
                    }

                    this.document.location.href = session.url;
                },
                error: () => {
                    this.billingError.set('USER_MANAGE.BILLING_PORTAL_ERROR');
                    this.toastService.error(this.translateService.instant('USER_MANAGE.BILLING_PORTAL_ERROR'));
                },
            });
    }

    private applyUserData(userData: Partial<UserFormValues>): void {
        this.userForm().reset({
            ...createUserManageFormModel(),
            ...userData,
        });
        this.updateProfileStatus();
    }

    private queueUserAutosave(): void {
        if (!this.userForm().dirty()) {
            return;
        }

        this.facade.queueProfileAutosave(this.buildUserUpdateDto());
    }

    private buildUserUpdateDto(): UpdateUserDto {
        const formData = this.userFormModel();
        return new UpdateUserDto({
            ...formData,
            profileImage: formData.profileImage,
        });
    }

    private loadDietologistRelationship(): void {
        this.isLoadingDietologist.set(true);
        this.dietologistFacade
            .getRelationship()
            .pipe(
                finalize(() => {
                    this.isLoadingDietologist.set(false);
                }),
            )
            .subscribe({
                next: relationship => {
                    this.facade.dietologistRelationship.set(relationship);
                    this.dietologistError.set(null);
                },
                error: () => {
                    this.setDietologistError('USER_MANAGE.DIETOLOGIST_LOAD_ERROR');
                },
            });
    }

    private loadBillingOverview(): void {
        this.isLoadingBilling.set(true);
        this.billingError.set(null);
        this.billingFacade
            .getOverview()
            .pipe(
                finalize(() => {
                    this.isLoadingBilling.set(false);
                }),
            )
            .subscribe({
                next: overview => {
                    this.billingOverview.set(overview);
                },
                error: () => {
                    this.billingError.set('USER_MANAGE.BILLING_LOAD_ERROR');
                },
            });
    }

    private syncDietologistFormFromRelationship(relationship: DietologistRelationship | null): void {
        const model = mapDietologistRelationshipToForm(relationship);
        this.dietologistForm().reset(model);
        this.dietologistPermissions.set(getDietologistPermissions(model));
    }

    private updateDietologistPermissionsState(): void {
        this.dietologistPermissions.set(getDietologistPermissions(this.dietologistFormModel()));
    }

    private setDietologistError(errorKey: string): void {
        this.dietologistError.set(this.translateService.instant(errorKey));
    }

    private executeDietologistRevoke(): void {
        this.isSavingDietologist.set(true);
        this.dietologistFacade
            .revokeRelationship()
            .pipe(
                finalize(() => {
                    this.isSavingDietologist.set(false);
                }),
            )
            .subscribe({
                next: () => {
                    this.toastService.info(this.translateService.instant('USER_MANAGE.DIETOLOGIST_DISCONNECTED'));
                    this.facade.dietologistRelationship.set(null);
                },
                error: () => {
                    this.setDietologistError('USER_MANAGE.DIETOLOGIST_DISCONNECT_ERROR');
                },
            });
    }

    private buildSelectOptions(): void {
        const options = buildUserManageSelectOptions(key => this.translateService.instant(key));
        this.genderOptions = options.genderOptions;
        this.activityLevelOptions = options.activityLevelOptions;
        this.languageOptions = options.languageOptions;
        this.themeOptions = options.themeOptions;
        this.uiStyleOptions = options.uiStyleOptions;
    }

    private async clearProfileIntentQueryParamAsync(): Promise<void> {
        await this.router.navigate([], {
            relativeTo: this.route,
            queryParams: { intent: null },
            queryParamsHandling: 'merge',
            replaceUrl: true,
        });
    }

    private surfaceBusySignals(): boolean[] {
        return [
            this.isSavingProfile(),
            this.isDeleting(),
            this.isRevokingAiConsent(),
            this.notifications.isUpdatingNotifications(),
            this.notifications.isSchedulingTestNotification(),
            this.isSavingDietologist(),
            this.isLoadingDietologist(),
            this.isLoadingBilling(),
            this.isOpeningBillingPortal(),
            this.notifications.isLoadingConnectedDevices(),
            this.notifications.removingConnectedDeviceEndpoint() !== null,
        ];
    }
}
