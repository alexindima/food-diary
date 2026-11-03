import { getRecordProperty, getStringProperty } from './unknown-value.utils';

export const AI_TRIAL_QUOTA_EXCEEDED = 'Ai.TrialQuotaExceeded';

export function getAiTrialQuotaErrorKey(error: unknown): string | undefined {
    const body = getRecordProperty(error, 'error');
    const code = typeof error === 'string' ? error : (getStringProperty(body, 'code') ?? getStringProperty(body, 'error'));
    return code === AI_TRIAL_QUOTA_EXCEEDED ? 'AI_RECOGNITION.ERROR_TRIAL_QUOTA' : undefined;
}
