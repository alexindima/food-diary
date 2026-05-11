import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';

export type AdminUser = {
    id: string;
    email: string;
    username?: string | null;
    firstName?: string | null;
    lastName?: string | null;
    language?: string | null;
    isActive: boolean;
    isEmailConfirmed: boolean;
    createdOnUtc: string;
    deletedAt?: string | null;
    lastLoginAtUtc?: string | null;
    roles: string[];
};

export type AdminUserUpdate = {
    isActive?: boolean | null;
    isEmailConfirmed?: boolean | null;
    roles: string[];
    language?: string | null;
};

export type AdminImpersonationStart = {
    accessToken: string;
    targetUserId: string;
    targetEmail: string;
    actorUserId: string;
    reason: string;
};

export type AdminImpersonationSession = {
    id: string;
    actorUserId: string;
    actorEmail: string;
    targetUserId: string;
    targetEmail: string;
    reason: string;
    actorIpAddress?: string | null;
    actorUserAgent?: string | null;
    startedAtUtc: string;
};

export type AdminUserLoginEvent = {
    id: string;
    userId: string;
    userEmail: string;
    authProvider: string;
    maskedIpAddress?: string | null;
    userAgent?: string | null;
    browserName?: string | null;
    browserVersion?: string | null;
    operatingSystem?: string | null;
    deviceType?: string | null;
    loggedInAtUtc: string;
};

export type AdminUserLoginDeviceSummary = {
    key: string;
    count: number;
    lastSeenAtUtc: string;
};

type ApiPagedResponse<T> = {
    data: T[];
    page: number;
    limit: number;
    totalPages: number;
    totalItems: number;
};

export type PagedResponse<T> = {
    items: T[];
    page: number;
    limit: number;
    totalPages: number;
    totalItems: number;
};

@Injectable({ providedIn: 'root' })
export class AdminUsersService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/users`;

    public getUsers(page: number, limit: number, search?: string | null, includeDeleted = false): Observable<PagedResponse<AdminUser>> {
        let params = new HttpParams().set('page', page).set('limit', limit).set('includeDeleted', includeDeleted);

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

    public getLoginEvents(page: number, limit: number, search?: string | null): Observable<PagedResponse<AdminUserLoginEvent>> {
        let params = new HttpParams().set('page', page).set('limit', limit);

        if (search !== null && search !== undefined && search.length > 0) {
            params = params.set('search', search);
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
