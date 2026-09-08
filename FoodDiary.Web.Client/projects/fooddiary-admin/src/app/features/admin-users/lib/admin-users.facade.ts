import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';

import { AdminUsersService } from '../api/admin-users.service';
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

@Service()
export class AdminUsersFacade {
    private readonly usersService = inject(AdminUsersService);

    public createUser(payload: AdminUserCreate): Observable<AdminUserCreation> {
        return this.usersService.createUser(payload);
    }

    public getUsers(
        page: number,
        limit: number,
        search?: string | null,
        filters: Record<string, string> = {},
    ): Observable<PagedResponse<AdminUser>> {
        return this.usersService.getUsers(page, limit, search, filters);
    }

    public updateUser(userId: string, payload: AdminUserUpdate): Observable<AdminUser> {
        return this.usersService.updateUser(userId, payload);
    }

    public setPassword(userId: string, payload: AdminUserSetPassword): Observable<void> {
        return this.usersService.setPassword(userId, payload);
    }

    public getUser(userId: string): Observable<AdminUser> {
        return this.usersService.getUser(userId);
    }

    public getUserRoleAudit(userId: string): Observable<AdminUserRoleAuditEvent[]> {
        return this.usersService.getUserRoleAudit(userId);
    }

    public startImpersonation(userId: string, reason: string): Observable<AdminImpersonationStart> {
        return this.usersService.startImpersonation(userId, reason);
    }

    public getImpersonationSessions(
        page: number,
        limit: number,
        search?: string | null,
        filters: Record<string, string> = {},
    ): Observable<PagedResponse<AdminImpersonationSession>> {
        return this.usersService.getImpersonationSessions(page, limit, search, filters);
    }

    public getLoginEvents(
        page: number,
        limit: number,
        search?: string | null,
        filters: Record<string, string> = {},
    ): Observable<PagedResponse<AdminUserLoginEvent>> {
        return this.usersService.getLoginEvents(page, limit, search, filters);
    }

    public getLoginSummary(): Observable<AdminUserLoginDeviceSummary[]> {
        return this.usersService.getLoginSummary();
    }
}
