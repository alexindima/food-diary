import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, FactoryProvider, inject, OnInit } from '@angular/core';
import { NgIf } from '@angular/common';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiFormErrorComponent, FD_VALIDATION_ERRORS, FdValidationErrors } from 'fd-ui-kit/form-error/fd-ui-form-error.component';
import { FdUiDateInputComponent } from 'fd-ui-kit/date-input/fd-ui-date-input.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiSelectComponent, FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select.component';
import { PageBodyComponent } from '../../../components/shared/page-body/page-body.component';
import { ImageUploadFieldComponent } from '../../../components/shared/image-upload-field/image-upload-field.component';
import { PageHeaderComponent } from '../../../components/shared/page-header/page-header.component';
import { FdPageContainerDirective } from '../../../directives/layout/page-container.directive';
import { AuthService } from '../../../services/auth.service';
import { ImageUploadService } from '../../../shared/api/image-upload.service';
import { LocalizationService } from '../../../services/localization.service';
import { ImageSelection } from '../../../shared/models/image-upload.data';
import { FormGroupControls } from '../../../shared/lib/common.data';
import { ActivityLevelOption, Gender, UpdateUserDto, User } from '../../../shared/models/user.data';
import { ProfileManageFacade } from '../lib/profile-manage.facade';

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
        NgIf,
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
    providers: [VALIDATION_ERRORS_PROVIDER, ProfileManageFacade],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserManageComponent implements OnInit {
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly imageUploadService = inject(ImageUploadService);
    private readonly authService = inject(AuthService);
    private readonly localizationService = inject(LocalizationService);
    private readonly facade = inject(ProfileManageFacade);
    private lastUserData: Partial<UserFormValues> | null = null;

    public genders = Object.values(Gender);
    public activityLevels: ActivityLevelOption[] = ['MINIMAL', 'LIGHT', 'MODERATE', 'HIGH', 'EXTREME'];
    public languageCodes: string[] = ['en', 'ru'];
    public genderOptions: FdUiSelectOption<Gender | null>[] = [];
    public activityLevelOptions: FdUiSelectOption<ActivityLevelOption | null>[] = [];
    public languageOptions: FdUiSelectOption<string | null>[] = [];
    public userForm: FormGroup<UserFormData>;
    public readonly globalError = this.facade.globalError;
    public readonly isDeleting = this.facade.isDeleting;
    public readonly hasAiConsent = computed(() => !!this.facade.user()?.aiConsentAcceptedAt);

    public constructor() {
        this.userForm = new FormGroup<UserFormData>({
            email: new FormControl<string | null>({ value: '', disabled: true }),
            username: new FormControl<string | null>(null),
            firstName: new FormControl<string | null>(null),
            lastName: new FormControl<string | null>(null),
            birthDate: new FormControl<string | null>(null),
            gender: new FormControl<Gender | null>(null),
            language: new FormControl<string | null>(null),
            height: new FormControl<number | null>(null),
            activityLevel: new FormControl<ActivityLevelOption | null>(null),
            stepGoal: new FormControl<number | null>(null),
            profileImage: new FormControl<ImageSelection | null>(null),
        });

        this.buildSelectOptions();
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.buildSelectOptions());
        effect(() => {
            const user = this.facade.user();
            if (!user) {
                return;
            }

            const userData = this.mapUserToForm(user);
            this.lastUserData = userData;
            this.applyUserData(userData);
        });
    }

    public ngOnInit(): void {
        this.userForm.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.facade.clearGlobalError());
        this.userForm.controls.language.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(language => {
            if (!this.userForm.controls.language.dirty) {
                return;
            }

            this.applyLanguagePreference(language ?? null);
        });

        this.facade.initialize();
    }

    public async onSubmit(): Promise<void> {
        this.userForm.markAllAsTouched();

        if (this.userForm.valid) {
            const formData = this.userForm.value;
            const updateData = new UpdateUserDto({
                ...formData,
                profileImage: formData.profileImage as ImageSelection | null,
            });
            this.facade.submitUpdate(updateData);
        }
    }

    public resetForm(): void {
        this.facade.clearGlobalError();
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

    public isAdminUser(): boolean {
        return this.authService.isAdmin();
    }

    public openAdminPanel(): void {
        this.facade.openAdminPanel();
    }

    private applyUserData(userData: Partial<UserFormValues>): void {
        this.userForm.patchValue(userData);
        this.userForm.markAsPristine();
        this.userForm.markAsUntouched();
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
        void this.localizationService.applyLanguagePreference(language);
    }

    private normalizeLanguage(value: string | null | undefined): string | null {
        if (!value) {
            return null;
        }

        const normalized = value.trim().toLowerCase();
        if (!normalized) {
            return null;
        }

        const [code] = normalized.split(/[-_]/);
        return code || null;
    }

    private mapUserToForm(user: User): Partial<UserFormValues> {
        return {
            email: user.email ?? null,
            username: user.username ?? null,
            firstName: user.firstName ?? null,
            lastName: user.lastName ?? null,
            gender: user.gender as Gender | null,
            language: this.normalizeLanguage(user.language),
            birthDate: user.birthDate ? this.formatDateInput(new Date(user.birthDate)) : null,
            height: user.height ?? null,
            activityLevel: user.activityLevel ? (user.activityLevel.toUpperCase() as ActivityLevelOption) : null,
            stepGoal: user.stepGoal ?? null,
            profileImage: user.profileImage ? { url: user.profileImage, assetId: user.profileImageAssetId ?? null } : null,
        };
    }

    private formatDateInput(date: Date): string {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    }
}

export interface UserFormValues {
    username: string | null;
    firstName: string | null;
    lastName: string | null;
    email: string | null;
    birthDate: string | null;
    gender: Gender | null;
    language: string | null;
    height: number | null;
    activityLevel: ActivityLevelOption | null;
    stepGoal: number | null;
    profileImage: ImageSelection | null;
}

export type UserFormData = FormGroupControls<UserFormValues>;
