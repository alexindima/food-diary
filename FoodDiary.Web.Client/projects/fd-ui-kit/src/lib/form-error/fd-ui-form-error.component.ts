import { ChangeDetectionStrategy, Component, computed, effect, inject, InjectionToken, input, signal } from '@angular/core';
import type { AbstractControl } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { merge } from 'rxjs';

export interface FdValidationErrorConfig {
    key: string;
    params?: Record<string, unknown>;
}

export type FdValidationErrors = Record<string, (error?: unknown) => FdValidationErrorConfig | string>;

export const FD_VALIDATION_ERRORS = new InjectionToken<FdValidationErrors>('FD_VALIDATION_ERRORS', {
    providedIn: 'root',
    factory: (): FdValidationErrors => ({
        required: (): string => 'FORM_ERRORS.REQUIRED',
        requiredTrue: (): string => 'FORM_ERRORS.REQUIRED',
        email: (): string => 'FORM_ERRORS.EMAIL',
        minlength: (error?: unknown): FdValidationErrorConfig => ({
            key: 'FORM_ERRORS.PASSWORD.MIN_LENGTH',
            params: { requiredLength: (error as { requiredLength?: number } | undefined)?.requiredLength },
        }),
        userExists: (): string => 'FORM_ERRORS.USER_EXISTS',
        matchField: (): string => 'FORM_ERRORS.PASSWORD.MATCH',
    }),
});

@Component({
    selector: 'fd-ui-form-error',
    standalone: true,
    imports: [TranslateModule],
    templateUrl: './fd-ui-form-error.component.html',
    styleUrls: ['./fd-ui-form-error.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiFormErrorComponent {
    private readonly translate = inject(TranslateService);
    private readonly validationErrors = inject<FdValidationErrors>(FD_VALIDATION_ERRORS, { optional: true });
    private readonly controlVersion = signal(0);

    public readonly control = input<AbstractControl | null>();
    public readonly error = input<string | null>();
    public readonly context = input<Record<string, unknown>>();
    public readonly showOnDirty = input(false);
    private readonly controlSubscription = effect((onCleanup): void => {
        const control = this.control();
        if (!control) {
            return;
        }

        const subscription = merge(control.statusChanges, control.valueChanges, control.events).subscribe(() => {
            this.controlVersion.update(version => version + 1);
        });

        onCleanup((): void => {
            subscription.unsubscribe();
        });
    });

    public readonly message = computed((): string | null => {
        this.controlVersion();

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

            const controlError: unknown = controlErrors[key];
            const controlParams = this.getValidationParams(controlError);
            const result = resolver(controlError);

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
    });

    private getValidationParams(error: unknown): Record<string, unknown> {
        return this.isRecord(error) ? error : {};
    }

    private isRecord(value: unknown): value is Record<string, unknown> {
        return typeof value === 'object' && value !== null && !Array.isArray(value);
    }
}
