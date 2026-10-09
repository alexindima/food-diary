import type { AdminMailInboxDmarcRecordHttpResponse } from '../../../shared/api/sdk/generated/model/admin-mail-inbox-dmarc-record-http-response';
import type { AdminMailInboxDmarcReportHttpResponse } from '../../../shared/api/sdk/generated/model/admin-mail-inbox-dmarc-report-http-response';
import type { AdminMailInboxMessageDetailsHttpResponse } from '../../../shared/api/sdk/generated/model/admin-mail-inbox-message-details-http-response';
import type { AdminMailInboxMessagePageHttpResponse } from '../../../shared/api/sdk/generated/model/admin-mail-inbox-message-page-http-response';
import type { AdminMailInboxMessageSummaryHttpResponse } from '../../../shared/api/sdk/generated/model/admin-mail-inbox-message-summary-http-response';
import { requireSdkFields, sdkMaybe } from '../../../shared/api/sdk/sdk-response';
import { adminId, adminUtcInstant, optionalAdminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
import type {
    AdminMailInboxDmarcRecord,
    AdminMailInboxDmarcReport,
    AdminMailInboxMessageDetails,
    AdminMailInboxMessagePage,
    AdminMailInboxMessageSummary,
} from '../models/admin-mail-inbox.data';

export function adminMailInboxMessagePageFromSdk(response: AdminMailInboxMessagePageHttpResponse): AdminMailInboxMessagePage {
    const value = requireSdkFields(response, ['items', 'totalItems']);
    return { ...value, items: value.items.map(item => adminMailInboxMessageSummaryFromSdk(item)) };
}

export function adminMailInboxMessageSummaryFromSdk(response: AdminMailInboxMessageSummaryHttpResponse): AdminMailInboxMessageSummary {
    const value = requireSdkFields(response, ['id', 'isTrustedRelay', 'toRecipients', 'category', 'status', 'receivedAtUtc']);
    return {
        ...value,
        id: adminId<'mail-inbox-message'>(value.id),
        readAtUtc: optionalAdminUtcInstant(value.readAtUtc),
        receivedAtUtc: adminUtcInstant(value.receivedAtUtc),
    };
}

export function adminMailInboxMessageDetailsFromSdk(response: AdminMailInboxMessageDetailsHttpResponse): AdminMailInboxMessageDetails {
    const value = requireSdkFields(response, ['id', 'isTrustedRelay', 'toRecipients', 'category', 'status', 'receivedAtUtc']);
    return {
        ...value,
        dmarcReport: sdkMaybe(value.dmarcReport, adminMailInboxDmarcReportFromSdk),
        id: adminId<'mail-inbox-message'>(value.id),
        readAtUtc: optionalAdminUtcInstant(value.readAtUtc),
        receivedAtUtc: adminUtcInstant(value.receivedAtUtc),
        contentPurgedAtUtc: optionalAdminUtcInstant(value.contentPurgedAtUtc),
    };
}

export function adminMailInboxDmarcReportFromSdk(response: AdminMailInboxDmarcReportHttpResponse): AdminMailInboxDmarcReport {
    const value = requireSdkFields(response, ['records']);
    return {
        ...value,
        records: value.records.map(item => adminMailInboxDmarcRecordFromSdk(item)),
        dateRangeStartUtc: optionalAdminUtcInstant(value.dateRangeStartUtc),
        dateRangeEndUtc: optionalAdminUtcInstant(value.dateRangeEndUtc),
    };
}

export function adminMailInboxDmarcRecordFromSdk(response: AdminMailInboxDmarcRecordHttpResponse): AdminMailInboxDmarcRecord {
    const value = requireSdkFields(response, ['count']);
    return { ...value };
}
