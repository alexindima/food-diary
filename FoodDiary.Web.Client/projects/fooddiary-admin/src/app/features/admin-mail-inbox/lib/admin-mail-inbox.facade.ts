import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';

import { AdminMailInboxService } from '../api/admin-mail-inbox.service';
import type { AdminMailInboxMessageDetails, AdminMailInboxMessageSummary } from '../models/admin-mail-inbox.data';

@Service()
export class AdminMailInboxFacade {
    private readonly mailInboxService = inject(AdminMailInboxService);

    public getMessages(limit: number): Observable<AdminMailInboxMessageSummary[]> {
        return this.mailInboxService.getMessages(limit);
    }

    public getMessage(id: string): Observable<AdminMailInboxMessageDetails> {
        return this.mailInboxService.getMessage(id);
    }

    public markMessageRead(id: string): Observable<void> {
        return this.mailInboxService.markMessageRead(id);
    }
}
