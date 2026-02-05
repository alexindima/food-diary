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

export type AdminAiUsageDaily = {
  date: string;
  totalTokens: number;
  inputTokens: number;
  outputTokens: number;
};

export type AdminAiUsageBreakdown = {
  key: string;
  totalTokens: number;
  inputTokens: number;
  outputTokens: number;
};

export type AdminAiUsageSummary = {
  totalTokens: number;
  inputTokens: number;
  outputTokens: number;
  byDay: AdminAiUsageDaily[];
  byOperation: AdminAiUsageBreakdown[];
  byModel: AdminAiUsageBreakdown[];
};

@Injectable({ providedIn: 'root' })
export class AdminDashboardService {
  private readonly baseUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/dashboard`;
  private readonly aiUsageUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/ai-usage/summary`;

  constructor(private readonly http: HttpClient) {}

  public getSummary(): Observable<AdminDashboardSummary> {
    return this.http.get<AdminDashboardSummary>(this.baseUrl);
  }

  public getAiUsageSummary(): Observable<AdminAiUsageSummary> {
    return this.http.get<AdminAiUsageSummary>(this.aiUsageUrl);
  }
}
