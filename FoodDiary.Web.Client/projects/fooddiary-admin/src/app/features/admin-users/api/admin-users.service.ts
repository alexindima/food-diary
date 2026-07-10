import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import type {
    AdminImpersonationSession,
    AdminImpersonationStart,
    AdminUser,
    AdminUserLoginDeviceSummary,
    AdminUserLoginEvent,
    AdminUserRoleAuditEvent,
    AdminUserSetPassword,
    AdminUserStatusFilter,
    AdminUserUpdate,
    PagedResponse,
} from '../models/admin-user.models';

const DEFAULT_ROLE_AUDIT_LIMIT = 20;

type ApiPagedResponse<T> = {
    data: T[];
    page: number;
    limit: number;
    totalPages: number;
    totalItems: number;
};

@Service()
export class AdminUsersService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/users`;

    public getUsers(
        page: number,
        limit: number,
        search?: string | null,
        status: AdminUserStatusFilter = 'active',
    ): Observable<PagedResponse<AdminUser>> {
        let params = new HttpParams().set('page', page).set('limit', limit).set('status', status);

        if (search !== null && search !== undefined && search.length > 0) {
            params = params.set('search', search);
        }

        return this.http.get<ApiPagedResponse<AdminUser>>(this.baseUrl, { params }).pipe(
            map(response => ({
                items: response.data,
                page: response.page,
                limit: response.limit,
                totalPages: response.totalPages,
                totalItems: response.totalItems,
            })),
        );
    }

    public updateUser(userId: string, payload: AdminUserUpdate): Observable<AdminUser> {
        return this.http.patch<AdminUser>(`${this.baseUrl}/${userId}`, payload);
    }

    public setPassword(userId: string, payload: AdminUserSetPassword): Observable<void> {
        return this.http.patch<void>(`${this.baseUrl}/${userId}/password`, payload);
    }

    public getUser(userId: string): Observable<AdminUser> {
        return this.http.get<AdminUser>(`${this.baseUrl}/${userId}`);
    }

    public getUserRoleAudit(userId: string, limit = DEFAULT_ROLE_AUDIT_LIMIT): Observable<AdminUserRoleAuditEvent[]> {
        const params = new HttpParams().set('limit', limit);
        return this.http.get<AdminUserRoleAuditEvent[]>(`${this.baseUrl}/${userId}/role-audit`, { params });
    }

    public startImpersonation(userId: string, reason: string): Observable<AdminImpersonationStart> {
        return this.http.post<AdminImpersonationStart>(`${this.baseUrl}/${userId}/impersonation`, { reason });
    }

    public getImpersonationSessions(
        page: number,
        limit: number,
        search?: string | null,
    ): Observable<PagedResponse<AdminImpersonationSession>> {
        let params = new HttpParams().set('page', page).set('limit', limit);

        if (search !== null && search !== undefined && search.length > 0) {
            params = params.set('search', search);
        }

        return this.http.get<ApiPagedResponse<AdminImpersonationSession>>(`${this.baseUrl}/impersonation-sessions`, { params }).pipe(
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
        userId?: string | null,
    ): Observable<PagedResponse<AdminUserLoginEvent>> {
        let params = new HttpParams().set('page', page).set('limit', limit);

        if (search !== null && search !== undefined && search.length > 0) {
            params = params.set('search', search);
        }

        if (userId !== null && userId !== undefined && userId.length > 0) {
            params = params.set('userId', userId);
        }

        return this.http.get<ApiPagedResponse<AdminUserLoginEvent>>(`${this.baseUrl}/login-events`, { params }).pipe(
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
        return this.http.get<AdminUserLoginDeviceSummary[]>(`${this.baseUrl}/login-summary`);
    }
}
