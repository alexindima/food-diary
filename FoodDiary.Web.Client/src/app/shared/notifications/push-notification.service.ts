import { DOCUMENT } from '@angular/common';
import { HttpStatusCode } from '@angular/common/http';
import { DestroyRef, effect, inject, Service, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { SwPush } from '@angular/service-worker';
import { firstValueFrom, from, take, timeout } from 'rxjs';

import { AuthService } from '../../services/auth.service';
import { AuthMetadataStorageService } from '../auth/auth-metadata-storage.service';
import { SessionEventsService } from '../auth/session-events.service';
import { LocalizationService } from '../i18n/localization.service';
import { getNumberProperty, getRecordProperty, getStringProperty } from '../lib/unknown-value.utils';
import { BrowserStorageService } from '../platform/browser-storage.service';
import { BrowserWindowService } from '../platform/browser-window.service';
import { BrowserNotificationCapabilityService } from './browser-notification-capability.service';
import { NotificationService, type WebPushSubscriptionRequest } from './notification.service';

export type PushNotificationEnableResult = 'subscribed' | 'already-subscribed' | 'unsupported' | 'blocked' | 'unavailable';

type PushSession = {
    version: number;
    owner: string;
    token: string;
    localInvalidationVersion: number;
};

type OwnedPushSubscription = {
    subscription: PushSubscription | null;
    revoked: boolean;
};

const PUSH_REQUEST_TIMEOUT_MS = 5000;
const PUSH_BROWSER_LOCK = 'fooddiary.push.subscription';
const PUSH_OWNER_STORAGE_KEY = 'webPushSubscriptionOwner';

@Service()
export class PushNotificationService {
    private readonly swPush = inject(SwPush);
    private readonly authService = inject(AuthService);
    private readonly localizationService = inject(LocalizationService);
    private readonly notificationService = inject(NotificationService);
    private readonly router = inject(Router);
    private readonly destroyRef = inject(DestroyRef);
    private readonly browserNotifications = inject(BrowserNotificationCapabilityService);
    private readonly document = inject(DOCUMENT);
    private readonly sessionEvents = inject(SessionEventsService);
    private readonly browserContext = this.swPush.isEnabled
        ? { storage: inject(BrowserStorageService), window: inject(BrowserWindowService), metadata: inject(AuthMetadataStorageService) }
        : null;
    private browserMutationQueue: Promise<void> | null = null;
    private sessionVersion = 0;
    private sessionEnding = false;
    private sessionOwner = this.readSessionOwner();
    private lastSession: PushSession | null = null;
    private currentSubscription: PushSubscription | null = null;
    private subscriptionObserved = false;
    private readonly pendingDiscoveryRevocations = new Map<string, PushSession>();
    private readonly knownSubscriptions = new Map<string, PushSubscription>();
    private readonly pendingUpserts = new Set<Promise<void>>();
    private sessionCleanup: Promise<void> | null = null;
    private subscriptionRequestInFlight = false;
    private localSessionInvalidated = this.sessionEvents.isLocalSessionInvalidated;
    private locallyInvalidatedSession: PushSession | null = null;
    private localInvalidationVersion = 0;

    public readonly isSupported = signal(this.swPush.isEnabled);
    public readonly isSubscribed = signal(false);
    public readonly isBusy = signal(false);
    public readonly currentSubscriptionEndpoint = signal<string | null>(null);

    public constructor() {
        if (!this.swPush.isEnabled) {
            return;
        }

        const unregisterCleanup = this.sessionEvents.registerSessionEndCleanup(async () => this.endSessionAsync());
        this.destroyRef.onDestroy(unregisterCleanup);
        this.sessionEvents.authenticated$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            if (!this.authService.isImpersonating() && this.knownSubscriptions.size > 0) {
                void this.endSessionAsync();
            }
            this.sessionVersion++;
            this.localSessionInvalidated = false;
            this.locallyInvalidatedSession = null;
            this.sessionEnding = false;
            if (this.authService.isImpersonating()) {
                this.currentSubscription = null;
                this.knownSubscriptions.clear();
            }
            this.isBusy.set(false);
            this.setSubscriptionState(null);
        });
        this.sessionEvents.sessionEnded$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            if (this.sessionEvents.isLocalSessionInvalidated) {
                this.invalidateLocalSession();
            } else {
                void this.endSessionAsync();
            }
        });
        this.sessionEvents.localSessionInvalidating$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.invalidateLocalSession();
        });

        this.swPush.subscription.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
            next: subscription => {
                this.observeSubscription(subscription);
            },
            error: () => {
                this.isSupported.set(false);
                this.setSubscriptionState(null);
            },
        });

        this.swPush.pushSubscriptionChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(change => {
            void this.syncSubscriptionChangeAsync(change.oldSubscription, change.newSubscription);
        });

        this.swPush.notificationClicks.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(event => {
            const targetUrl = getStringProperty(event.notification.data, 'targetUrl') ?? getStringProperty(event.notification.data, 'url');
            if (targetUrl !== undefined && targetUrl.length > 0) {
                void this.router.navigateByUrl(this.browserNotifications.toAppUrl(targetUrl));
            }

            this.notificationService.fetchUnreadCount({ force: true });
            this.notificationService.notifyNotificationsChanged();
        });

        effect(() => {
            const owner = this.readSessionOwner();
            if (owner !== this.sessionOwner) {
                this.sessionOwner = owner;
                this.sessionVersion++;
            }
            if (owner === null || this.sessionEnding) {
                this.isSubscribed.set(false);
                this.currentSubscriptionEndpoint.set(null);
                return;
            }

            untracked(() => {
                void this.syncExistingSubscriptionAsync();
            });
        });
    }

    private observeSubscription(subscription: PushSubscription | null): void {
        this.subscriptionObserved = true;
        if (this.authService.isImpersonating()) {
            this.setSubscriptionState(null);
            return;
        }
        if (this.finishPendingDiscovery(subscription)) {
            return;
        }
        if (this.observeInvalidatedSubscription(subscription)) {
            return;
        }
        this.currentSubscription = subscription;
        if (subscription !== null) {
            this.knownSubscriptions.set(subscription.endpoint, subscription);
        }
        if (this.captureSession() === null) {
            this.setSubscriptionState(null);
            if (subscription !== null && this.sessionCleanup === null) {
                void this.revokeBrowserSubscriptionAsync(subscription, null);
            }
            return;
        }
        if (subscription === null) {
            this.setSubscriptionState(null);
        }
    }

    private observeInvalidatedSubscription(subscription: PushSubscription | null): boolean {
        if (!this.localSessionInvalidated) {
            return false;
        }
        if (subscription !== null && this.locallyInvalidatedSession !== null) {
            void this.revokeSubscriptionsAsync(new Map([[subscription.endpoint, subscription]]), this.locallyInvalidatedSession);
        }
        this.setSubscriptionState(null);
        return true;
    }

    private finishPendingDiscovery(subscription: PushSubscription | null): boolean {
        const previousOwners = [...this.pendingDiscoveryRevocations.values()];
        this.pendingDiscoveryRevocations.clear();
        if (subscription !== null && previousOwners.length > 0) {
            if (this.sessionCleanup === null) {
                void this.trackSessionCleanupAsync(
                    Promise.allSettled(
                        previousOwners.map(async session =>
                            this.revokeSubscriptionsAsync(new Map([[subscription.endpoint, subscription]]), session),
                        ),
                    ).then(() => {}),
                );
            }
            this.setSubscriptionState(null);
            return true;
        }
        return false;
    }

    public async ensureSubscriptionAsync(): Promise<PushNotificationEnableResult> {
        if (!this.isSupported()) {
            return 'unsupported';
        }

        if (this.isBusy() || this.subscriptionRequestInFlight) {
            return this.isSubscribed() ? 'already-subscribed' : 'unavailable';
        }

        const session = this.captureSession();
        if (session === null) {
            return 'unavailable';
        }

        this.isBusy.set(true);

        try {
            return await this.ensureSubscriptionForSessionAsync(session);
        } catch {
            if (this.isNotificationPermissionDenied()) {
                return 'blocked';
            }

            return 'unavailable';
        } finally {
            if (this.isCurrentSession(session)) {
                this.isBusy.set(false);
            }
        }
    }

    private async ensureSubscriptionForSessionAsync(session: PushSession): Promise<PushNotificationEnableResult> {
        const { subscription: current } = await this.readOwnedSubscriptionAsync(session);
        if (!this.isCurrentSession(session)) {
            return 'unavailable';
        }
        if (current !== null) {
            const replaced = await this.synchronizeExistingSubscriptionAsync(current, session);
            if (!this.isCurrentSession(session)) {
                return 'unavailable';
            }
            return replaced ? 'subscribed' : 'already-subscribed';
        }

        const configuration = await firstValueFrom(this.notificationService.getWebPushConfiguration());
        if (!this.isCurrentSession(session)) {
            return 'unavailable';
        }
        const publicKey = this.getSubscriptionPublicKey(configuration);
        if (publicKey === null) {
            return 'unavailable';
        }
        if (this.isNotificationPermissionDenied()) {
            return 'blocked';
        }

        const subscription = await this.requestSubscriptionForSessionAsync(publicKey, session);
        await this.upsertSubscriptionAsync(subscription, session);
        return this.isCurrentSession(session) ? 'subscribed' : 'unavailable';
    }

    private async syncExistingSubscriptionAsync(): Promise<void> {
        if (this.subscriptionRequestInFlight) {
            return;
        }
        const session = this.captureSession();
        if (session === null) {
            return;
        }
        try {
            const { subscription, revoked } = await this.readOwnedSubscriptionAsync(session);
            if (!this.isCurrentSession(session)) {
                return;
            }
            this.currentSubscription = subscription;

            if (subscription === null) {
                if (revoked) {
                    await this.ensureSubscriptionForSessionAsync(session);
                }
                return;
            }

            await this.synchronizeExistingSubscriptionAsync(subscription, session);
        } catch {
            if (this.isCurrentSession(session)) {
                this.setSubscriptionState(null);
            }
        }
    }

    private async syncSubscriptionChangeAsync(
        oldSubscription: PushSubscription | null,
        newSubscription: PushSubscription | null,
    ): Promise<void> {
        const session = this.captureSession();
        if (session === null) {
            await this.handleUnauthenticatedSubscriptionChangeAsync(oldSubscription, newSubscription);
            return;
        }
        try {
            const { subscription: current, revoked } = await this.readRenewedSubscriptionAsync(oldSubscription, newSubscription, session);
            if (oldSubscription !== null) {
                await firstValueFrom(this.notificationService.removeWebPushSubscription(oldSubscription.endpoint));
            }

            if (revoked) {
                await this.ensureSubscriptionForSessionAsync(session);
                return;
            }

            if (current !== null) {
                await this.upsertSubscriptionAsync(current, session);
            }

            if (current === null && this.isCurrentSession(session)) {
                this.setSubscriptionState(null);
            }
        } catch {
            if (this.isCurrentSession(session)) {
                this.setSubscriptionState(null);
            }
        }
    }

    private async readRenewedSubscriptionAsync(
        oldSubscription: PushSubscription | null,
        newSubscription: PushSubscription | null,
        session: PushSession,
    ): Promise<OwnedPushSubscription> {
        if (newSubscription === null || (oldSubscription !== null && this.isConfirmedOwner(oldSubscription, session.owner))) {
            return { subscription: newSubscription, revoked: false };
        }
        return this.readOwnedSubscriptionAsync(session);
    }

    private async handleUnauthenticatedSubscriptionChangeAsync(
        oldSubscription: PushSubscription | null,
        newSubscription: PushSubscription | null,
    ): Promise<void> {
        if (this.localSessionInvalidated && this.locallyInvalidatedSession !== null) {
            const subscriptions = [oldSubscription, newSubscription].filter(value => value !== null);
            await this.revokeSubscriptionsAsync(
                new Map(subscriptions.map(value => [value.endpoint, value])),
                this.locallyInvalidatedSession,
            );
        } else if (newSubscription !== null && !this.localSessionInvalidated) {
            await this.revokeBrowserSubscriptionAsync(newSubscription, null);
        }
    }

    public async removeSubscriptionAsync(endpoint: string): Promise<boolean> {
        if (endpoint.length === 0 || this.isBusy()) {
            return false;
        }

        const session = this.captureSession();
        if (session === null) {
            return false;
        }

        this.isBusy.set(true);

        try {
            const current = await this.readCurrentSubscriptionAsync();
            if (!this.isCurrentSession(session)) {
                return false;
            }
            return await this.removeEndpointAsync(endpoint, current, session);
        } catch {
            return false;
        } finally {
            if (this.isCurrentSession(session)) {
                this.isBusy.set(false);
            }
        }
    }

    private async removeEndpointAsync(endpoint: string, current: PushSubscription | null, session: PushSession): Promise<boolean> {
        if (current?.endpoint === endpoint && !(await this.revokeBrowserSubscriptionAsync(current, session))) {
            return false;
        }
        if (!this.isCurrentSession(session)) {
            return false;
        }
        await firstValueFrom(this.notificationService.removeWebPushSubscription(endpoint));
        if (current?.endpoint === endpoint && this.isCurrentSession(session)) {
            this.setSubscriptionState(null);
        }
        return true;
    }

    private mapSubscription(subscription: PushSubscription): WebPushSubscriptionRequest {
        const json = subscription.toJSON();
        return {
            endpoint: subscription.endpoint,
            expirationTime: subscription.expirationTime !== null ? new Date(subscription.expirationTime).toISOString() : null,
            keys: {
                p256dh: json.keys?.['p256dh'] ?? '',
                auth: json.keys?.['auth'] ?? '',
            },
            locale: this.localizationService.getCurrentLanguage(),
            userAgent: this.browserNotifications.getUserAgent(),
        };
    }

    private getSubscriptionPublicKey(configuration: { enabled: boolean; publicKey: string | null }): string | null {
        if (!configuration.enabled || configuration.publicKey === null || configuration.publicKey.length === 0) {
            return null;
        }

        return configuration.publicKey;
    }

    private async synchronizeExistingSubscriptionAsync(subscription: PushSubscription, session: PushSession): Promise<boolean> {
        this.knownSubscriptions.set(subscription.endpoint, subscription);
        let publicKey: string | null;
        try {
            const configuration = await firstValueFrom(this.notificationService.getWebPushConfiguration());
            publicKey = this.getSubscriptionPublicKey(configuration);
        } catch {
            return this.upsertOrReplaceForeignSubscriptionAsync(subscription, session, null);
        }

        if (!this.isCurrentSession(session)) {
            return false;
        }

        if (publicKey === null || this.subscriptionUsesPublicKey(subscription, publicKey)) {
            return this.upsertOrReplaceForeignSubscriptionAsync(subscription, session, publicKey);
        }

        if (!(await this.revokeBrowserSubscriptionAsync(subscription, session))) {
            return false;
        }
        await firstValueFrom(this.notificationService.removeWebPushSubscription(subscription.endpoint));
        if (!this.isCurrentSession(session)) {
            return false;
        }
        this.setSubscriptionState(null);
        const replacement = await this.requestSubscriptionForSessionAsync(publicKey, session);
        await this.upsertSubscriptionAsync(replacement, session);
        return true;
    }

    private async upsertOrReplaceForeignSubscriptionAsync(
        subscription: PushSubscription,
        session: PushSession,
        publicKey: string | null,
    ): Promise<boolean> {
        let failure: unknown;
        try {
            await this.upsertSubscriptionAsync(subscription, session);
            return false;
        } catch (error: unknown) {
            failure = error;
        }
        if (!this.isCurrentSession(session) || !this.isSubscriptionOwnerConflict(failure)) {
            throw failure;
        }

        // The current owner received an explicit rejection for this endpoint.
        // Rotating that browser subscription stops delivery for its prior owner.
        if (!(await this.revokeBrowserSubscriptionAsync(subscription, session))) {
            return false;
        }
        if (!this.isCurrentSession(session)) {
            return false;
        }
        this.knownSubscriptions.delete(subscription.endpoint);
        this.currentSubscription = null;
        this.setSubscriptionState(null);
        if (publicKey === null) {
            throw new Error('A replacement push subscription requires the server public key.');
        }
        const replacement = await this.requestSubscriptionForSessionAsync(publicKey, session);
        await this.upsertSubscriptionAsync(replacement, session);
        return true;
    }

    private isSubscriptionOwnerConflict(error: unknown): boolean {
        const payload = getRecordProperty(error, 'error');
        return (
            getNumberProperty(error, 'status') === HttpStatusCode.BadRequest &&
            getStringProperty(payload, 'error') === 'Validation.Invalid' &&
            getStringProperty(payload, 'message') === 'Field Endpoint is invalid: Endpoint cannot be registered.'
        );
    }

    private subscriptionUsesPublicKey(subscription: PushSubscription, publicKey: string): boolean {
        const applicationServerKey = subscription.options.applicationServerKey;
        if (applicationServerKey === null) {
            return true;
        }

        const binaryKey = Array.from(new Uint8Array(applicationServerKey), byte => String.fromCodePoint(byte)).join('');
        const encodedPublicKey = this.document.defaultView?.btoa(binaryKey);
        if (encodedPublicKey === undefined) {
            return true;
        }

        const currentPublicKey = encodedPublicKey.replaceAll('+', '-').replaceAll('/', '_').replace(/=+$/, '');
        const normalizedPublicKey = publicKey.replaceAll('+', '-').replaceAll('/', '_').replace(/=+$/, '');
        return currentPublicKey === normalizedPublicKey;
    }

    private isNotificationPermissionDenied(): boolean {
        return this.browserNotifications.isPermissionDenied();
    }

    private async requestSubscriptionAsync(publicKey: string): Promise<PushSubscription> {
        if (this.subscriptionRequestInFlight) {
            throw new Error('A push subscription request is already pending.');
        }
        this.subscriptionRequestInFlight = true;
        try {
            return await this.swPush.requestSubscription({ serverPublicKey: publicKey });
        } finally {
            this.subscriptionRequestInFlight = false;
        }
    }

    private async readCurrentSubscriptionAsync(): Promise<PushSubscription | null> {
        return this.swPush.isEnabled ? firstValueFrom(this.swPush.subscription.pipe(take(1), timeout(PUSH_REQUEST_TIMEOUT_MS))) : null;
    }

    private async runBrowserMutationAsync<T>(operation: () => Promise<T>): Promise<T> {
        const context = this.browserContext;
        if (context === null) {
            throw new Error('Browser push is unavailable.');
        }
        const pending =
            this.browserMutationQueue === null
                ? context.window.runWithLockAsync(PUSH_BROWSER_LOCK, operation)
                : this.browserMutationQueue.then(async () => context.window.runWithLockAsync(PUSH_BROWSER_LOCK, operation));
        const settled = pending.then(
            () => {},
            () => {},
        );
        this.browserMutationQueue = settled;
        void settled.finally(() => {
            if (this.browserMutationQueue === settled) {
                this.browserMutationQueue = null;
            }
        });
        return pending;
    }

    private isConfirmedOwner(subscription: PushSubscription, owner: string): boolean {
        const marker = this.browserContext?.storage.getJson('local', PUSH_OWNER_STORAGE_KEY);
        return getStringProperty(marker, 'endpoint') === subscription.endpoint && getStringProperty(marker, 'userId') === owner;
    }

    private forgetSubscription(subscription: PushSubscription): void {
        const marker = this.browserContext?.storage.getJson('local', PUSH_OWNER_STORAGE_KEY);
        if (getStringProperty(marker, 'endpoint') === subscription.endpoint) {
            this.browserContext?.storage.removeItem('local', PUSH_OWNER_STORAGE_KEY);
        }
        this.knownSubscriptions.delete(subscription.endpoint);
        if (this.currentSubscription?.endpoint === subscription.endpoint) {
            this.currentSubscription = null;
            this.setSubscriptionState(null);
        }
    }

    private async readOwnedSubscriptionAsync(session: PushSession): Promise<OwnedPushSubscription> {
        return this.runBrowserMutationAsync(async () => {
            this.requireCurrentSession(session);
            const subscription = await this.readCurrentSubscriptionAsync();
            this.requireCurrentSession(session);
            if (subscription === null || this.isConfirmedOwner(subscription, session.owner)) {
                return { subscription, revoked: false };
            }
            // The receiving account must stop an unconfirmed endpoint before
            // waiting for the API: its previous owner can still deliver offline.
            const removed = await firstValueFrom(from(subscription.unsubscribe()).pipe(timeout(PUSH_REQUEST_TIMEOUT_MS)));
            if (!removed) {
                throw new Error('The previous push subscription could not be revoked.');
            }
            this.forgetSubscription(subscription);
            this.requireCurrentSession(session);
            return { subscription: null, revoked: true };
        });
    }

    private requireCurrentSession(session: PushSession): void {
        if (!this.isCurrentSession(session)) {
            throw new Error('The push session has changed.');
        }
    }

    private async requestSubscriptionForSessionAsync(publicKey: string, session: PushSession): Promise<PushSubscription> {
        const request = await this.runBrowserMutationAsync<{ subscription: PushSubscription } | { pending: Promise<PushSubscription> }>(
            async () => {
                this.requireCurrentSession(session);
                const current = await this.readCurrentSubscriptionAsync();
                this.requireCurrentSession(session);
                if (current !== null) {
                    if (this.isConfirmedOwner(current, session.owner)) {
                        return { subscription: current };
                    }
                    if (!(await firstValueFrom(from(current.unsubscribe()).pipe(timeout(PUSH_REQUEST_TIMEOUT_MS))))) {
                        throw new Error('The previous push subscription could not be revoked.');
                    }
                    this.forgetSubscription(current);
                    this.requireCurrentSession(session);
                }
                // Release the browser lock while permission is pending. A later
                // result must still pass the session and endpoint checks below.
                return { pending: this.requestSubscriptionAsync(publicKey) };
            },
        );
        return 'subscription' in request ? request.subscription : request.pending;
    }

    private async revokeBrowserSubscriptionAsync(subscription: PushSubscription, session: PushSession | null): Promise<boolean> {
        return this.runBrowserMutationAsync(async () => {
            if (!this.canRevokeBrowserSubscription(subscription, session)) {
                return false;
            }
            const current = await this.readCurrentSubscriptionAsync();
            if (current?.endpoint !== subscription.endpoint || !this.canRevokeBrowserSubscription(current, session)) {
                return false;
            }
            const removed = await firstValueFrom(from(current.unsubscribe()).pipe(timeout(PUSH_REQUEST_TIMEOUT_MS)));
            if (removed) {
                this.forgetSubscription(current);
            }
            return removed;
        }).catch(() => false);
    }

    private canRevokeBrowserSubscription(subscription: PushSubscription, session: PushSession | null): boolean {
        if (this.authService.isImpersonating() || this.localSessionInvalidated) {
            return false;
        }
        const context = this.browserContext;
        if (context === null) {
            return false;
        }
        if (session === null) {
            return this.readSessionOwner() === null && !context.metadata.hasPublishedSession();
        }
        return (
            session.localInvalidationVersion === this.localInvalidationVersion &&
            !context.metadata.hasForeignSession(session.owner) &&
            !this.hasForeignConfirmedOwner(subscription, session.owner)
        );
    }

    private hasForeignConfirmedOwner(subscription: PushSubscription, owner: string): boolean {
        const marker = this.browserContext?.storage.getJson('local', PUSH_OWNER_STORAGE_KEY);
        const markerOwner = getStringProperty(marker, 'userId');
        return getStringProperty(marker, 'endpoint') === subscription.endpoint && markerOwner !== undefined && markerOwner !== owner;
    }

    private async upsertSubscriptionAsync(subscription: PushSubscription, session: PushSession): Promise<void> {
        if (!this.isCurrentSession(session)) {
            if (session.localInvalidationVersion === this.localInvalidationVersion) {
                await this.revokeBrowserSubscriptionAsync(subscription, session);
            } else {
                await this.revokeSubscriptionsAsync(new Map([[subscription.endpoint, subscription]]), session);
            }
            return;
        }
        this.currentSubscription = subscription;
        this.knownSubscriptions.set(subscription.endpoint, subscription);
        const pending = firstValueFrom(
            this.notificationService.upsertWebPushSubscription(this.mapSubscription(subscription)).pipe(timeout(PUSH_REQUEST_TIMEOUT_MS)),
        );
        this.pendingUpserts.add(pending);
        try {
            await pending;
            if (this.isCurrentSession(session)) {
                await this.runBrowserMutationAsync(async () => {
                    this.requireCurrentSession(session);
                    const current = await this.readCurrentSubscriptionAsync();
                    this.requireCurrentSession(session);
                    if (current?.endpoint !== subscription.endpoint) {
                        throw new Error('The browser push subscription has changed.');
                    }
                    this.browserContext?.storage.setJson('local', PUSH_OWNER_STORAGE_KEY, {
                        endpoint: subscription.endpoint,
                        userId: session.owner,
                    });
                    this.setSubscriptionState(subscription);
                });
            } else if (this.sessionCleanup === null) {
                await this.revokeSubscriptionsAsync(new Map([[subscription.endpoint, subscription]]), session);
            }
        } finally {
            this.pendingUpserts.delete(pending);
        }
    }

    private readSessionOwner(): string | null {
        return this.authService.isAuthenticated() && !this.authService.isImpersonating() ? this.authService.getUserId() : null;
    }

    private captureSession(): PushSession | null {
        const owner = this.readSessionOwner();
        if (owner !== this.sessionOwner) {
            this.sessionOwner = owner;
            this.sessionVersion++;
        }
        const token = this.authService.getSessionKey();
        if (
            owner === null ||
            token === null ||
            this.sessionEnding ||
            this.sessionCleanup !== null ||
            this.pendingDiscoveryRevocations.size > 0
        ) {
            return null;
        }
        const session = { version: this.sessionVersion, owner, token, localInvalidationVersion: this.localInvalidationVersion };
        this.lastSession = session;
        return session;
    }

    private isCurrentSession(session: PushSession): boolean {
        return (
            !this.sessionEnding &&
            session.version === this.sessionVersion &&
            session.owner === this.readSessionOwner() &&
            this.browserContext?.metadata.hasForeignSession(session.owner) === false
        );
    }

    private async endSessionAsync(): Promise<void> {
        if (this.localSessionInvalidated || this.authService.isImpersonating()) {
            return;
        }
        if (this.sessionCleanup !== null) {
            return this.sessionCleanup;
        }
        if (this.sessionEnding) {
            return;
        }
        const session = this.captureSession() ?? this.lastSession;
        this.lastSession = null;
        this.sessionEnding = true;
        this.sessionVersion++;
        this.isBusy.set(false);
        this.setSubscriptionState(null);
        const subscriptions = new Map(this.knownSubscriptions);
        if (this.currentSubscription !== null) {
            subscriptions.set(this.currentSubscription.endpoint, this.currentSubscription);
        }
        this.knownSubscriptions.clear();
        this.currentSubscription = null;
        if (session !== null && !this.subscriptionObserved) {
            this.pendingDiscoveryRevocations.set(session.owner, session);
        }
        const discovery = this.discoverSubscriptionAsync();
        const cleanup = (async (): Promise<void> => {
            const discovered = await discovery;
            if (discovered !== null) {
                subscriptions.set(discovered.endpoint, discovered);
            }
            await this.revokeSubscriptionsAsync(subscriptions, session);
        })();
        return this.trackSessionCleanupAsync(cleanup);
    }

    private async discoverSubscriptionAsync(): Promise<PushSubscription | null> {
        if (this.subscriptionObserved) {
            return null;
        }
        return firstValueFrom(this.swPush.subscription.pipe(take(1), timeout(PUSH_REQUEST_TIMEOUT_MS))).catch(() => null);
    }

    private async revokeSubscriptionsAsync(subscriptions: Map<string, PushSubscription>, session: PushSession | null): Promise<void> {
        // Browser revocation runs even offline. Wait for already-sent registrations
        // before deleting their server records, always using their original owner.
        const unsubscribe = Promise.allSettled(
            [...subscriptions.values()].map(async subscription => this.revokeBrowserSubscriptionAsync(subscription, session)),
        );
        const upserts = Promise.allSettled(this.pendingUpserts);
        await Promise.allSettled([unsubscribe, upserts]);
        if (session !== null) {
            await Promise.allSettled(
                [...subscriptions.keys()].map(async endpoint =>
                    firstValueFrom(
                        this.notificationService.removeWebPushSubscription(endpoint, session.token).pipe(timeout(PUSH_REQUEST_TIMEOUT_MS)),
                    ),
                ),
            );
        }
    }

    private setSubscriptionState(subscription: PushSubscription | null): void {
        this.isSubscribed.set(subscription !== null);
        this.currentSubscriptionEndpoint.set(subscription?.endpoint ?? null);
    }

    private invalidateLocalSession(): void {
        if (this.localSessionInvalidated) {
            return;
        }
        const owner = this.readSessionOwner();
        const token = this.authService.getSessionKey();
        const session =
            owner !== null && token !== null
                ? { version: this.sessionVersion, owner, token, localInvalidationVersion: this.localInvalidationVersion }
                : this.lastSession;
        const subscriptions = new Map(this.knownSubscriptions);
        if (this.currentSubscription !== null) {
            subscriptions.set(this.currentSubscription.endpoint, this.currentSubscription);
        }
        this.localSessionInvalidated = true;
        this.locallyInvalidatedSession = session;
        this.localInvalidationVersion++;
        this.sessionVersion++;
        this.sessionEnding = true;
        this.lastSession = null;
        this.currentSubscription = null;
        this.knownSubscriptions.clear();
        this.isBusy.set(false);
        this.setSubscriptionState(null);
        if (session !== null) {
            if (!this.subscriptionObserved) {
                this.pendingDiscoveryRevocations.set(session.owner, session);
            }
            const pendingCleanup = this.sessionCleanup;
            const cleanup = (async (): Promise<void> => {
                const discovered = await this.discoverSubscriptionAsync();
                if (discovered !== null) {
                    subscriptions.set(discovered.endpoint, discovered);
                }
                await this.revokeSubscriptionsAsync(subscriptions, session);
            })();
            void this.trackSessionCleanupAsync(Promise.allSettled([pendingCleanup, cleanup]).then(() => {}));
        }
    }

    private async trackSessionCleanupAsync(cleanup: Promise<void>): Promise<void> {
        const tracked = cleanup.finally(() => {
            if (this.sessionCleanup === tracked) {
                this.sessionCleanup = null;
                if (
                    !this.destroyRef.destroyed &&
                    !this.sessionEnding &&
                    !this.localSessionInvalidated &&
                    this.readSessionOwner() !== null
                ) {
                    void this.syncExistingSubscriptionAsync();
                }
            }
        });
        this.sessionCleanup = tracked;
        await tracked;
    }
}
