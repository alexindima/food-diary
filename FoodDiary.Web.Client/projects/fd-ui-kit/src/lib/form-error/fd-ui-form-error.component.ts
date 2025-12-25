
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, InjectionToken, effect, inject, input } from '@angular/core';
import { AbstractControl } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { merge } from 'rxjs';

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
    imports: [TranslateModule],
    template: `
        @if (message) {
            <p class="fd-ui-form-error__text">{{ message }}</p>
        }
    `,
    styleUrls: ['./fd-ui-form-error.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiFormErrorComponent {
    private readonly translate = inject(TranslateService);
    private readonly cdr = inject(ChangeDetectorRef);
    private readonly validationErrors = inject<FdValidationErrors>(FD_VALIDATION_ERRORS, { optional: true });

    public readonly control = input<AbstractControl | null>();
    public readonly error = input<string | null>();
    public readonly context = input<Record<string, unknown>>();
    public readonly showOnDirty = input(false);
    private readonly controlSubscription = effect(onCleanup => {
        const control = this.control();
        if (!control) {
            return;
        }

        const subscription = merge(control.statusChanges, control.valueChanges)
            .subscribe(() => this.cdr.markForCheck());

        onCleanup(() => subscription.unsubscribe());
    });

    public get message(): string | null {
        const error = this.error();
        if (error) {
            return this.translate.instant(error, this.context());
        }

        const control = this.control();
        if (!control) {
            return null;
        }
        const shouldShow = control.touched || (this.showOnDirty() && control.dirty);
        if (!control.invalid || !shouldShow) {
            return null;
        }

        const errors = control.errors;
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
                return this.translate.instant(result, { ...controlParams, ...(this.context() ?? {}) });
            }

            return this.translate.instant(result.key, {
                ...controlParams,
                ...(result.params ?? {}),
                ...(this.context() ?? {}),
            });
        }

        return this.translate.instant('FORM_ERRORS.UNKNOWN');
    }
}
