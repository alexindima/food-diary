import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
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

        if (search) {
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

        if (search) {
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
}
