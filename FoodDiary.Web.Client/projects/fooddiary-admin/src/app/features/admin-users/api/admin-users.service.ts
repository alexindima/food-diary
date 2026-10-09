import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { AdminUsersSdk } from '../../../shared/api/sdk/generated/api/admin-users.service';
import { createSdkConnection, sdkRequestOptions } from '../../../shared/api/sdk/sdk-connection';
import { sdkPage } from '../../../shared/api/sdk/sdk-response';
import type { AdminId } from '../../../shared/models/semantics/admin-meaning';
import type {
    AdminImpersonationSession,
    AdminImpersonationStart,
    AdminUser,
    AdminUserCreate,
    AdminUserCreation,
    AdminUserLoginDeviceSummary,
    AdminUserLoginEvent,
    AdminUserRoleAuditEvent,
    AdminUserSetPassword,
    AdminUserUpdate,
    PagedResponse,
} from '../models/admin-user.models';
import {
    adminImpersonationSessionFromSdk,
    adminImpersonationStartFromSdk,
    adminUserCreationFromSdk,
    adminUserFromSdk,
    adminUserLoginDeviceSummaryFromSdk,
    adminUserLoginEventFromSdk,
    adminUserRoleAuditEventFromSdk,
} from './admin-users-sdk.mapper';

const DEFAULT_ROLE_AUDIT_LIMIT = 20;

@Service()
export class AdminUsersService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/users`;
    private readonly sdk = createSdkConnection(AdminUsersSdk, this.baseUrl, this.http);

    public createUser(payload: AdminUserCreate): Observable<AdminUserCreation> {
        return this.sdk.client
            .postAdminUsers({ version: this.sdk.version, adminUserCreateHttpRequest: payload })
            .pipe(map(adminUserCreationFromSdk));
    }

    public getUsers(
        page: number,
        limit: number,
        search?: string | null,
        filters: Record<string, string> = {},
    ): Observable<PagedResponse<AdminUser>> {
        let params = new HttpParams()
            .set('page', page)
            .set('limit', limit)
            .set('status', filters['status'] ?? 'active');
        for (const [key, value] of Object.entries(filters)) {
            if (value.length > 0) {
                params = params.set(key, value);
            }
        }

        if (search !== null && search !== undefined && search.length > 0) {
            params = params.set('search', search);
        }

        return this.sdk.client
            .getAdminUsers({ version: this.sdk.version }, 'body', false, sdkRequestOptions(undefined, params))
            .pipe(map(response => sdkPage(response, adminUserFromSdk)))
            .pipe(
                map(response => ({
                    items: response.data,
                    page: response.page,
                    limit: response.limit,
                    totalPages: response.totalPages,
                    totalItems: response.totalItems,
                })),
            );
    }

    public updateUser(userId: AdminId<'user'>, payload: AdminUserUpdate): Observable<AdminUser> {
        return this.sdk.client
            .patchAdminUsersById({ version: this.sdk.version, id: userId, adminUserUpdateHttpRequest: payload })
            .pipe(map(adminUserFromSdk));
    }

    public setPassword(userId: AdminId<'user'>, payload: AdminUserSetPassword): Observable<void> {
        return this.sdk.client.patchAdminUsersByIdPassword({
            version: this.sdk.version,
            id: userId,
            adminUserSetPasswordHttpRequest: payload,
        });
    }

    public getUser(userId: AdminId<'user'>): Observable<AdminUser> {
        return this.sdk.client.getAdminUsersById({ version: this.sdk.version, id: userId }).pipe(map(adminUserFromSdk));
    }

    public getUserRoleAudit(userId: AdminId<'user'>, limit = DEFAULT_ROLE_AUDIT_LIMIT): Observable<AdminUserRoleAuditEvent[]> {
        const params = new HttpParams().set('limit', limit);
        return this.sdk.client
            .getAdminUsersByIdRoleAudit({ version: this.sdk.version, id: userId }, 'body', false, sdkRequestOptions(undefined, params))
            .pipe(map(items => items.map(adminUserRoleAuditEventFromSdk)));
    }

    public startImpersonation(userId: AdminId<'user'>, reason: string): Observable<AdminImpersonationStart> {
        return this.sdk.client
            .postAdminUsersByIdImpersonation({ version: this.sdk.version, id: userId, adminImpersonationStartHttpRequest: { reason } })
            .pipe(map(adminImpersonationStartFromSdk));
    }

    public getImpersonationSessions(
        page: number,
        limit: number,
        search?: string | null,
        filters: Record<string, string> = {},
    ): Observable<PagedResponse<AdminImpersonationSession>> {
        let params = new HttpParams({ fromObject: filters }).set('page', page).set('limit', limit);

        if (search !== null && search !== undefined && search.length > 0) {
            params = params.set('search', search);
        }

        return this.sdk.client
            .getAdminUsersImpersonationSessions({ version: this.sdk.version }, 'body', false, sdkRequestOptions(undefined, params))
            .pipe(map(response => sdkPage(response, adminImpersonationSessionFromSdk)))
            .pipe(
                map(response => ({
                    items: response.data,
                    page: response.page,
                    limit: response.limit,
                    totalPages: response.totalPages,
                    totalItems: response.totalItems,
                })),
            );
    }

    public getLoginEvents(
        page: number,
        limit: number,
        search?: string | null,
        filters: Record<string, string> = {},
    ): Observable<PagedResponse<AdminUserLoginEvent>> {
        let params = new HttpParams({ fromObject: filters }).set('page', page).set('limit', limit);

        if (search !== null && search !== undefined && search.length > 0) {
            params = params.set('search', search);
        }

        return this.sdk.client
            .getAdminUsersLoginEvents({ version: this.sdk.version }, 'body', false, sdkRequestOptions(undefined, params))
            .pipe(map(response => sdkPage(response, adminUserLoginEventFromSdk)))
            .pipe(
                map(response => ({
                    items: response.data,
                    page: response.page,
                    limit: response.limit,
                    totalPages: response.totalPages,
                    totalItems: response.totalItems,
                })),
            );
    }

    public getLoginSummary(): Observable<AdminUserLoginDeviceSummary[]> {
        return this.sdk.client
            .getAdminUsersLoginSummary({ version: this.sdk.version })
            .pipe(map(items => items.map(adminUserLoginDeviceSummaryFromSdk)));
    }
}
