import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { inject, Injector, PLATFORM_ID, Service } from '@angular/core';

import { FrontendObservabilityService, type ServiceWorkerFailureStage } from '../../services/frontend-observability.service';

const FAILURE_SUPPRESSION_MS = 300_000;
const MAX_RECENT_FAILURES = 16;
const VERSION_HASH_PATTERN = /^(?:[a-f0-9]{40}|[a-f0-9]{64})$/iu;

@Service()
export class ServiceWorkerDiagnosticsService {
    private readonly document = inject(DOCUMENT);
    private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
    private readonly injector = inject(Injector);
    private readonly reportedAt = new Map<string, number>();

    public report(stage: ServiceWorkerFailureStage, version?: string): void {
        if (!this.canReport(stage)) {
            return;
        }
        const targetVersion = version !== undefined && VERSION_HASH_PATTERN.test(version) ? version : undefined;
        const key = `${stage}:${targetVersion ?? ''}`;
        const now = Date.now();
        const previous = this.reportedAt.get(key);
        if (previous !== undefined && now - previous < FAILURE_SUPPRESSION_MS) {
            return;
        }
        this.reportedAt.delete(key);
        this.reportedAt.set(key, now);
        if (this.reportedAt.size > MAX_RECENT_FAILURES) {
            const oldest = this.reportedAt.keys().next().value;
            if (oldest !== undefined) {
                this.reportedAt.delete(oldest);
            }
        }
        try {
            this.injector.get(FrontendObservabilityService).recordServiceWorkerFailure(stage, targetVersion);
        } catch {
            // Diagnostics must never interrupt recovery or the current app version.
        }
    }

    private canReport(stage: ServiceWorkerFailureStage): boolean {
        const view = this.document.defaultView;
        return this.isBrowser && view !== null && (stage === 'recovery' || view.navigator.onLine);
    }
}
