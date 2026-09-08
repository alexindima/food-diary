import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';

import { AdminAiPromptsService } from '../api/admin-ai-prompts.service';
import type { AdminAiPrompt } from '../models/admin-ai-prompt';

@Service()
export class AdminAiPromptsFacade {
    private readonly api = inject(AdminAiPromptsService);
    public getAll(): Observable<AdminAiPrompt[]> {
        return this.api.getAll();
    }
    public save(key: string, locale: string, promptText: string, isActive: boolean): Observable<AdminAiPrompt> {
        return this.api.save(key, locale, promptText, isActive);
    }
}
