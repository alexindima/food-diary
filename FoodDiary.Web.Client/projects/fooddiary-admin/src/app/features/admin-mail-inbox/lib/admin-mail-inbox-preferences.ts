import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { computed, inject, Injectable, linkedSignal, PLATFORM_ID } from '@angular/core';

import { AdminAuthService } from '../../admin-auth/contracts/admin-session';

export const FEEDBACK_MAILBOX = 'feedback@fooddiary.club';
export const DMARC_MAILBOX = 'dmarc@fooddiary.club';
export const INCOMING_MAILBOXES = ['admin@fooddiary.club', 'support@fooddiary.club', FEEDBACK_MAILBOX, 'bugs@fooddiary.club'] as const;

export type IncomingMailbox = (typeof INCOMING_MAILBOXES)[number];

// eslint-disable-next-line @angular-eslint/use-injectable-provided-in -- Preference state belongs to the inbox page.
@Injectable()
export class AdminMailInboxPreferences {
    private readonly document = inject(DOCUMENT);
    private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
    private readonly auth = inject(AdminAuthService);
    private readonly storageKey = computed(() => {
        const userId = this.auth.getUserId();
        return userId === null ? null : `fooddiary-admin.mail-inbox.hidden-recipients.${userId}`;
    });
    private readonly hidden = linkedSignal<IncomingMailbox[]>(() => this.readHiddenRecipients());
    public readonly hiddenRecipients = this.hidden.asReadonly();

    public setHidden(recipient: IncomingMailbox, isHidden: boolean): void {
        const updated = this.hidden().filter(value => value !== recipient);
        if (isHidden) {
            updated.push(recipient);
        }
        this.hidden.set(updated);

        const key = this.storageKey();
        if (key !== null && this.isBrowser) {
            try {
                this.document.defaultView?.localStorage.setItem(key, JSON.stringify(updated));
            } catch {
                // Keep the settings active for the current page when browser storage is unavailable.
            }
        }
    }

    private readHiddenRecipients(): IncomingMailbox[] {
        const key = this.storageKey();
        if (key === null || !this.isBrowser) {
            return [];
        }
        try {
            const stored = this.document.defaultView?.localStorage.getItem(key);
            const parsed: unknown = JSON.parse(stored ?? '[]');
            return Array.isArray(parsed) ? INCOMING_MAILBOXES.filter(recipient => parsed.includes(recipient)) : [];
        } catch {
            return [];
        }
    }
}
