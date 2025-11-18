import { CommonModule } from '@angular/common';
import {
    ChangeDetectionStrategy,
    Component,
    Inject,
    InjectionToken,
    Input,
    Optional,
} from '@angular/core';
import { AbstractControl } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

export interface FdValidationErrorConfig {
    key: string;
    params?: Record<string, unknown>;
}

export type FdValidationErrors = Record<string, (error?: unknown) => FdValidationErrorConfig | string>;

export const FD_VALIDATION_ERRORS = new InjectionToken<FdValidationErrors>('FD_VALIDATION_ERRORS', {
    providedIn: 'root',
    factory: () => ({
        required: () => 'FORM_ERRORS.REQUIRED',
        requiredTrue: () => 'FORM_ERRORS.REQUIRED',
        email: () => 'FORM_ERRORS.EMAIL',
        minlength: (error?: unknown) => ({
            key: 'FORM_ERRORS.PASSWORD.MIN_LENGTH',
            params: { requiredLength: (error as { requiredLength?: number } | undefined)?.requiredLength },
        }),
        userExists: () => 'FORM_ERRORS.USER_EXISTS',
        matchField: () => 'FORM_ERRORS.PASSWORD.MATCH',
    }),
});

@Component({
    selector: 'fd-ui-form-error',
    standalone: true,
    imports: [CommonModule, TranslateModule],
    template: `
        @if (message) {
            <p class="fd-ui-form-error__text">{{ message }}</p>
        }
    `,
    styleUrls: ['./fd-ui-form-error.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiFormErrorComponent {
    @Input() public control?: AbstractControl | null;
    @Input() public error?: string | null;
    @Input() public context?: Record<string, unknown>;

    public constructor(
        private readonly translate: TranslateService,
        @Optional() @Inject(FD_VALIDATION_ERRORS) private readonly validationErrors?: FdValidationErrors,
    ) {}

    public get message(): string | null {
        if (this.error) {
            return this.translate.instant(this.error, this.context);
        }

        if (!this.control || !this.control.invalid || !this.control.touched) {
            return null;
        }

        const errors = this.control.errors;
        if (!errors) {
            return null;
        }

        const controlErrors = errors;

        for (const key of Object.keys(controlErrors)) {
            const resolver = this.validationErrors?.[key];
            if (!resolver) {
                continue;
            }

            const controlParams = typeof controlErrors[key] === 'object' ? controlErrors[key] : {};
            const result = resolver(controlErrors[key]);

            if (typeof result === 'string') {
                return this.translate.instant(result, { ...controlParams, ...(this.context ?? {}) });
            }

            return this.translate.instant(result.key, {
                ...controlParams,
                ...(result.params ?? {}),
                ...(this.context ?? {}),
            });
        }

        return this.translate.instant('FORM_ERRORS.UNKNOWN');
    }
}
