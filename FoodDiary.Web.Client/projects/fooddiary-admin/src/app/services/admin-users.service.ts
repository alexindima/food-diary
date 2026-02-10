import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../environments/environment';

export type AdminUser = {
  id: string;
  email: string;
  username?: string | null;
  firstName?: string | null;
  lastName?: string | null;
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
  private readonly baseUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/users`;

  constructor(private readonly http: HttpClient) {}

  public getUsers(
    page: number,
    limit: number,
    search?: string | null,
    includeDeleted = false
  ): Observable<PagedResponse<AdminUser>> {
    let params = new HttpParams()
      .set('page', page)
      .set('limit', limit)
      .set('includeDeleted', includeDeleted);

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
      }))
    );
  }

  public updateUser(userId: string, payload: AdminUserUpdate): Observable<AdminUser> {
    return this.http.patch<AdminUser>(`${this.baseUrl}/${userId}`, payload);
  }
}
