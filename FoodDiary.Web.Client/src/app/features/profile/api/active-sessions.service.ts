import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import type { ActiveSession } from '../models/active-session.model';

@Service()
export class ActiveSessionsService {
    private readonly http = inject(HttpClient);
    private readonly sessionsUrl = `${environment.apiUrls.auth}/sessions`;

    public getAll(): Observable<ActiveSession[]> {
        return this.http.get<ActiveSession[]>(this.sessionsUrl);
    }

    public revoke(sessionId: string): Observable<void> {
        return this.http.delete<void>(`${this.sessionsUrl}/${sessionId}`);
    }

    public revokeOthers(): Observable<void> {
        return this.http.delete<void>(this.sessionsUrl);
    }
}
