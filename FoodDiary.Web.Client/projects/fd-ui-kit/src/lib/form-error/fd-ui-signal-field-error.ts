import { computed, inject, Injector, type Signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormField, type ValidationError } from '@angular/forms/signals';
import { TranslateService } from '@ngx-translate/core';
import { EMPTY } from 'rxjs';

import { FD_VALIDATION_ERRORS, resolveSignalValidationMessage } from './fd-ui-form-error';

/** Explicit domain errors take precedence; framework errors appear only after interaction. */
export function injectFdUiFieldError(
    explicit: Signal<string | null | undefined>,
    errors: Signal<readonly ValidationError.WithOptionalFieldTree[]>,
    touched: Signal<boolean>,
): Signal<string | null> {
    const injector = inject(Injector);
    const field = computed(() => injector.get(FormField, null, { self: true }));
    const isTouched = computed(() => field()?.state().touched() ?? touched());
    const translate = inject(TranslateService, { optional: true });
    const messages = inject(FD_VALIDATION_ERRORS);
    const language = toSignal(translate?.onLangChange ?? EMPTY, { initialValue: null });
    return computed(() => {
        language();
        const provided = explicit();
        if (provided !== undefined && provided !== null) {
            return provided;
        }
        if (translate === null || !isTouched()) {
            return null;
        }
        const firstError = errors().at(0);
        if (firstError === undefined) {
            return null;
        }
        const message = firstError.message;
        if (message !== undefined) {
            const translated: unknown = translate.instant(message);
            return typeof translated === 'string' ? translated : message;
        }
        return resolveSignalValidationMessage(firstError, messages, translate);
    });
}
