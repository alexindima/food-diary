import { DOCUMENT } from '@angular/common';
import { HttpStatusCode } from '@angular/common/http';
import { DestroyRef, effect, inject, Service, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router } from '@angular/router';
import { filter, firstValueFrom, merge, timeout } from 'rxjs';

import { AuthService } from '../../services/auth.service';
import { SessionEventsService } from '../auth/session-events.service';
import { getNumberProperty, getRecordProperty, getStringProperty } from '../lib/unknown-value.utils';
import type { NotificationId } from '../models/semantics/entity-id';
import { BrowserStorageService } from '../platform/browser-storage.service';
import { NotificationService } from './notification.service';
import { parsePushNotificationId, parsePushNotificationReference, type PushNotificationReference } from './push-notification-reference';

type ReadSession = { owner: string; key: string; version: number };
const PENDING_READS_KEY = 'webPushPendingReads:';
const READ_RETRY_INTERVAL_MS = 30_000;
const READ_REQUEST_TIMEOUT_MS = 5000;
const MAX_PENDING_READS = 100;

/** Durable click receipts contain only IDs and are replayed through the authenticated API. */
@Service()
export class PushNotificationInteractionService {
    private readonly auth = inject(AuthService);
    private readonly notifications = inject(NotificationService);
    private readonly sessionEvents = inject(SessionEventsService);
    private readonly storage = inject(BrowserStorageService);
    private readonly router = inject(Router);
    private readonly document = inject(DOCUMENT);
    private readonly destroyRef = inject(DestroyRef);
    private readonly pending = new Map<string, Set<NotificationId>>();
    private sessionVersion = 0;
    private queueVersion = 0;
    private flushing: Promise<void> | null = null;

    public constructor() {
        this.router.events
            .pipe(
                filter(event => event instanceof NavigationEnd),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(event => {
                this.captureNavigation(event.urlAfterRedirects);
            });
        merge(this.sessionEvents.sessionEnded$, this.sessionEvents.localSessionInvalidating$)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => {
                this.sessionVersion++;
            });
        this.sessionEvents.authenticated$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.sessionVersion++;
            void this.flushAsync();
        });
        effect(() => {
            if (this.captureSession() !== null) {
                untracked(() => {
                    void this.flushAsync();
                });
            }
        });

        const resume = (): void => {
            if (this.document.visibilityState !== 'hidden') {
                void this.flushAsync();
            }
        };
        const view = this.document.defaultView;
        this.document.addEventListener('visibilitychange', resume);
        view?.addEventListener('focus', resume);
        view?.addEventListener('online', resume);
        const interval = view?.setInterval(resume, READ_RETRY_INTERVAL_MS);
        this.destroyRef.onDestroy(() => {
            this.document.removeEventListener('visibilitychange', resume);
            view?.removeEventListener('focus', resume);
            view?.removeEventListener('online', resume);
            view?.clearInterval(interval);
        });
        this.captureNavigation(this.router.url);
    }

    public recordClick(notification: unknown): boolean {
        const data = getRecordProperty(notification, 'data');
        const reference =
            parsePushNotificationReference(data) ??
            parsePushNotificationReference({
                notificationId: getStringProperty(notification, 'tag'),
                userId: this.captureSession()?.owner,
            });
        if (reference === null) {
            return false;
        }
        this.enqueue(reference);
        return true;
    }

    private captureNavigation(url: string): void {
        const tree = this.router.parseUrl(url);
        const reference = parsePushNotificationReference({
            notificationId: getStringProperty(tree.queryParams, 'notificationId'),
            userId: getStringProperty(tree.queryParams, 'notificationUserId'),
        });
        if (reference === null) {
            return;
        }
        this.enqueue(reference);
    }

    private enqueue(reference: PushNotificationReference): void {
        const pending = this.readPending(reference.userId);
        pending.add(reference.notificationId);
        this.savePending(reference.userId, pending);
        this.queueVersion++;
        void this.flushAsync();
    }

    private captureSession(): ReadSession | null {
        const owner = this.auth.isAuthenticated() && !this.auth.isImpersonating() ? this.auth.getUserId() : null;
        const key = this.auth.getSessionKey();
        return owner !== null && key !== null ? { owner, key, version: this.sessionVersion } : null;
    }

    private isCurrentSession(session: ReadSession): boolean {
        const current = this.captureSession();
        return current?.owner === session.owner && current.key === session.key && current.version === session.version;
    }

    private async flushAsync(): Promise<void> {
        if (this.flushing !== null) {
            return this.flushing;
        }
        const session = this.captureSession();
        if (session === null) {
            return;
        }
        const queueVersion = this.queueVersion;
        const pending = this.flushSessionAsync(session).finally(() => {
            this.flushing = null;
            if (!this.destroyRef.destroyed && (this.queueVersion !== queueVersion || !this.isCurrentSession(session))) {
                void this.flushAsync();
            }
        });
        this.flushing = pending;
        return pending;
    }

    private async flushSessionAsync(session: ReadSession): Promise<void> {
        let changed = false;
        for (const notificationId of this.readPending(session.owner)) {
            if (!this.isCurrentSession(session) || !(await this.acknowledgeAsync(notificationId))) {
                break;
            }
            if (!this.isCurrentSession(session)) {
                return;
            }
            const pending = this.readPending(session.owner);
            pending.delete(notificationId);
            this.savePending(session.owner, pending);
            changed = true;
        }
        if (changed && this.isCurrentSession(session)) {
            this.notifications.fetchUnreadCount({ force: true, supersedePending: true });
            this.notifications.notifyNotificationsChanged({ supersedePending: true });
        }
    }

    private async acknowledgeAsync(notificationId: NotificationId): Promise<boolean> {
        try {
            await firstValueFrom(this.notifications.markAsRead(notificationId).pipe(timeout(READ_REQUEST_TIMEOUT_MS)));
            return true;
        } catch (error: unknown) {
            // Deleted/expired notifications no longer need a receipt. Other failures retry.
            return getNumberProperty(error, 'status') === HttpStatusCode.NotFound;
        }
    }

    private readPending(owner: string): Set<NotificationId> {
        const stored = this.storage.getJson('local', PENDING_READS_KEY + owner);
        const pending = new Set(this.pending.get(owner));
        for (const value of Array.isArray(stored) ? stored : []) {
            const notificationId = parsePushNotificationId(value);
            if (notificationId !== null) {
                pending.add(notificationId);
            }
        }
        return pending;
    }

    private savePending(owner: string, pending: Set<NotificationId>): void {
        const bounded = new Set([...pending].slice(-MAX_PENDING_READS));
        this.pending.set(owner, bounded);
        if (bounded.size === 0) {
            this.storage.removeItem('local', PENDING_READS_KEY + owner);
        } else {
            this.storage.setJson('local', PENDING_READS_KEY + owner, [...bounded]);
        }
    }
}
