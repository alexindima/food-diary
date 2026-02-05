import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export type AdminDashboardUser = {
  id: string;
  email: string;
  username?: string | null;
  firstName?: string | null;
  lastName?: string | null;
  isActive: boolean;
  createdOnUtc: string;
  deletedAt?: string | null;
  roles: string[];
};

export type AdminDashboardSummary = {
  totalUsers: number;
  activeUsers: number;
  premiumUsers: number;
  deletedUsers: number;
  recentUsers: AdminDashboardUser[];
};

@Injectable({ providedIn: 'root' })
export class AdminDashboardService {
  private readonly baseUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/dashboard`;

  constructor(private readonly http: HttpClient) {}

  public getSummary(): Observable<AdminDashboardSummary> {
    return this.http.get<AdminDashboardSummary>(this.baseUrl);
  }
}
