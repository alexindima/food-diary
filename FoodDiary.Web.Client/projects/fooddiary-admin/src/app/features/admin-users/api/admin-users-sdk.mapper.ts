import type { AdminImpersonationSessionHttpResponse } from '../../../shared/api/sdk/generated/model/admin-impersonation-session-http-response';
import type { AdminImpersonationStartHttpResponse } from '../../../shared/api/sdk/generated/model/admin-impersonation-start-http-response';
import type { AdminUserCreationHttpResponse } from '../../../shared/api/sdk/generated/model/admin-user-creation-http-response';
import type { AdminUserHttpResponse } from '../../../shared/api/sdk/generated/model/admin-user-http-response';
import type { AdminUserLoginDeviceSummaryHttpResponse } from '../../../shared/api/sdk/generated/model/admin-user-login-device-summary-http-response';
import type { AdminUserLoginEventHttpResponse } from '../../../shared/api/sdk/generated/model/admin-user-login-event-http-response';
import type { AdminUserRoleAuditEventHttpResponse } from '../../../shared/api/sdk/generated/model/admin-user-role-audit-event-http-response';
import { requireSdkFields } from '../../../shared/api/sdk/sdk-response';
import {
    adminId,
    adminUtcInstant,
    optionalAdminCalendarDate,
    optionalAdminId,
    optionalAdminUtcInstant,
} from '../../../shared/models/semantics/admin-meaning';
import type {
    AdminImpersonationSession,
    AdminImpersonationStart,
    AdminUser,
    AdminUserCreation,
    AdminUserLoginDeviceSummary,
    AdminUserLoginEvent,
    AdminUserRoleAuditEvent,
} from '../models/admin-user.models';

export function adminUserFromSdk(response: AdminUserHttpResponse): AdminUser {
    const value = requireSdkFields(response, ['id', 'isActive', 'isEmailConfirmed', 'createdOnUtc', 'roles']);
    return {
        ...value,
        email: value.email ?? null,
        id: adminId<'user'>(value.id),
        birthDate: optionalAdminCalendarDate(value.birthDate),
        latestWeightDate: optionalAdminCalendarDate(value.latestWeightDate),
        profileImageAssetId: optionalAdminId<'image-asset'>(value.profileImageAssetId),
        createdOnUtc: adminUtcInstant(value.createdOnUtc),
        deletedAt: optionalAdminUtcInstant(value.deletedAt),
        lastLoginAtUtc: optionalAdminUtcInstant(value.lastLoginAtUtc),
    };
}

export function adminUserCreationFromSdk(response: AdminUserCreationHttpResponse): AdminUserCreation {
    const value = requireSdkFields(response, ['user', 'temporaryPassword', 'credentialsEmailQueued']);
    return { ...value, user: adminUserFromSdk(value.user) };
}

export function adminUserRoleAuditEventFromSdk(response: AdminUserRoleAuditEventHttpResponse): AdminUserRoleAuditEvent {
    const value = requireSdkFields(response, ['id', 'userId', 'roleName', 'action', 'source', 'occurredAtUtc']);
    return {
        ...value,
        id: adminId<'role-audit-event'>(value.id),
        userId: adminId<'user'>(value.userId),
        actorUserId: optionalAdminId<'user'>(value.actorUserId),
        occurredAtUtc: adminUtcInstant(value.occurredAtUtc),
    };
}

export function adminImpersonationStartFromSdk(response: AdminImpersonationStartHttpResponse): AdminImpersonationStart {
    const value = requireSdkFields(response, ['code', 'targetUserId', 'actorUserId', 'reason']);
    return {
        ...value,
        targetEmail: value.targetEmail ?? null,
        targetUserId: adminId<'user'>(value.targetUserId),
        actorUserId: adminId<'user'>(value.actorUserId),
    };
}

export function adminImpersonationSessionFromSdk(response: AdminImpersonationSessionHttpResponse): AdminImpersonationSession {
    const value = requireSdkFields(response, ['id', 'actorUserId', 'targetUserId', 'reason', 'startedAtUtc']);
    return {
        ...value,
        actorEmail: value.actorEmail ?? null,
        targetEmail: value.targetEmail ?? null,
        id: adminId<'impersonation-session'>(value.id),
        actorUserId: adminId<'user'>(value.actorUserId),
        targetUserId: adminId<'user'>(value.targetUserId),
        startedAtUtc: adminUtcInstant(value.startedAtUtc),
    };
}

export function adminUserLoginEventFromSdk(response: AdminUserLoginEventHttpResponse): AdminUserLoginEvent {
    const value = requireSdkFields(response, ['id', 'userId', 'authProvider', 'loggedInAtUtc']);
    return {
        ...value,
        userEmail: value.userEmail ?? null,
        id: adminId<'login-event'>(value.id),
        userId: adminId<'user'>(value.userId),
        loggedInAtUtc: adminUtcInstant(value.loggedInAtUtc),
    };
}

export function adminUserLoginDeviceSummaryFromSdk(response: AdminUserLoginDeviceSummaryHttpResponse): AdminUserLoginDeviceSummary {
    const value = requireSdkFields(response, ['key', 'count', 'lastSeenAtUtc']);
    return { ...value, lastSeenAtUtc: adminUtcInstant(value.lastSeenAtUtc) };
}
