import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { loadPagedCollection } from '../../../shared/api/load-paged-collection';
import { AuthSessionsSdk } from '../../../shared/api/sdk/generated/api/auth-sessions.service';
import type { ActiveSessionHttpResponse } from '../../../shared/api/sdk/generated/model/active-session-http-response';
import { createSdkConnection } from '../../../shared/api/sdk/sdk-connection';
import { requireSdkFields, sdkNullableFields } from '../../../shared/api/sdk/sdk-response';
import { utcInstant } from '../../../shared/models/semantics/date-value';
import { entityId, type RefreshTokenSessionId } from '../../../shared/models/semantics/entity-id';
import type { ActiveSession } from '../models/active-session.model';

@Service()
export class ActiveSessionsService {
    private readonly http = inject(HttpClient);
    private readonly sessionsUrl = `${environment.apiUrls.auth}/sessions`;
    private readonly sdk = createSdkConnection(AuthSessionsSdk, this.sessionsUrl, this.http);

    public getAll(): Observable<ActiveSession[]> {
        return loadPagedCollection((page, limit) =>
            this.sdk.client
                .getAuthSessions({ version: this.sdk.version, page, limit })
                .pipe(map(values => values.map(activeSessionFromSdk))),
        );
    }

    public revoke(sessionId: RefreshTokenSessionId): Observable<void> {
        return this.sdk.client.deleteAuthSessionsBySessionId({ version: this.sdk.version, sessionId });
    }

    public revokeOthers(): Observable<void> {
        return this.sdk.client.deleteAuthSessions({ version: this.sdk.version });
    }
}

function activeSessionFromSdk(response: ActiveSessionHttpResponse): ActiveSession {
    const value = sdkNullableFields(requireSdkFields(response, ['id', 'isCurrent', 'createdAtUtc', 'lastActiveAtUtc']), [
        'authProvider',
        'browser',
        'operatingSystem',
        'deviceType',
    ]);
    return {
        ...value,
        id: entityId<'refresh-token-session'>(value.id),
        createdAtUtc: utcInstant(value.createdAtUtc),
        lastActiveAtUtc: utcInstant(value.lastActiveAtUtc),
    };
}
