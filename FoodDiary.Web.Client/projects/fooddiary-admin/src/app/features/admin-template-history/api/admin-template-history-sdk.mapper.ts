import type { AdminTemplateRevisionHttpResponse } from '../../../shared/api/sdk/generated/model/admin-template-revision-http-response';
import { requireSdkFields } from '../../../shared/api/sdk/sdk-response';
import type { AdminTemplateRevision } from '../models/admin-template-revision';

export function adminTemplateRevisionFromSdk(response: AdminTemplateRevisionHttpResponse): AdminTemplateRevision {
    const value = requireSdkFields(response, ['id', 'textBody', 'isActive', 'savedOnUtc', 'archivedOnUtc']);
    return { ...value, subject: value.subject ?? null, htmlBody: value.htmlBody ?? null, version: value.version ?? null };
}
