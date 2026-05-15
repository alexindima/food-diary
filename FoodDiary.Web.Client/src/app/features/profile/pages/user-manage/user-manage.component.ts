import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import {
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    Component,
    computed,
    DestroyRef,
    effect,
    type FactoryProvider,
    inject,
    PLATFORM_ID,
    signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { type FormGroup, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiConfirmDialogComponent } from 'fd-ui-kit/dialog/fd-ui-confirm-dialog.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import {
    FD_VALIDATION_ERRORS,
    FdUiFormErrorComponent,
    type FdValidationErrors,
    getNumberProperty,
} from 'fd-ui-kit/form-error/fd-ui-form-error.component';
import type { FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select.component';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { EMPTY, finalize, merge, type Observable } from 'rxjs';

import { PageBodyComponent } from '../../../../components/shared/page-body/page-body.component';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header.component';
import { FdPageContainerDirective } from '../../../../directives/layout/page-container.directive';
import { ImageUploadService } from '../../../../shared/api/image-upload.service';
import type { DietologistPermissions, DietologistRelationship } from '../../../../shared/models/dietologist.data';
import { type ActivityLevelOption, type Gender, UpdateUserDto } from '../../../../shared/models/user.data';
import type { AppThemeName, AppUiStyleName } from '../../../../theme/app-theme.config';
import { DietologistService } from '../../../dietologist/api/dietologist.service';
import { PremiumBillingService } from '../../../premium/api/premium-billing.service';
import type { BillingOverview } from '../../../premium/models/billing.models';
import { ProfileManageFacade } from '../../lib/profile-manage.facade';
import { resolveTranslatedControlError } from '../../lib/profile-validation-error.mapper';
import { UserManageAccountCardComponent } from '../user-manage-sections/account-card/user-manage-account-card.component';
import { UserManageBillingCardComponent } from '../user-manage-sections/billing-card/user-manage-billing-card.component';
import { UserManageBodyCardComponent } from '../user-manage-sections/body-card/user-manage-body-card.component';
import { UserManageDietologistCardComponent } from '../user-manage-sections/dietologist-card/user-manage-dietologist-card.component';
import { UserManageNotificationsCardComponent } from '../user-manage-sections/notifications-card/user-manage-notifications-card.component';
import { UserManagePrivacyCardComponent } from '../user-manage-sections/privacy-card/user-manage-privacy-card.component';
import { DEFAULT_DIETOLOGIST_PERMISSIONS } from './user-manage.config';
import type {
    BillingViewModel,
    DietologistFormData,
    DietologistPermissionChange,
    DietologistPermissionControlName,
    PasswordActionState,
    ProfileStatusViewModel,
    UserFormData,
    UserFormValues,
} from './user-manage.types';
import { buildBillingView } from './user-manage-billing.mapper';
import { getDietologistPermissions, syncDietologistFormFromRelationship } from './user-manage-dietologist-form.mapper';
import { buildUserManageSelectOptions, createDietologistForm, createUserManageForm, mapUserToForm } from './user-manage-form.mapper';
import { UserManageNotificationsFacade } from './user-manage-notifications.facade';
import { buildProfileStatus } from './user-manage-profile-status.mapper';

export const VALIDATION_ERRORS_PROVIDER: FactoryProvider = {
    provide: FD_VALIDATION_ERRORS,
    useFactory: (): FdValidationErrors => ({
        required: () => 'FORM_ERRORS.REQUIRED',
        email: () => 'FORM_ERRORS.EMAIL',
        minlength: (error?: unknown) => ({
            key: 'FORM_ERRORS.PASSWORD.MIN_LENGTH',
            params: { requiredLength: getNumberProperty(error, 'requiredLength') },
        }),
    }),
};

@Component({
    selector: 'fd-user-manage',
    imports: [
        ReactiveFormsModule,
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
    templateUrl: './user-manage.component.html',
    styleUrl: './user-manage.component.scss',
    providers: [VALIDATION_ERRORS_PROVIDER, ProfileManageFacade, UserManageNotificationsFacade],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserManageComponent {
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly imageUploadService = inject(ImageUploadService);
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly facade = inject(ProfileManageFacade);
    public readonly notifications = inject(UserManageNotificationsFacade);
    private readonly dialogService = inject(FdUiDialogService);
    private readonly cdr = inject(ChangeDetectorRef);
    private readonly dietologistService = inject(DietologistService);
    private readonly billingService = inject(PremiumBillingService);
    private readonly toastService = inject(FdUiToastService);
    private readonly document = inject(DOCUMENT);
    private readonly platformId = inject(PLATFORM_ID);
    private readonly validationErrors = inject<FdValidationErrors>(FD_VALIDATION_ERRORS, { optional: true });
    private readonly isBrowser = isPlatformBrowser(this.platformId);
    private lastUserData: Partial<UserFormValues> | null = null;
    private lastNotificationSyncVersion = -1;
    private readonly pendingPasswordSetupIntent = signal(false);

    public genderOptions: Array<FdUiSelectOption<Gender | null>> = [];
    public activityLevelOptions: Array<FdUiSelectOption<ActivityLevelOption | null>> = [];
    public languageOptions: Array<FdUiSelectOption<string | null>> = [];
    public themeOptions: Array<FdUiSelectOption<AppThemeName | null>> = [];
    public uiStyleOptions: Array<FdUiSelectOption<AppUiStyleName | null>> = [];
    public userForm: FormGroup<UserFormData>;
    public dietologistForm: FormGroup<DietologistFormData>;
    public readonly globalError = this.facade.globalError;
    public readonly dietologistRelationship = this.facade.dietologistRelationship;
    public readonly dietologistError = signal<string | null>(null);
    public readonly dietologistPermissions = signal<DietologistPermissions>(DEFAULT_DIETOLOGIST_PERMISSIONS);
    public readonly isLoadingDietologist = signal(false);
    public readonly isSavingDietologist = signal(false);
    public readonly billingOverview = signal<BillingOverview | null>(null);
    public readonly isLoadingBilling = signal(false);
    public readonly isOpeningBillingPortal = signal(false);
    public readonly billingError = signal<string | null>(null);
    public readonly isDeleting = this.facade.isDeleting;
    public readonly isSavingProfile = this.facade.isSavingProfile;
    public readonly isRevokingAiConsent = this.facade.isRevokingAiConsent;
    public readonly isSurfaceBusy = computed(() => this.surfaceBusySignals().some(isBusy => isBusy));
    public readonly hasAiConsent = computed(() => {
        const acceptedAt = this.facade.user()?.aiConsentAcceptedAt;
        return acceptedAt !== null && acceptedAt !== undefined && acceptedAt.length > 0;
    });
    public readonly hasPassword = computed(() => this.facade.user()?.hasPassword ?? true);
    public readonly passwordActionState = computed<PasswordActionState>(() => {
        const hasPassword = this.hasPassword();

        return {
            buttonLabelKey: hasPassword ? 'USER_MANAGE.CHANGE_PASSWORD' : 'USER_MANAGE.SET_PASSWORD',
            descriptionKey: hasPassword ? 'USER_MANAGE.CHANGE_PASSWORD_DESCRIPTION' : 'USER_MANAGE.SET_PASSWORD_DESCRIPTION',
        };
    });
    public readonly hasDietologistRelationship = computed(() => this.dietologistRelationship() !== null);
    public readonly isDietologistPending = computed(() => this.dietologistRelationship()?.status === 'Pending');
    public readonly isDietologistConnected = computed(() => this.dietologistRelationship()?.status === 'Accepted');
    public readonly profileStatus = signal<ProfileStatusViewModel>({
        key: 'USER_MANAGE.PROFILE_STATUS_SAVED',
        tone: 'success',
    });
    public readonly dietologistInviteEmailError = signal<string | null>(null);
    public readonly billingView = computed<BillingViewModel | null>(() => buildBillingView(this.billingOverview()));

    public constructor() {
        this.userForm = createUserManageForm();
        this.dietologistForm = createDietologistForm();

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
            this.lastUserData = userData;
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
        this.userForm.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.facade.clearGlobalError();
            this.queueUserAutosave();
            this.updateProfileStatus();
        });
        this.userForm.statusChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.updateProfileStatus();
        });
    }

    private watchDietologistFormChanges(): void {
        this.dietologistForm.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.dietologistError.set(null);
            this.updateDietologistPermissionsState();
        });
        const dietologistFormEvents = (this.dietologistForm as { events?: Observable<unknown> }).events ?? EMPTY;
        merge(dietologistFormEvents, this.dietologistForm.statusChanges, this.dietologistForm.valueChanges)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => {
                this.updateDietologistInviteEmailError();
            });
    }

    public onSubmit(): void {
        this.userForm.markAllAsTouched();

        if (this.userForm.valid) {
            this.facade.saveProfileNow(this.buildUserUpdateDto());
        }
    }

    public resetForm(): void {
        this.facade.clearGlobalError();
        const currentImageId = this.userForm.controls.profileImage.value?.assetId;
        const baselineImageId = this.lastUserData?.profileImage?.assetId ?? null;
        if (currentImageId !== null && currentImageId !== undefined && currentImageId.length > 0 && currentImageId !== baselineImageId) {
            this.imageUploadService.deleteAsset(currentImageId).subscribe({
                error: () => {
                    // swallow errors to avoid blocking reset
                },
            });
        }

        if (this.lastUserData !== null) {
            this.applyUserData(this.lastUserData);
        } else {
            this.facade.initialize();
        }
    }

    public openChangePasswordDialog(): void {
        this.facade.openChangePasswordDialog();
    }

    public onRevokeAiConsent(): void {
        this.facade.revokeAiConsent();
    }

    public onDeleteAccount(): void {
        this.facade.deleteAccount();
    }

    public inviteDietologist(): void {
        if (this.isSavingDietologist()) {
            return;
        }

        this.dietologistForm.controls.email.markAsTouched();
        this.updateDietologistInviteEmailError();
        if (this.dietologistForm.invalid) {
            return;
        }

        this.isSavingDietologist.set(true);
        this.dietologistService
            .invite({
                dietologistEmail: this.dietologistForm.controls.email.getRawValue(),
                permissions: getDietologistPermissions(this.dietologistForm),
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

    public updateDietologistPermission(controlName: DietologistPermissionControlName, nextValue: boolean): void {
        if (!this.hasDietologistRelationship() || this.isSavingDietologist()) {
            return;
        }

        const previousPermissions = getDietologistPermissions(this.dietologistForm);
        this.dietologistForm.controls[controlName].setValue(nextValue);
        this.persistDietologistPermissions(previousPermissions);
    }

    public onDietologistPermissionChangeRequest(change: DietologistPermissionChange): void {
        this.updateDietologistPermission(change.controlName, change.value);
    }

    public persistDietologistPermissions(previousPermissions?: DietologistPermissions): void {
        if (!this.hasDietologistRelationship() || this.isSavingDietologist()) {
            return;
        }

        this.isSavingDietologist.set(true);
        this.dietologistService
            .updatePermissions(getDietologistPermissions(this.dietologistForm))
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
                        this.dietologistForm.patchValue(previousPermissions, { emitEvent: false });
                        this.updateDietologistPermissionsState();
                        this.dietologistForm.markAsPristine();
                        this.dietologistForm.markAsUntouched();
                        this.cdr.markForCheck();
                    }

                    this.setDietologistError('USER_MANAGE.DIETOLOGIST_PERMISSIONS_ERROR');
                },
            });
    }

    public revokeDietologistRelationship(): void {
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

                    this.cdr.markForCheck();
                });
            return;
        }

        this.executeDietologistRevoke();
    }

    public onDietologistProfileToggle(nextValue: boolean): void {
        if (this.isSavingDietologist()) {
            return;
        }

        if (nextValue) {
            this.dietologistForm.controls.shareProfile.setValue(nextValue);
            if (this.hasDietologistRelationship()) {
                this.persistDietologistPermissions({
                    ...getDietologistPermissions(this.dietologistForm),
                    shareProfile: !nextValue,
                });
            }
            this.cdr.markForCheck();
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
                    const previousPermissions = getDietologistPermissions(this.dietologistForm);
                    this.dietologistForm.controls.shareProfile.setValue(false);
                    if (this.hasDietologistRelationship()) {
                        this.persistDietologistPermissions(previousPermissions);
                    }
                }

                this.cdr.markForCheck();
            });
    }

    private updateDietologistInviteEmailError(): void {
        this.dietologistInviteEmailError.set(
            resolveTranslatedControlError(this.dietologistForm.controls.email, this.validationErrors, this.translateService),
        );
    }

    private updateProfileStatus(): void {
        this.profileStatus.set(this.buildProfileStatus());
    }

    private buildProfileStatus(): ProfileStatusViewModel {
        return buildProfileStatus({
            globalError: this.globalError(),
            isSaving: this.isSavingProfile(),
            isDirty: this.userForm.dirty,
            isValid: this.userForm.valid,
        });
    }

    public reloadBillingOverview(): void {
        this.loadBillingOverview();
    }

    public openPremiumPage(): void {
        void this.router.navigate(['/premium']);
    }

    public openBillingPortal(): void {
        if (!this.isBrowser || this.isOpeningBillingPortal()) {
            return;
        }

        this.billingError.set(null);
        this.isOpeningBillingPortal.set(true);
        this.billingService
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
        this.userForm.patchValue(userData, { emitEvent: false });
        this.userForm.markAsPristine();
        this.userForm.markAsUntouched();
        this.updateProfileStatus();
    }

    private queueUserAutosave(): void {
        if (!this.userForm.dirty || !this.userForm.valid) {
            return;
        }

        this.facade.queueProfileAutosave(this.buildUserUpdateDto());
    }

    private buildUserUpdateDto(): UpdateUserDto {
        const formData = this.userForm.getRawValue();
        return new UpdateUserDto({
            ...formData,
            profileImage: formData.profileImage,
        });
    }

    private loadDietologistRelationship(): void {
        this.isLoadingDietologist.set(true);
        this.dietologistService
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
        this.billingService
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
        syncDietologistFormFromRelationship(this.dietologistForm, relationship);
        this.updateDietologistPermissionsState();
        this.cdr.markForCheck();
    }

    private updateDietologistPermissionsState(): void {
        this.dietologistPermissions.set(getDietologistPermissions(this.dietologistForm));
    }

    private setDietologistError(errorKey: string): void {
        this.dietologistError.set(this.translateService.instant(errorKey));
    }

    private executeDietologistRevoke(): void {
        this.isSavingDietologist.set(true);
        this.dietologistService
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
