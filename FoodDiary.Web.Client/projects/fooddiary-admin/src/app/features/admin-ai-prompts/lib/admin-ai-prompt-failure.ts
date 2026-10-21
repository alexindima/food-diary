import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';

import { adminApiErrorCode } from '../../../shared/api/admin-api-error';

export function adminAiPromptFailureKey(error: unknown, run: boolean): string {
    if (
        error instanceof HttpErrorResponse &&
        error.status === Number(HttpStatusCode.Forbidden) &&
        adminApiErrorCode(error) === 'Ai.ConsentRequired'
    ) {
        return 'ADMIN_PROMPTS.CONSENT_REQUIRED';
    }
    if (error instanceof HttpErrorResponse && error.status === Number(HttpStatusCode.TooManyRequests)) {
        return 'ADMIN_PROMPTS.TEST_LIMIT';
    }
    return run ? 'ADMIN_PROMPTS.TEST_ERROR' : 'ADMIN_PROMPTS.PREVIEW_ERROR';
}
