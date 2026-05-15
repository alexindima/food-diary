import type { AbstractControl } from '@angular/forms';
import type { TranslateService } from '@ngx-translate/core';
import type { FdValidationErrorConfig, FdValidationErrors } from 'fd-ui-kit/form-error/fd-ui-form-error.component';

export function resolveTranslatedControlError(
    control: AbstractControl | null,
    validationErrors: FdValidationErrors | null | undefined,
    translateService: TranslateService,
): string | null {
    if (control?.invalid !== true) {
        return null;
    }

    const shouldShow = control.touched || control.dirty;
    if (!shouldShow) {
        return null;
    }

    const errors = control.errors;
    if (errors === null) {
        return null;
    }

    for (const key of Object.keys(errors)) {
        const resolver = validationErrors?.[key];
        if (resolver === undefined) {
            continue;
        }

        const controlError: unknown = errors[key];
        const controlParams = getValidationParams(controlError);
        const result = resolver(controlError);

        return translateValidationResult(result, controlParams, translateService);
    }

    return translateService.instant('FORM_ERRORS.UNKNOWN');
}

function translateValidationResult(
    result: FdValidationErrorConfig | string,
    controlParams: Record<string, unknown>,
    translateService: TranslateService,
): string {
    if (typeof result === 'string') {
        return translateService.instant(result, controlParams);
    }

    return translateService.instant(result.key, {
        ...controlParams,
        ...(result.params ?? {}),
    });
}

function getValidationParams(error: unknown): Record<string, unknown> {
    return isRecord(error) ? error : {};
}

function isRecord(value: unknown): value is Record<string, unknown> {
    return typeof value === 'object' && value !== null && !Array.isArray(value);
}
