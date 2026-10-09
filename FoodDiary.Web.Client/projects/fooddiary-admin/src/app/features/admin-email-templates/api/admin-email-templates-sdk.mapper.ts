import type { AdminEmailTemplateHttpResponse } from '../../../shared/api/sdk/generated/model/admin-email-template-http-response';
import { requireSdkFields } from '../../../shared/api/sdk/sdk-response';
import { adminId, adminUtcInstant, optionalAdminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
import type { AdminEmailTemplate } from '../models/admin-email-template.data';

export function adminEmailTemplateFromSdk(response: AdminEmailTemplateHttpResponse): AdminEmailTemplate {
    const value = requireSdkFields(response, ['id', 'key', 'locale', 'subject', 'htmlBody', 'textBody', 'isActive', 'createdOnUtc']);
    return {
        ...value,
        id: adminId<'email-template'>(value.id),
        createdOnUtc: adminUtcInstant(value.createdOnUtc),
        updatedOnUtc: optionalAdminUtcInstant(value.updatedOnUtc),
    };
}
