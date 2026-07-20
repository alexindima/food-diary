import { ChangeDetectionStrategy, Component, computed, effect, inject, InjectionToken, input, signal } from '@angular/core';
import type { ValidationError } from '@angular/forms/signals';
import { TranslateService } from '@ngx-translate/core';
import { merge, type Observable } from 'rxjs';

export type FdValidationErrorConfig = {
    key: string;
    params?: Record<string, unknown>;
};

export type FdValidationErrors = Record<string, (error?: unknown) => FdValidationErrorConfig | string>;

export type FdUiFormErrorControlState = {
    dirty: boolean;
    errors: Record<string, unknown> | null;
    events: Observable<unknown>;
    invalid: boolean;
    statusChanges: Observable<unknown>;
    touched: boolean;
    valueChanges: Observable<unknown>;
};

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
        maxLength: (error?: unknown): FdValidationErrorConfig => ({
            key: 'FORM_ERRORS.MAX_LENGTH',
            params: { requiredLength: getNumberProperty(error, 'requiredLength') },
        }),
        nonEmptyArray: (): string => 'FORM_ERRORS.NON_EMPTY_ARRAY',
        min: (error?: unknown): FdValidationErrorConfig => ({
            key: 'FORM_ERRORS.INVALID_MIN_AMOUNT_MUST_BE_MORE_ZERO',
            params: { min: getNumberProperty(error, 'min') },
        }),
        max: (error?: unknown): FdValidationErrorConfig => ({
            key: 'FORM_ERRORS.INVALID_MAX_AMOUNT',
            params: { max: getNumberProperty(error, 'max') },
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

export type FdSignalFormErrorOptions = {
    showOnDirty?: boolean;
};

export type FdSignalFormFieldState = {
    dirty: () => boolean;
    errors: () => ValidationError[];
    invalid: () => boolean;
    touched: () => boolean;
};

export type FdSignalFormField = () => FdSignalFormFieldState;

export function resolveSignalFormFieldError(
    field: FdSignalFormField,
    validationErrors: Partial<FdValidationErrors> | null | undefined,
    translateService: TranslateService,
    options: FdSignalFormErrorOptions = {},
): string | null {
    const state = field();
    if (!state.invalid() || (!state.touched() && !((options.showOnDirty ?? true) && state.dirty()))) {
        return null;
    }

    const error = state.errors()[0];
    const key = mapSignalValidationErrorKey(error);
    const resolver = validationErrors?.[key];
    if (resolver === undefined) {
        return translateService.instant('FORM_ERRORS.UNKNOWN');
    }

    const params = getSignalValidationParams(error);
    const result = resolver(params);
    return translateValidationResult(result, params, translateService);
}

export function mapSignalValidationErrorKey(error: ValidationError): string {
    return error.kind === 'minLength' ? 'minlength' : error.kind;
}

export function getSignalValidationParams(error: ValidationError): Record<string, unknown> {
    if (error.kind === 'minLength') {
        return { requiredLength: getNumberProperty(error, 'minLength') };
    }

    if (error.kind === 'maxLength') {
        return { requiredLength: getNumberProperty(error, 'maxLength') };
    }

    return isRecord(error) ? error : {};
}

function translateValidationResult(
    result: FdValidationErrorConfig | string,
    controlParams: Record<string, unknown>,
    translateService: TranslateService,
    context: Record<string, unknown> = {},
): string {
    if (typeof result === 'string') {
        return translateMessage(result, translateService, { ...controlParams, ...context });
    }

    return translateMessage(result.key, translateService, {
        ...controlParams,
        ...result.params,
        ...context,
    });
}

function translateMessage(key: string, translateService: TranslateService, params?: Record<string, unknown>): string {
    const translated: unknown = translateService.instant(key, params);
    return typeof translated === 'string' ? translated : key;
}

function isRecord(value: unknown): value is Record<string, unknown> {
    return typeof value === 'object' && value !== null && !Array.isArray(value);
}

@Component({
    selector: 'fd-ui-form-error',
    templateUrl: './fd-ui-form-error.html',
    styleUrls: ['./fd-ui-form-error.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiFormErrorComponent {
    private readonly translate = inject(TranslateService);
    private readonly validationErrors = inject<FdValidationErrors>(FD_VALIDATION_ERRORS, { optional: true });
    private readonly controlVersion = signal(0);

    public readonly control = input<FdUiFormErrorControlState | null>();
    public readonly error = input<string | null>();
    public readonly context = input<Record<string, unknown>>();
    public readonly showOnDirty = input(false);

    public constructor() {
        effect((onCleanup): void => {
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
    }

    protected readonly message = computed((): string | null => {
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

    private resolveControlMessage(control: FdUiFormErrorControlState): string | null {
        const shouldShow = control.touched || (this.showOnDirty() && control.dirty);
        if (!shouldShow || !control.invalid) {
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
        return translateValidationResult(result, controlParams, this.translate, this.context() ?? {});
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
