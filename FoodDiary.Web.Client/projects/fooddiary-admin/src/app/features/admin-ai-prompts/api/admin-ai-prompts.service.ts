import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import type { AdminAiPrompt } from '../models/admin-ai-prompt';

@Service()
export class AdminAiPromptsService {
    private readonly http = inject(HttpClient);
    private readonly url = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/ai-prompts`;
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
