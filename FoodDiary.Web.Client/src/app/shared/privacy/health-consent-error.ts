import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';

import { getRecordProperty, getStringProperty } from '../lib/unknown-value.utils';

export function isHealthConsentRequiredError(error: unknown): boolean {
    return (
        error instanceof HttpErrorResponse &&
        getRecordProperty(error, 'status') === HttpStatusCode.Forbidden &&
        getStringProperty(error.error, 'error') === 'Privacy.HealthConsentRequired'
    );
}
