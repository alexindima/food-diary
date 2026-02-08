import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export type AdminEmailTemplate = {
  id: string;
  key: string;
  locale: string;
  subject: string;
  htmlBody: string;
  textBody: string;
  isActive: boolean;
  createdOnUtc: string;
  updatedOnUtc?: string | null;
};

export type AdminEmailTemplateUpsertRequest = {
  subject: string;
  htmlBody: string;
  textBody: string;
  isActive: boolean;
};

@Injectable({ providedIn: 'root' })
export class AdminEmailTemplatesService {
  private readonly baseUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/email-templates`;

  public constructor(private readonly http: HttpClient) {}

  public getAll(): Observable<AdminEmailTemplate[]> {
    return this.http.get<AdminEmailTemplate[]>(this.baseUrl);
  }

  public upsert(key: string, locale: string, request: AdminEmailTemplateUpsertRequest): Observable<AdminEmailTemplate> {
    return this.http.put<AdminEmailTemplate>(`${this.baseUrl}/${key}/${locale}`, request);
  }
}
