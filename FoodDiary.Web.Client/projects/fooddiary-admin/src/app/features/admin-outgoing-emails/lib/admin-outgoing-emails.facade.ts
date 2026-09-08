import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';

import { AdminOutgoingEmailsService } from '../api/admin-outgoing-emails.service';
import type { OutgoingEmailPage } from '../models/outgoing-email';

@Service()
export class AdminOutgoingEmailsFacade {
    private readonly api = inject(AdminOutgoingEmailsService);
    public getPage(page: number, purpose: string, status: string, filters: Record<string, string> = {}): Observable<OutgoingEmailPage> {
        return this.api.getPage(page, purpose, status, filters);
    }
}
