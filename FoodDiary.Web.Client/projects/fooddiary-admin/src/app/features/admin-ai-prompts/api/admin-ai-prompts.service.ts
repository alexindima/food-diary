import { DOCUMENT } from '@angular/common';
import { HttpBackend, HttpClient, HttpHeaders } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { type Observable, switchMap } from 'rxjs';

import { environment } from '../../../../environments/environment';
import type { AdminAiPrompt } from '../models/admin-ai-prompt';
import type { AdminAiPromptDraft, AdminAiPromptScenario } from '../models/admin-ai-prompt-scenario';

@Service()
export class AdminAiPromptsService {
    private readonly http = inject(HttpClient);
    private readonly uploadHttp = new HttpClient(inject(HttpBackend));
    private readonly document = inject(DOCUMENT);
    private readonly url = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/ai-prompts`;
    public getScenarios(): Observable<AdminAiPromptScenario[]> {
        return this.http.get<AdminAiPromptScenario[]>(`${this.url}/scenarios`);
    }
    public preview(draft: AdminAiPromptDraft): Observable<{ text: string }> {
        return this.http.post<{ text: string }>(`${this.url}/preview`, draft);
    }
    public test(draft: AdminAiPromptDraft): Observable<{ text: string }> {
        const key = this.document.defaultView?.crypto.randomUUID();
        if (key === undefined) {
            throw new Error('A secure browser context is required.');
        }
        return this.http.post<{ text: string }>(`${this.url}/test`, draft, {
            headers: new HttpHeaders({ 'Idempotency-Key': key }),
        });
    }
    public uploadImage(file: File): Observable<{ assetId: string }> {
        const imagesUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/images`;
        return this.http
            .post<{ uploadUrl: string; assetId: string }>(`${imagesUrl}/upload-url`, {
                fileName: file.name,
                contentType: file.type,
                fileSizeBytes: file.size,
            })
            .pipe(
                switchMap(upload =>
                    this.uploadHttp
                        .put(upload.uploadUrl, file, {
                            headers: { 'Content-Type': file.type },
                            responseType: 'text',
                        })
                        .pipe(switchMap(() => this.http.post<{ assetId: string }>(`${imagesUrl}/${upload.assetId}/confirm`, {}))),
                ),
            );
    }
    public getAll(): Observable<AdminAiPrompt[]> {
        return this.http.get<AdminAiPrompt[]>(this.url);
    }
    public save(key: string, locale: string, promptText: string, isActive: boolean): Observable<AdminAiPrompt> {
        return this.http.put<AdminAiPrompt>(`${this.url}/${encodeURIComponent(key)}/${encodeURIComponent(locale)}`, {
            promptText,
            isActive,
        });
    }
}
