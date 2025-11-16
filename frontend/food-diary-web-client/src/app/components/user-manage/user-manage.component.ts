import {
    ChangeDetectionStrategy,
    Component,
    DestroyRef,
    FactoryProvider,
    inject,
    OnInit,
    signal,
    TemplateRef,
    ViewChild,
} from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import {
    TuiAlertService,
    TuiDialogContext,
    TuiDialogService,
    TuiError,
} from '@taiga-ui/core';
import { FormGroupControls } from '../../types/common.data';
import { UserService } from '../../services/user.service';
import { ActivityLevelOption, ChangePasswordRequest, Gender, UpdateUserDto } from '../../types/user.data';
import { NavigationService } from '../../services/navigation.service';
import { TuiDay } from '@taiga-ui/cdk';
import { AsyncPipe } from '@angular/common';
import { TUI_VALIDATION_ERRORS, TuiFieldErrorPipe } from '@taiga-ui/kit';
import { matchFieldValidator } from '../../validators/match-field.validator';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ValidationErrors } from '../../types/validation-error.data';
import { Observer, Subscription } from 'rxjs';
import { FdUiCardComponent } from '../../ui-kit/card/fd-ui-card.component';
import { FdUiInputComponent } from '../../ui-kit/input/fd-ui-input.component';
import { FdUiSelectComponent, FdUiSelectOption } from '../../ui-kit/select/fd-ui-select.component';
import { FdUiDateInputComponent } from '../../ui-kit/date-input/fd-ui-date-input.component';
import { FdUiButtonComponent } from '../../ui-kit/button/fd-ui-button.component';

export const VALIDATION_ERRORS_PROVIDER: FactoryProvider = {
    provide: TUI_VALIDATION_ERRORS,
    useFactory: (translate: TranslateService): ValidationErrors => ({
        required: () => translate.instant('FORM_ERRORS.REQUIRED'),
        userExists: () => translate.instant('FORM_ERRORS.USER_EXISTS'),
        email: () => translate.instant('FORM_ERRORS.EMAIL'),
        matchField: () => translate.instant('FORM_ERRORS.PASSWORD.MATCH'),
        minlength: ({ requiredLength }) =>
            translate.instant('FORM_ERRORS.PASSWORD.MIN_LENGTH', {
                requiredLength,
            }),
    }),
    deps: [TranslateService],
};

@Component({
    selector: 'fd-user-manage',
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        TuiError,
        TuiFieldErrorPipe,
        AsyncPipe,
        FdUiCardComponent,
        FdUiInputComponent,
        FdUiSelectComponent,
        FdUiDateInputComponent,
        FdUiButtonComponent,
    ],
    templateUrl: './user-manage.component.html',
    styleUrl: './user-manage.component.less',
    providers: [VALIDATION_ERRORS_PROVIDER],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class UserManageComponent implements OnInit {
    private readonly userService = inject(UserService);
    private readonly translateService = inject(TranslateService);
    private readonly dialogService = inject(TuiDialogService);
    private readonly navigationService = inject(NavigationService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly alertService = inject(TuiAlertService);
    private changePasswordDialogSubscription: Subscription | null = null;

    @ViewChild('successDialog') private successDialog!: TemplateRef<TuiDialogContext<boolean>>;
    @ViewChild('changePasswordDialog') private changePasswordDialog!: TemplateRef<TuiDialogContext<void>>;
    @ViewChild('passwordSuccessDialog') private passwordSuccessDialog!: TemplateRef<TuiDialogContext<void>>;

    public genders = Object.values(Gender);
    public activityLevels: ActivityLevelOption[] = ['MINIMAL', 'LIGHT', 'MODERATE', 'HIGH', 'EXTREME'];
    public genderOptions: FdUiSelectOption<Gender | null>[] = [];
    public activityLevelOptions: FdUiSelectOption<ActivityLevelOption | null>[] = [];

    public userForm: FormGroup<UserFormData>;
    public globalError = signal<string | null>(null);
    public changePasswordForm: FormGroup<ChangePasswordFormData>;
    public passwordError = signal<string | null>(null);
    public isPasswordSubmitting = signal<boolean>(false);

    public constructor() {
        this.userForm = new FormGroup<UserFormData>({
            email: new FormControl<string | null>({ value: '', disabled: true }),
            username: new FormControl<string | null>(null),
            firstName: new FormControl<string | null>(null),
            lastName: new FormControl<string | null>(null),
            birthDate: new FormControl<TuiDay | null>(null),
            gender: new FormControl<Gender | null>(null),
            height: new FormControl<number | null>(null),
            activityLevel: new FormControl<ActivityLevelOption | null>(null),
            dailyCalorieTarget: new FormControl<number | null>(null),
            proteinTarget: new FormControl<number | null>(null),
            fatTarget: new FormControl<number | null>(null),
            carbTarget: new FormControl<number | null>(null),
            stepGoal: new FormControl<number | null>(null),
            waterGoal: new FormControl<number | null>(null),
            profileImage: new FormControl<string | null>(null),
        });

        this.changePasswordForm = new FormGroup<ChangePasswordFormData>({
            currentPassword: new FormControl<string | null>(null, [Validators.required]),
            newPassword: new FormControl<string | null>(null, [Validators.required, Validators.minLength(6)]),
            confirmPassword: new FormControl<string | null>(null, [
                Validators.required,
                matchFieldValidator('newPassword'),
            ]),
        });

        this.buildSelectOptions();
        this.translateService.onLangChange
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => this.buildSelectOptions());
    }

    public ngOnInit(): void {
        this.userForm.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.clearGlobalError());

        this.loadUserData();
    }

    private loadUserData(): void {
        this.userService.getInfo().subscribe({
            next: user => {
                if (user) {
                    const userData = {
                        ...user,
                        gender: user.gender as Gender | null,
                        birthDate: user.birthDate ? TuiDay.fromLocalNativeDate(new Date(user.birthDate)) : null,
                        activityLevel: user.activityLevel
                            ? (user.activityLevel.toUpperCase() as ActivityLevelOption)
                            : null,
                        dailyCalorieTarget: user.dailyCalorieTarget ?? null,
                        proteinTarget: user.proteinTarget ?? null,
                        fatTarget: user.fatTarget ?? null,
                        carbTarget: user.carbTarget ?? null,
                        stepGoal: user.stepGoal ?? null,
                        waterGoal: user.waterGoal ?? null,
                    };
                    this.userForm.patchValue(userData);
                } else {
                    this.setGlobalError('USER_MANAGE.LOAD_ERROR');
                }
            },
            error: () => {
                this.setGlobalError('USER_MANAGE.LOAD_ERROR');
            },
        });
    }

    public async onSubmit(): Promise<void> {
        this.userForm.markAllAsTouched();

        if (this.userForm.valid) {
            const formData = this.userForm.value;
            const updateData = new UpdateUserDto(formData);

            this.userService.update(updateData).subscribe({
                next: user => {
                    if (user) {
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

    public openChangePasswordDialog(): void {
        this.changePasswordForm.reset();
        this.passwordError.set(null);
        this.isPasswordSubmitting.set(false);

        this.changePasswordDialogSubscription?.unsubscribe();
        this.changePasswordDialogSubscription = this.dialogService
            .open(this.changePasswordDialog, {
                dismissible: true,
                appearance: 'without-border-radius',
            })
            .subscribe();
    }

    public onChangePasswordSubmit(observer: Observer<void>): void {
        this.changePasswordForm.markAllAsTouched();
        if (this.changePasswordForm.invalid || this.isPasswordSubmitting()) {
            return;
        }

        const formValue = this.changePasswordForm.value;
        const payload: ChangePasswordRequest = {
            currentPassword: formValue.currentPassword?.trim() ?? '',
            newPassword: formValue.newPassword?.trim() ?? '',
        };

        this.isPasswordSubmitting.set(true);
        this.userService.changePassword(payload).subscribe({
            next: success => {
                this.isPasswordSubmitting.set(false);
                if (success) {
                    this.closeChangePasswordDialog(observer);
                    this.dialogService
                        .open(this.passwordSuccessDialog, {
                            dismissible: true,
                            appearance: 'without-border-radius',
                        })
                        .subscribe();
                } else {
                    this.passwordError.set(this.translateService.instant('USER_MANAGE.CHANGE_PASSWORD_ERROR'));
                }
            },
            error: () => {
                this.isPasswordSubmitting.set(false);
                this.passwordError.set(this.translateService.instant('USER_MANAGE.CHANGE_PASSWORD_ERROR'));
            },
        });
    }

    public onChangePasswordCancel(observer: Observer<void>): void {
        this.closeChangePasswordDialog(observer);
    }

    private closeChangePasswordDialog(observer: Observer<void>): void {
        observer.complete();
        this.changePasswordDialogSubscription?.unsubscribe();
        this.changePasswordDialogSubscription = null;
    }

    public onDeleteAccount(): void {
        this.alertService
            .open(this.translateService.instant('USER_MANAGE.DELETE_ACCOUNT_INFO'))
            .subscribe();
    }

    protected async showSuccessDialog(): Promise<void> {
        this.dialogService
            .open(this.successDialog, {
                dismissible: true,
                appearance: 'without-border-radius',
            })
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
    }
}

export interface UserFormValues {
    username: string | null;
    firstName: string | null;
    lastName: string | null;
    email: string | null;
    birthDate: TuiDay | null;
    gender: Gender | null;
    height: number | null;
    activityLevel: ActivityLevelOption | null;
    dailyCalorieTarget: number | null;
    proteinTarget: number | null;
    fatTarget: number | null;
    carbTarget: number | null;
    stepGoal: number | null;
    waterGoal: number | null;
    profileImage: string | null;
}

export type UserFormData = FormGroupControls<UserFormValues>;

interface ChangePasswordFormValues {
    currentPassword: string | null;
    newPassword: string | null;
    confirmPassword: string | null;
}

type ChangePasswordFormData = FormGroupControls<ChangePasswordFormValues>;
