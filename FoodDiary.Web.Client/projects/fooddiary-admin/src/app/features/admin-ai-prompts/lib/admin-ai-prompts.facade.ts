import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';

import { AdminAiPromptsService } from '../api/admin-ai-prompts.service';
import type { AdminAiPrompt } from '../models/admin-ai-prompt';
import type { AdminAiPromptDraft, AdminAiPromptScenario } from '../models/admin-ai-prompt-scenario';

@Service()
export class AdminAiPromptsFacade {
    private readonly api = inject(AdminAiPromptsService);
    public getScenarios(): Observable<AdminAiPromptScenario[]> {
        return this.api.getScenarios();
    }
    public preview(draft: AdminAiPromptDraft): Observable<{ text: string }> {
        return this.api.preview(draft);
    }
    public test(draft: AdminAiPromptDraft): Observable<{ text: string }> {
        return this.api.test(draft);
    }
    public uploadImage(file: File): Observable<{ assetId: string }> {
        return this.api.uploadImage(file);
    }
    public getAll(): Observable<AdminAiPrompt[]> {
        return this.api.getAll();
    }
    public save(key: string, locale: string, promptText: string, isActive: boolean): Observable<AdminAiPrompt> {
        return this.api.save(key, locale, promptText, isActive);
    }
}
