import type { BillingOverview } from '../../../shared/models/billing.models';
import type { EntityId } from '../../../shared/models/semantics/entity-id';

export const CANCELLATION_REASONS = [
    'too_expensive',
    'ai_not_useful',
    'missing_features',
    'technical_issues',
    'not_using',
    'other',
] as const;
export type CancellationId = EntityId<'billing-cancellation'>;
export type CancellationFeedback = { reasonCode: string | null; comment: string | null };
export type CancellationConfirmation = { cancellationId: CancellationId; overview: BillingOverview };
