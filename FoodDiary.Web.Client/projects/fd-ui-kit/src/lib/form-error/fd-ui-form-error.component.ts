import { ChangeDetectionStrategy, Component, computed, effect, inject, InjectionToken, input, signal } from '@angular/core';
import type { AbstractControl } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { merge } from 'rxjs';

export type FdValidationErrorConfig = {
    key: string;
    params?: Record<string, unknown>;
};

export type FdValidationErrors = Record<string, (error?: unknown) => FdValidationErrorConfig | string>;

export const FD_VALIDATION_ERRORS = new InjectionToken<FdValidationErrors>('FD_VALIDATION_ERRORS', {
    providedIn: 'root',
    factory: (): FdValidationErrors => ({
        required: (): string => 'FORM_ERRORS.REQUIRED',
        requiredTrue: (): string => 'FORM_ERRORS.REQUIRED',
        email: (): string => 'FORM_ERRORS.EMAIL',
        minlength: (error?: unknown): FdValidationErrorConfig => ({
            key: 'FORM_ERRORS.PASSWORD.MIN_LENGTH',
            params: { requiredLength: getNumberProperty(error, 'requiredLength') },
        }),
        userExists: (): string => 'FORM_ERRORS.USER_EXISTS',
        matchField: (): string => 'FORM_ERRORS.PASSWORD.MATCH',
    }),
});

export const getNumberProperty = (value: unknown, property: string): number | undefined => {
    if (typeof value !== 'object' || value === null) {
        return undefined;
    }

    const propertyValue: unknown = Object.getOwnPropertyDescriptor(value, property)?.value;
    return typeof propertyValue === 'number' ? propertyValue : undefined;
};

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
        if (control === null || control === undefined) {
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
        if (error !== null && error !== undefined && error.length > 0) {
            return this.translateMessage(error, this.context());
        }

        const control = this.control();
        if (control === null || control === undefined) {
            return null;
        }

        return this.resolveControlMessage(control);
    });

    private resolveControlMessage(control: AbstractControl): string | null {
        const shouldShow = control.touched || (this.showOnDirty() && control.dirty);
        if (!control.invalid || !shouldShow) {
            return null;
        }

        const errors = control.errors;
        if (errors === null) {
            return null;
        }

        const controlErrors = errors;

        for (const key of Object.keys(controlErrors)) {
            const resolver = this.validationErrors?.[key];
            if (resolver === undefined) {
                continue;
            }

            const controlError: unknown = controlErrors[key];
            const controlParams = this.getValidationParams(controlError);
            const result = resolver(controlError);

            return this.translateValidationResult(result, controlParams);
        }

        return this.translateMessage('FORM_ERRORS.UNKNOWN');
    }

    private translateValidationResult(result: FdValidationErrorConfig | string, controlParams: Record<string, unknown>): string {
        const context = this.context() ?? {};
        if (typeof result === 'string') {
            return this.translateMessage(result, { ...controlParams, ...context });
        }

        return this.translateMessage(result.key, {
            ...controlParams,
            ...(result.params ?? {}),
            ...context,
        });
    }

    private translateMessage(key: string, params?: Record<string, unknown>): string {
        const translated: unknown = this.translate.instant(key, params);
        return typeof translated === 'string' ? translated : key;
    }

    private getValidationParams(error: unknown): Record<string, unknown> {
        return this.isRecord(error) ? error : {};
    }

    private isRecord(value: unknown): value is Record<string, unknown> {
        return typeof value === 'object' && value !== null && !Array.isArray(value);
    }
}
