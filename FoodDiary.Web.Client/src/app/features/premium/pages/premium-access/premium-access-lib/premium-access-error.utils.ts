import { HttpErrorResponse } from '@angular/common/http';

import { getStringProperty } from '../../../../../shared/lib/unknown-value.utils';

export function resolvePremiumErrorMessage(error: unknown, fallbackMessage: string): string {
    if (error instanceof HttpErrorResponse) {
        const payload: unknown = error.error;
        const payloadMessage = getStringProperty(payload, 'message');
        if (payloadMessage !== undefined) {
            const message = payloadMessage.trim();
            if (message.length > 0) {
                return message;
            }
        }

        if (typeof payload === 'string') {
            const message = payload.trim();
            if (message.length > 0) {
                return message;
            }
        }
    }

    if (error instanceof Error) {
        const message = error.message.trim();
        if (message.length > 0) {
            return message;
        }
    }

    return fallbackMessage;
}
