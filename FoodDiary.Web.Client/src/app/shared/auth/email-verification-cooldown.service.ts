import { inject, Service } from '@angular/core';

import { AUTH_EMAIL_RESEND_COOLDOWN_SECONDS } from '../../config/runtime-ui.tokens';
import { MS_PER_SECOND } from '../lib/time.constants';
import { BrowserStorageService } from '../platform/browser-storage.service';

@Service()
export class EmailVerificationCooldownService {
    private readonly storage = inject(BrowserStorageService);
    private readonly cooldownSeconds = inject(AUTH_EMAIL_RESEND_COOLDOWN_SECONDS);
    private readonly deadlines = new Map<string, number>();

    public markSent(userId: string | null): void {
        if (userId === null) {
            return;
        }
        const deadline = Date.now() + this.cooldownSeconds * MS_PER_SECOND;
        this.deadlines.set(userId, deadline);
        this.storage.setItem('session', this.key(userId), String(deadline));
    }

    public remainingSeconds(userId: string | null): number {
        if (userId === null) {
            return 0;
        }
        const deadline = this.deadlines.get(userId) ?? Number(this.storage.getItem('session', this.key(userId)));
        if (!Number.isFinite(deadline)) {
            return 0;
        }
        return Math.min(this.cooldownSeconds, Math.max(0, Math.ceil((deadline - Date.now()) / MS_PER_SECOND)));
    }

    private key(userId: string): string {
        return `email-verification-cooldown:${userId}`;
    }
}
