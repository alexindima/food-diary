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
    TuiButton,
    TuiDialogContext,
    TuiDialogService,
    TuiError,
    TuiLabel,
    TuiTextfieldComponent,
    TuiTextfieldDirective,
} from '@taiga-ui/core';
import { FormGroupControls } from '../../types/common.data';
import { ApiResponse } from '../../types/api-response.data';
import { UserService } from '../../services/user.service';
import { Gender, UpdateUserDto } from '../../types/user.data';
import { NavigationService } from '../../services/navigation.service';
import { TuiDay } from '@taiga-ui/cdk';
import { TuiInputDateModule, TuiSelectModule, TuiTextfieldControllerModule } from '@taiga-ui/legacy';
import { AsyncPipe } from '@angular/common';
import { TUI_VALIDATION_ERRORS, TuiFieldErrorPipe } from '@taiga-ui/kit';
import { matchFieldValidator } from '../../validators/match-field.validator';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

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
    selector: 'app-user-manage',
    standalone: true,
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
    ],
    templateUrl: './user-manage.component.html',
    styleUrl: './user-manage.component.less',
    providers: [VALIDATION_ERRORS_PROVIDER],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserManageComponent implements OnInit {
    private readonly userService = inject(UserService);
    private readonly translateService = inject(TranslateService);
    private readonly dialogService = inject(TuiDialogService);
    private readonly navigationService = inject(NavigationService);
    private readonly destroyRef = inject(DestroyRef);

    @ViewChild('successDialog') private successDialog!: TemplateRef<TuiDialogContext<boolean>>;

    public genders = Object.values(Gender);

    public userForm: FormGroup<UserFormData>;
    public globalError = signal<string | null>(null);

    public constructor() {
        this.userForm = new FormGroup<UserFormData>({
            email: new FormControl<string | null>({ value: '', disabled: true }),
            password: new FormControl<string | null>(null, Validators.minLength(6)),
            confirmPassword: new FormControl<string | null>(null, matchFieldValidator('password')),
            username: new FormControl<string | null>(null),
            firstName: new FormControl<string | null>(null),
            lastName: new FormControl<string | null>(null),
            birthDate: new FormControl<TuiDay | null>(null),
            gender: new FormControl<Gender | null>(null),
            weight: new FormControl<number | null>(null),
            height: new FormControl<number | null>(null),
            profileImage: new FormControl<string | null>(null),
        });
    }

    public ngOnInit(): void {
        this.userForm.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.clearGlobalError());
        this.userForm.controls.password.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.userForm.controls.confirmPassword.updateValueAndValidity();
        });

        this.loadUserData();
    }

    private loadUserData(): void {
        this.userService.getInfo().subscribe({
            next: response => {
                if (response.status === 'success' && response.data) {
                    const userData = {
                        ...response.data,
                        gender: response.data.gender as Gender | null,
                        birthDate: response.data.birthDate ? TuiDay.fromLocalNativeDate(new Date(response.data.birthDate)) : null,
                    };
                    this.userForm.patchValue(userData);
                }
            },
            error: () => {
                this.globalError = this.translateService.instant('USER_MANAGE.LOAD_ERROR');
            },
        });
    }

    public async onSubmit(): Promise<void> {
        this.userForm.markAllAsTouched();

        if (this.userForm.valid) {
            const formData = this.userForm.value;
            const updateData = new UpdateUserDto(formData);

            this.userService.update(updateData).subscribe({
                next: (response: ApiResponse<UpdateUserDto | null>) => {
                    if (response.status === 'success') {
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
    password: string | null;
    confirmPassword: string | null;
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

interface ValidationErrors {
    required: () => string;
    userExists: () => string;
    email: () => string;
    matchField: () => string;
    minlength: (_params: { requiredLength: string }) => string;
}
