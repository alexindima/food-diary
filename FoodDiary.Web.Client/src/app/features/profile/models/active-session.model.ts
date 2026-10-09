import type { UtcInstant } from '../../../shared/models/semantics/date-value';
import type { RefreshTokenSessionId } from '../../../shared/models/semantics/entity-id';

export type ActiveSession = {
    id: RefreshTokenSessionId;
    isCurrent: boolean;
    authProvider: string | null;
    browser: string | null;
    operatingSystem: string | null;
    deviceType: string | null;
    createdAtUtc: UtcInstant;
    lastActiveAtUtc: UtcInstant;
};

export type SessionRevocation = { kind: 'idle' } | { kind: 'single'; sessionId: RefreshTokenSessionId } | { kind: 'others' };
