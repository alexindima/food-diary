import { DOCUMENT } from '@angular/common';
import { DestroyRef, effect, inject, Service, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { firstValueFrom, merge, timeout } from 'rxjs';

import { AuthService } from '../../services/auth.service';
import { SessionEventsService } from '../auth/session-events.service';
import type { NotificationId } from '../models/semantics/entity-id';
import { BrowserNotificationCapabilityService } from './browser-notification-capability.service';
import { NotificationService } from './notification.service';
import { parsePushNotificationReference } from './push-notification-reference';

const STATUS_BATCH_SIZE = 100;
const STATUS_TIMEOUT_MS = 5000;

type SyncSession = { owner: string; version: number };

@Service()
export class PushNotificationReadSyncService {
    private readonly auth = inject(AuthService);
    private readonly sessions = inject(SessionEventsService);
    private readonly notifications = inject(NotificationService);
    private readonly browser = inject(BrowserNotificationCapabilityService);
    private readonly document = inject(DOCUMENT);
    private readonly destroyRef = inject(DestroyRef);
    private readonly suspended = signal(false);
    private version = 0;
    private destroyed = false;
    private running = false;
    private requested = false;

    public constructor() {
        this.sessions.authenticated$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.version++;
            this.suspended.set(false);
            this.requestSync();
        });
        merge(this.sessions.sessionEnded$, this.sessions.localSessionInvalidating$)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => {
                this.version++;
                this.suspended.set(true);
            });
        this.notifications.readStateChanged$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.requestSync();
        });
        effect(() => {
            this.owner();
            untracked(() => {
                this.version++;
                this.requestSync();
            });
        });

        const resume = (): void => {
            if (this.document.visibilityState !== 'hidden') {
                this.requestSync();
            }
        };
        this.document.addEventListener('visibilitychange', resume);
        const view = this.document.defaultView;
        view?.addEventListener('focus', resume);
        view?.addEventListener('online', resume);
        this.destroyRef.onDestroy(() => {
            this.destroyed = true;
            this.document.removeEventListener('visibilitychange', resume);
            view?.removeEventListener('focus', resume);
            view?.removeEventListener('online', resume);
        });
    }

    private owner(): string | null {
        return !this.destroyed &&
            !this.suspended() &&
            !this.sessions.isLocalSessionInvalidated &&
            this.auth.isAuthenticated() &&
            !this.auth.isImpersonating()
            ? this.auth.getUserId()
            : null;
    }

    private isCurrent(session: SyncSession): boolean {
        return session.version === this.version && session.owner === this.owner();
    }

    private requestSync(): void {
        this.requested = true;
        if (!this.running) {
            void this.drainAsync();
        }
    }

    private async drainAsync(): Promise<void> {
        this.running = true;
        try {
            while (this.requested && !this.destroyed) {
                this.requested = false;
                const owner = this.owner();
                if (owner !== null) {
                    try {
                        await this.reconcileAsync({ owner, version: this.version });
                    } catch {
                        // Offline, unsupported and OS notification failures must not affect reading.
                        // The next realtime hint, reconciliation tick or resume retries from server state.
                    }
                }
            }
        } finally {
            this.running = false;
        }
    }

    private async reconcileAsync(session: SyncSession): Promise<void> {
        const displayed = await this.browser.getDisplayedNotificationsAsync();
        if (!this.isCurrent(session)) {
            return;
        }
        const owned = new Map<NotificationId, Notification[]>();
        for (const notification of displayed) {
            const reference = parsePushNotificationReference(notification.data);
            if (reference?.userId === session.owner) {
                const matches = owned.get(reference.notificationId) ?? [];
                matches.push(notification);
                owned.set(reference.notificationId, matches);
            }
        }

        const ids = [...owned.keys()];
        for (let offset = 0; offset < ids.length; offset += STATUS_BATCH_SIZE) {
            if (!this.isCurrent(session)) {
                return;
            }
            const readIds = await firstValueFrom(
                this.notifications.getReadNotificationIds(ids.slice(offset, offset + STATUS_BATCH_SIZE)).pipe(timeout(STATUS_TIMEOUT_MS)),
            );
            this.closeReadNotifications(session, readIds, owned);
        }
    }

    private closeReadNotifications(
        session: SyncSession,
        readIds: readonly NotificationId[],
        owned: ReadonlyMap<NotificationId, Notification[]>,
    ): void {
        for (const id of readIds) {
            if (!this.isCurrent(session)) {
                return;
            }
            for (const notification of owned.get(id) ?? []) {
                try {
                    notification.close();
                } catch {
                    // One failed OS dismissal must not prevent closing the other confirmed reads.
                }
            }
        }
    }
}
