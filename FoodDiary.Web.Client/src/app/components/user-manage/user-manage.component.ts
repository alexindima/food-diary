import {
    ChangeDetectionStrategy,
    Component,
    DestroyRef,
    FactoryProvider,
    inject,
    OnInit,
    signal,
} from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FormGroupControls } from '../../types/common.data';
import { UserService } from '../../services/user.service';
import { ActivityLevelOption, Gender, UpdateUserDto, User } from '../../types/user.data';
import { NavigationService } from '../../services/navigation.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiSelectComponent, FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select.component';
import { FdUiDateInputComponent } from 'fd-ui-kit/date-input/fd-ui-date-input.component';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiFormErrorComponent, FD_VALIDATION_ERRORS, FdValidationErrors } from 'fd-ui-kit/form-error/fd-ui-form-error.component';
import { ChangePasswordDialogComponent } from './dialogs/change-password-dialog/change-password-dialog.component';
import { PasswordSuccessDialogComponent } from './dialogs/password-success-dialog/password-success-dialog.component';
import { UpdateSuccessDialogComponent } from './dialogs/update-success-dialog/update-success-dialog.component';
import { PageHeaderComponent } from '../shared/page-header/page-header.component';
import { PageBodyComponent } from "../shared/page-body/page-body.component";
import { FdPageContainerDirective } from "../../directives/layout/page-container.directive";
import { ImageUploadFieldComponent } from '../shared/image-upload-field/image-upload-field.component';
import { ImageSelection } from '../../types/image-upload.data';
import { ImageUploadService } from "../../services/image-upload.service";
import {
    ConfirmDeleteDialogComponent,
    ConfirmDeleteDialogData,
} from '../shared/confirm-delete-dialog/confirm-delete-dialog.component';
import { AuthService } from '../../services/auth.service';

export const VALIDATION_ERRORS_PROVIDER: FactoryProvider = {
    provide: FD_VALIDATION_ERRORS,
    useFactory: (): FdValidationErrors => ({
        required: () => 'FORM_ERRORS.REQUIRED',
        email: () => 'FORM_ERRORS.EMAIL',
        minlength: (error?: unknown) => ({
            key: 'FORM_ERRORS.PASSWORD.MIN_LENGTH',
            params: { requiredLength: (error as { requiredLength?: number } | undefined)?.requiredLength },
        }),
    }),
};

@Component({
    selector: 'fd-user-manage',
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        FdUiCardComponent,
        FdUiInputComponent,
        FdUiSelectComponent,
        FdUiDateInputComponent,
        FdUiButtonComponent,
        FdUiFormErrorComponent,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        ImageUploadFieldComponent,
    ],
    templateUrl: './user-manage.component.html',
    styleUrl: './user-manage.component.scss',
    providers: [VALIDATION_ERRORS_PROVIDER],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class UserManageComponent implements OnInit {
    private readonly userService = inject(UserService);
    private readonly translateService = inject(TranslateService);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly navigationService = inject(NavigationService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly imageUploadService = inject(ImageUploadService);
    private readonly authService = inject(AuthService);
    private lastUserData: Partial<UserFormValues> | null = null;

    public genders = Object.values(Gender);
    public activityLevels: ActivityLevelOption[] = ['MINIMAL', 'LIGHT', 'MODERATE', 'HIGH', 'EXTREME'];
    public languageCodes: string[] = ['en', 'ru'];
    public genderOptions: FdUiSelectOption<Gender | null>[] = [];
    public activityLevelOptions: FdUiSelectOption<ActivityLevelOption | null>[] = [];
    public languageOptions: FdUiSelectOption<string | null>[] = [];

    public userForm: FormGroup<UserFormData>;
    public globalError = signal<string | null>(null);
    public isDeleting = signal<boolean>(false);

    public constructor() {
        this.userForm = new FormGroup<UserFormData>({
            email: new FormControl<string | null>({ value: '', disabled: true }),
            username: new FormControl<string | null>(null),
            firstName: new FormControl<string | null>(null),
            lastName: new FormControl<string | null>(null),
            birthDate: new FormControl<Date | null>(null),
            gender: new FormControl<Gender | null>(null),
            language: new FormControl<string | null>(null),
            height: new FormControl<number | null>(null),
            activityLevel: new FormControl<ActivityLevelOption | null>(null),
            dailyCalorieTarget: new FormControl<number | null>(null),
            proteinTarget: new FormControl<number | null>(null),
            fatTarget: new FormControl<number | null>(null),
            carbTarget: new FormControl<number | null>(null),
            stepGoal: new FormControl<number | null>(null),
            waterGoal: new FormControl<number | null>(null),
            profileImage: new FormControl<ImageSelection | null>(null),
        });

        this.buildSelectOptions();
        this.translateService.onLangChange
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => this.buildSelectOptions());
    }

    public ngOnInit(): void {
        this.userForm.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.clearGlobalError());
        this.userForm.controls.language.valueChanges
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(language => {
                if (!this.userForm.controls.language.dirty) {
                    return;
                }
                this.applyLanguagePreference(language ?? null);
            });

        this.loadUserData();
    }

    private loadUserData(): void {
        this.userService.getInfo().subscribe({
            next: user => {
                if (user) {
                    const userData = this.mapUserToForm(user);
                    this.lastUserData = userData;
                    this.applyUserData(userData);
                    this.applyLanguagePreference(user.language ?? null);
                } else {
                    this.setGlobalError('USER_MANAGE.LOAD_ERROR');
                }
            },
            error: () => {
                this.setGlobalError('USER_MANAGE.LOAD_ERROR');
            },
        });
    }

    private applyUserData(userData: Partial<UserFormValues>): void {
        this.userForm.patchValue(userData);
        this.userForm.markAsPristine();
        this.userForm.markAsUntouched();
    }

    public async onSubmit(): Promise<void> {
        this.userForm.markAllAsTouched();

        if (this.userForm.valid) {
            const formData = this.userForm.value;
            const updateData = new UpdateUserDto({
                ...formData,
                profileImage: formData.profileImage as ImageSelection | null,
            });

            this.userService.update(updateData).subscribe({
                next: user => {
                    if (user) {
                        const updatedData = this.mapUserToForm(user);
                        this.lastUserData = updatedData;
                        this.applyUserData(updatedData);
                        this.applyLanguagePreference(user.language ?? null);
                        this.showSuccessDialog();
                    } else {
                        this.setGlobalError('USER_MANAGE.UPDATE_ERROR');
                    }
                },
                error: () => {
                    this.setGlobalError('USER_MANAGE.UPDATE_ERROR');
                },
            });
        }
    }

    public resetForm(): void {
        this.clearGlobalError();
        const currentImageId = this.userForm.controls.profileImage.value?.assetId;
        const baselineImageId = this.lastUserData?.profileImage?.assetId ?? null;
        if (currentImageId && currentImageId !== baselineImageId) {
            this.imageUploadService.deleteAsset(currentImageId).subscribe({
                error: () => {
                    // swallow errors to avoid blocking reset
                },
            });
        }
        if (this.lastUserData) {
            this.applyUserData(this.lastUserData);
        } else {
            this.loadUserData();
        }
    }

    public openChangePasswordDialog(): void {
        this.fdDialogService
            .open(ChangePasswordDialogComponent, {
                size: 'sm',
            })
            .afterClosed()
            .subscribe(success => {
                if (success) {
                    this.openPasswordSuccessDialog();
                }
            });
    }

    private openPasswordSuccessDialog(): void {
        this.fdDialogService.open(PasswordSuccessDialogComponent, { size: 'sm' }).afterClosed().subscribe();
    }

    public onGenderSelect(value: Gender | null): void {
        this.userForm.controls.gender.setValue(value);
        this.userForm.controls.gender.markAsDirty();
        this.userForm.controls.gender.markAsTouched();
    }

    public onDeleteAccount(): void {
        if (this.isDeleting()) {
            return;
        }

        const data: ConfirmDeleteDialogData = {
            title: this.translateService.instant('USER_MANAGE.DELETE_ACCOUNT_CONFIRM_TITLE'),
            message: this.translateService.instant('USER_MANAGE.DELETE_ACCOUNT_CONFIRM_MESSAGE'),
            confirmLabel: this.translateService.instant('USER_MANAGE.DELETE_ACCOUNT_CONFIRM'),
            cancelLabel: this.translateService.instant('COMMON.CANCEL'),
        };

        this.fdDialogService
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
                this.userService.deleteCurrentUser().subscribe({
                    next: success => {
                        this.isDeleting.set(false);
                        if (!success) {
                            this.setGlobalError('USER_MANAGE.DELETE_ACCOUNT_ERROR');
                            return;
                        }

                        void this.authService.onLogout(true);
                    },
                    error: () => {
                        this.isDeleting.set(false);
                        this.setGlobalError('USER_MANAGE.DELETE_ACCOUNT_ERROR');
                    },
                });
            });
    }

    protected showSuccessDialog(): void {
        this.fdDialogService
            .open(UpdateSuccessDialogComponent, {
                size: 'sm',
            })
            .afterClosed()
            .subscribe(goToHome => {
                if (goToHome) {
                    this.navigationService.navigateToHome();
                }
            });
    }

    private setGlobalError(errorKey: string): void {
        this.globalError.set(this.translateService.instant(errorKey));
    }

    private clearGlobalError(): void {
        this.globalError.set(null);
    }

    private buildSelectOptions(): void {
        this.genderOptions = this.genders.map(gender => ({
            label: this.translateService.instant(`USER_MANAGE.GENDER_OPTIONS.${gender}`),
            value: gender,
        }));

        this.activityLevelOptions = this.activityLevels.map(level => ({
            label: this.translateService.instant(`USER_MANAGE.ACTIVITY_LEVEL_OPTIONS.${level}`),
            value: level,
        }));

        this.languageOptions = this.languageCodes.map(code => ({
            label: this.translateService.instant(`USER_MANAGE.LANGUAGE_OPTIONS.${code.toUpperCase()}`),
            value: code,
        }));
    }

    private applyLanguagePreference(language: string | null): void {
        if (!language) {
            return;
        }

        const current = this.translateService.currentLang || this.translateService.getDefaultLang();
        if (current === language) {
            return;
        }

        this.translateService.use(language).subscribe();
    }

    private mapUserToForm(user: User): Partial<UserFormValues> {
        return {
            email: user.email ?? null,
            username: user.username ?? null,
            firstName: user.firstName ?? null,
            lastName: user.lastName ?? null,
            gender: user.gender as Gender | null,
            language: user.language ?? null,
            birthDate: user.birthDate ? new Date(user.birthDate) : null,
            height: user.height ?? null,
            activityLevel: user.activityLevel ? (user.activityLevel.toUpperCase() as ActivityLevelOption) : null,
            dailyCalorieTarget: user.dailyCalorieTarget ?? null,
            proteinTarget: user.proteinTarget ?? null,
            fatTarget: user.fatTarget ?? null,
            carbTarget: user.carbTarget ?? null,
            stepGoal: user.stepGoal ?? null,
            waterGoal: user.waterGoal ?? null,
            profileImage: user.profileImage ? { url: user.profileImage, assetId: user.profileImageAssetId ?? null } : null,
        };
    }
}

export interface UserFormValues {
    username: string | null;
    firstName: string | null;
    lastName: string | null;
    email: string | null;
    birthDate: Date | null;
    gender: Gender | null;
    language: string | null;
    height: number | null;
    activityLevel: ActivityLevelOption | null;
    dailyCalorieTarget: number | null;
    proteinTarget: number | null;
    fatTarget: number | null;
    carbTarget: number | null;
    stepGoal: number | null;
    waterGoal: number | null;
    profileImage: ImageSelection | null;
}

export type UserFormData = FormGroupControls<UserFormValues>;
