import { DOCUMENT } from '@angular/common';
import { inject, isDevMode, Service } from '@angular/core';

import type { DevelopmentRequestCompletion } from '../../shared/development/development-tools';
import { localModelContext } from './local-webmcp.types';
import { developmentApiIdentity, developmentApiPathParts, developmentOperation } from './local-webmcp-request-metadata';

type DevelopmentRequest = {
    id: number;
    method: string;
    resource: string;
    operation: string | null;
    origin: string;
    status: number | null;
    pending: boolean;
    apiRuntime: ReturnType<typeof developmentApiIdentity>;
};
const HISTORY_LIMIT = 20;
const RESOURCES = new Set([
    'products',
    'recipes',
    'meals',
    'weight-entries',
    'waist-entries',
    'dietologist',
    'recommendations',
    'client-tasks',
    'admin',
    'cycles',
    'dashboard',
    'statistics',
]);

@Service()
export class LocalWebmcpRequestsService {
    private readonly document = inject(DOCUMENT);

    public observe(method: string, url: string): DevelopmentRequestCompletion {
        try {
            if (localModelContext(this.document, isDevMode()) !== undefined) {
                return this.begin(method, new URL(url, this.document.location.origin));
            }
        } catch {
            /* Optional diagnostics must not affect normal HTTP forwarding. */
        }
        return () => {};
    }
    private requests: DevelopmentRequest[] = [];
    private sequence = 0;

    public clear(): void {
        this.requests = [];
    }

    public begin(method: string, url: URL): DevelopmentRequestCompletion {
        // Resource name only: no IDs, query values, request/response bodies, headers or auth endpoints.
        const resource = developmentApiPathParts(url.pathname)?.[0];
        if (resource === undefined || !RESOURCES.has(resource)) {
            return () => {};
        }
        const request: DevelopmentRequest = {
            id: ++this.sequence,
            method,
            resource,
            operation: developmentOperation(method, url.pathname),
            origin: url.origin,
            status: null,
            pending: true,
            apiRuntime: null,
        };
        this.requests = [...this.requests.filter(item => item.pending || item.id > this.sequence - HISTORY_LIMIT), request];
        const complete: DevelopmentRequestCompletion = (status, metadata) => {
            request.pending = false;
            request.status = status ?? null;
            request.apiRuntime = developmentApiIdentity(metadata?.apiBuild, metadata?.apiInstance);
        };
        complete.requestId = request.id;
        return complete;
    }

    public read(): DevelopmentRequest[] {
        return this.requests.map(request => ({ ...request }));
    }
}
