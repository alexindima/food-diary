import { getRecordProperty, getStringProperty } from './unknown-value.utils';

export const AI_CONSENT_ERROR_KEY = 'AI_RECOGNITION.ERROR_CONSENT';

export function isAiConsentRequiredError(error: unknown): boolean {
    const body = getRecordProperty(error, 'error');
    return getStringProperty(body, 'code') === 'Ai.ConsentRequired' || getStringProperty(body, 'error') === 'Ai.ConsentRequired';
}
