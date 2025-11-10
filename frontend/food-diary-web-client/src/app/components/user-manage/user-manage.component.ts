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
    TuiButton,
    TuiDialogContext,
    TuiDialogService,
    TuiError,
    TuiLabel,
    TuiTextfieldComponent,
    TuiTextfieldDirective,
} from '@taiga-ui/core';
import { FormGroupControls } from '../../types/common.data';
import { UserService } from '../../services/user.service';
import { ChangePasswordRequest, Gender, UpdateUserDto } from '../../types/user.data';
import { CustomGroupComponent } from '../shared/custom-group/custom-group.component';
import { NavigationService } from '../../services/navigation.service';
import { TuiDay } from '@taiga-ui/cdk';
import { TuiInputDateModule, TuiSelectModule, TuiTextfieldControllerModule } from '@taiga-ui/legacy';
import { AsyncPipe } from '@angular/common';
import { TUI_VALIDATION_ERRORS, TuiFieldErrorPipe } from '@taiga-ui/kit';
import { matchFieldValidator } from '../../validators/match-field.validator';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ValidationErrors } from '../../types/validation-error.data';
import { Observer, Subscription } from 'rxjs';

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
        TuiTextfieldComponent,
        ReactiveFormsModule,
        TranslatePipe,
        TuiLabel,
        TuiTextfieldDirective,
        TuiError,
        TuiButton,
        TuiInputDateModule,
        TuiTextfieldControllerModule,
        TuiSelectModule,
        TuiFieldErrorPipe,
        AsyncPipe,
        CustomGroupComponent,
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
            weight: new FormControl<number | null>(null),
            height: new FormControl<number | null>(null),
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

    public stringifyGender = (gender: Gender): string => {
        return this.translateService.instant(`USER_MANAGE.GENDER_OPTIONS.${gender}`);
    };

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
}

export interface UserFormValues {
    username: string | null;
    firstName: string | null;
    lastName: string | null;
    email: string | null;
    birthDate: TuiDay | null;
    gender: Gender | null;
    weight: number | null;
    height: number | null;
    profileImage: string | null;
}

export type UserFormData = FormGroupControls<UserFormValues>;

interface ChangePasswordFormValues {
    currentPassword: string | null;
    newPassword: string | null;
    confirmPassword: string | null;
}

type ChangePasswordFormData = FormGroupControls<ChangePasswordFormValues>;
