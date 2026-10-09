/* eslint-disable @typescript-eslint/no-unsafe-type-assertion -- opaque identity roles retain their existing storage and wire strings */
import type { SemanticString, UnbrandedString } from '../models/semantics/string-meaning';

export type AnonymousVisitorId = SemanticString<'anonymous-visitor-id'>;
export type MarketingSessionId = SemanticString<'marketing-session-id'>;

export type MarketingAttributionIdentity = {
    anonymousId: AnonymousVisitorId;
    sessionId: MarketingSessionId;
};

export function anonymousVisitorId(value: UnbrandedString | AnonymousVisitorId): AnonymousVisitorId {
    return value as AnonymousVisitorId;
}

export function marketingSessionId(value: UnbrandedString | MarketingSessionId): MarketingSessionId {
    return value as MarketingSessionId;
}
