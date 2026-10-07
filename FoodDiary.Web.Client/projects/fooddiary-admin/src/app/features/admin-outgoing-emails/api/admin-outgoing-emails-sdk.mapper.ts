import type { AdminOutgoingEmailHttpResponse } from '../../../shared/api/sdk/generated/model/admin-outgoing-email-http-response';
import type { AdminOutgoingEmailPageHttpResponse } from '../../../shared/api/sdk/generated/model/admin-outgoing-email-page-http-response';
import { requireSdkFields } from '../../../shared/api/sdk/sdk-response';
import type { OutgoingEmail, OutgoingEmailPage } from '../models/outgoing-email';

export function adminOutgoingEmailPageFromSdk(response: AdminOutgoingEmailPageHttpResponse): OutgoingEmailPage {
    const value = requireSdkFields(response, ['items', 'totalItems']);
    return { ...value, items: value.items.map(item => adminOutgoingEmailFromSdk(item)) };
}

export function adminOutgoingEmailFromSdk(response: AdminOutgoingEmailHttpResponse): OutgoingEmail {
    const value = requireSdkFields(response, [
        'id',
        'status',
        'purpose',
        'fromAddress',
        'to',
        'subject',
        'createdAtUtc',
        'attemptCount',
        'maxAttempts',
        'contentHidden',
    ]);
    return {
        ...value,
        sentAtUtc: value.sentAtUtc ?? null,
        correlationId: value.correlationId ?? null,
        textBody: value.textBody ?? null,
        replyTo: value.replyTo ?? null,
        inReplyTo: value.inReplyTo ?? null,
    };
}
