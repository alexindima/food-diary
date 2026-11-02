import { inject, Service, signal } from '@angular/core';
import { Subject } from 'rxjs';

import { BrowserStorageService } from '../platform/browser-storage.service';
import { PRIVACY_NOTICE_VERSION } from '../privacy/privacy-notice-version';

export const MARKETING_NOTICE_VERSION = PRIVACY_NOTICE_VERSION;
const STORAGE_KEY = 'fd_marketing_consent';
const VALIDITY_MS = 15_552_000_000;

type ConsentDecision = { version: string; allowed: boolean; decidedAt: string };

@Service()
export class MarketingConsentService {
    private readonly storage = inject(BrowserStorageService);
    private readonly decision = signal<ConsentDecision | null>(this.readDecision());
    private deniedInThisSession = false;
    public readonly changed = new Subject<boolean>();
    public readonly currentDecision = this.decision.asReadonly();
    public readonly accountSyncFailed = signal(false);

    public isAllowed(): boolean {
        return !this.deniedInThisSession && this.readDecision()?.allowed === true;
    }

    public decide(allowed: boolean): void {
        this.deniedInThisSession = !allowed;
        if (!allowed) {
            this.storage.removeItem('local', STORAGE_KEY);
        }
        const decision = { version: MARKETING_NOTICE_VERSION, allowed, decidedAt: new Date().toISOString() };
        this.storage.setJson('local', STORAGE_KEY, decision);
        this.decision.set(decision);
        this.changed.next(allowed);
    }

    public grantedAt(): string | null {
        if (this.deniedInThisSession) {
            return null;
        }
        const decision = this.readDecision();
        return decision?.allowed === true ? decision.decidedAt : null;
    }

    private readDecision(): ConsentDecision | null {
        const value = this.storage.getJson('local', STORAGE_KEY);
        if (!isDecision(value)) {
            return null;
        }
        const age = Date.now() - Date.parse(value.decidedAt);
        return Number.isFinite(age) && age >= 0 && age < VALIDITY_MS ? value : null;
    }
}

function isDecision(value: unknown): value is ConsentDecision {
    if (typeof value !== 'object' || value === null || !('version' in value) || !('allowed' in value) || !('decidedAt' in value)) {
        return false;
    }
    return value.version === MARKETING_NOTICE_VERSION && typeof value.allowed === 'boolean' && typeof value.decidedAt === 'string';
}
