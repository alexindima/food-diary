import { DestroyRef, effect, inject, Injectable, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { SwPush } from '@angular/service-worker';
import { firstValueFrom, take } from 'rxjs';

import { getStringProperty } from '../shared/lib/unknown-value.utils';
import { AuthService } from './auth.service';
import { LocalizationService } from './localization.service';
import { NotificationService, type WebPushSubscriptionRequest } from './notification.service';

export type PushNotificationEnableResult = 'subscribed' | 'already-subscribed' | 'unsupported' | 'blocked' | 'unavailable';

@Injectable({ providedIn: 'root' })
export class PushNotificationService {
    private readonly swPush = inject(SwPush);
    private readonly authService = inject(AuthService);
    private readonly localizationService = inject(LocalizationService);
    private readonly notificationService = inject(NotificationService);
    private readonly router = inject(Router);
    private readonly destroyRef = inject(DestroyRef);

    public readonly isSupported = signal(this.swPush.isEnabled);
    public readonly isSubscribed = signal(false);
    public readonly isBusy = signal(false);
    public readonly currentSubscriptionEndpoint = signal<string | null>(null);

    public constructor() {
        if (!this.swPush.isEnabled) {
            return;
        }

        this.swPush.subscription.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(subscription => {
            this.setSubscriptionState(subscription);
        });

        this.swPush.pushSubscriptionChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(change => {
            void this.syncSubscriptionChangeAsync(change.oldSubscription, change.newSubscription);
        });

        this.swPush.notificationClicks.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(event => {
            const targetUrl = getStringProperty(event.notification.data, 'targetUrl') ?? getStringProperty(event.notification.data, 'url');
            if (targetUrl !== undefined && targetUrl.length > 0) {
                void this.router.navigateByUrl(this.toAppUrl(targetUrl));
            }

            this.notificationService.fetchUnreadCount({ force: true });
            this.notificationService.notifyNotificationsChanged();
        });

        effect(() => {
            if (!this.authService.isAuthenticated()) {
                this.isSubscribed.set(false);
                this.currentSubscriptionEndpoint.set(null);
                return;
            }

            untracked(() => {
                void this.syncExistingSubscriptionAsync();
            });
        });
    }

    public async ensureSubscriptionAsync(): Promise<PushNotificationEnableResult> {
        if (!this.swPush.isEnabled) {
            return 'unsupported';
        }

        if (this.isBusy()) {
            return this.isSubscribed() ? 'already-subscribed' : 'unavailable';
        }

        this.isBusy.set(true);

        try {
            const current = await firstValueFrom(this.swPush.subscription.pipe(take(1)));
            if (current !== null) {
                await this.upsertSubscriptionAsync(current);
                return 'already-subscribed';
            }

            const configuration = await firstValueFrom(this.notificationService.getWebPushConfiguration());
            const publicKey = this.getSubscriptionPublicKey(configuration);
            if (publicKey === null) {
                return 'unavailable';
            }

            if (this.isNotificationPermissionDenied()) {
                return 'blocked';
            }

            const subscription = await this.swPush.requestSubscription({
                serverPublicKey: publicKey,
            });

            await this.upsertSubscriptionAsync(subscription);
            return 'subscribed';
        } catch {
            if (this.isNotificationPermissionDenied()) {
                return 'blocked';
            }

            return 'unavailable';
        } finally {
            this.isBusy.set(false);
        }
    }

    private async syncExistingSubscriptionAsync(): Promise<void> {
        try {
            const subscription = await firstValueFrom(this.swPush.subscription.pipe(take(1)));
            this.setSubscriptionState(subscription);

            if (subscription === null) {
                return;
            }

            await this.upsertSubscriptionAsync(subscription);
        } catch {
            this.setSubscriptionState(null);
        }
    }

    private async syncSubscriptionChangeAsync(
        oldSubscription: PushSubscription | null,
        newSubscription: PushSubscription | null,
    ): Promise<void> {
        try {
            if (!this.authService.isAuthenticated()) {
                this.setSubscriptionState(newSubscription);
                return;
            }

            if (oldSubscription !== null) {
                await firstValueFrom(this.notificationService.removeWebPushSubscription(oldSubscription.endpoint));
            }

            if (newSubscription !== null) {
                await this.upsertSubscriptionAsync(newSubscription);
            }

            this.setSubscriptionState(newSubscription);
        } catch {
            this.setSubscriptionState(newSubscription);
        }
    }

    public async removeSubscriptionAsync(endpoint: string): Promise<boolean> {
        if (endpoint.length === 0 || this.isBusy()) {
            return false;
        }

        this.isBusy.set(true);

        try {
            const current = this.swPush.isEnabled ? await firstValueFrom(this.swPush.subscription.pipe(take(1))) : null;
            if (current?.endpoint === endpoint) {
                await current.unsubscribe();
                await firstValueFrom(this.notificationService.removeWebPushSubscription(endpoint));
                this.isSubscribed.set(false);
                this.currentSubscriptionEndpoint.set(null);
                return true;
            }

            await firstValueFrom(this.notificationService.removeWebPushSubscription(endpoint));
            return true;
        } catch {
            return false;
        } finally {
            this.isBusy.set(false);
        }
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
            userAgent: navigator.userAgent,
        };
    }

    private getSubscriptionPublicKey(configuration: { enabled: boolean; publicKey: string | null }): string | null {
        if (!configuration.enabled || configuration.publicKey === null || configuration.publicKey.length === 0) {
            return null;
        }

        return configuration.publicKey;
    }

    private isNotificationPermissionDenied(): boolean {
        return typeof Notification !== 'undefined' && Notification.permission === 'denied';
    }

    private async upsertSubscriptionAsync(subscription: PushSubscription): Promise<void> {
        await firstValueFrom(this.notificationService.upsertWebPushSubscription(this.mapSubscription(subscription)));
        this.setSubscriptionState(subscription);
    }

    private setSubscriptionState(subscription: PushSubscription | null): void {
        this.isSubscribed.set(subscription !== null);
        this.currentSubscriptionEndpoint.set(subscription?.endpoint ?? null);
    }

    private toAppUrl(url: string): string {
        if (!/^https?:\/\//i.test(url)) {
            return url;
        }

        try {
            const parsed = new URL(url, window.location.origin);
            if (parsed.origin === window.location.origin) {
                return `${parsed.pathname}${parsed.search}${parsed.hash}`;
            }
        } catch {
            return url;
        }

        return url;
    }
}
