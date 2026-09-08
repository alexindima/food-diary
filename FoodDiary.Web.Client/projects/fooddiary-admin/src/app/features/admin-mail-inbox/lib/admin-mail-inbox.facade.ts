import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';

import { AdminMailInboxService } from '../api/admin-mail-inbox.service';
import type {
    AdminMailInboxFilters,
    AdminMailInboxMessageDetails,
    AdminMailInboxMessagePage,
    AdminMailInboxMessageSummary,
} from '../models/admin-mail-inbox.data';

@Service()
export class AdminMailInboxFacade {
    private readonly mailInboxService = inject(AdminMailInboxService);

    public getMessagePage(page: number, limit: number, filters: AdminMailInboxFilters = {}): Observable<AdminMailInboxMessagePage> {
        return this.mailInboxService.getMessagePage(page, limit, filters);
    }

    public getMessages(limit: number, recipient = '', category = '', unread?: boolean): Observable<AdminMailInboxMessageSummary[]> {
        return this.mailInboxService.getMessages(limit, recipient, category, unread);
    }

    public getMessage(id: string): Observable<AdminMailInboxMessageDetails> {
        return this.mailInboxService.getMessage(id);
    }

    public markMessageRead(id: string): Observable<void> {
        return this.mailInboxService.markMessageRead(id);
    }
}
