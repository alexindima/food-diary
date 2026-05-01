import { DestroyRef, effect, inject, Injectable, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { SwPush } from '@angular/service-worker';
import { firstValueFrom, take } from 'rxjs';

import { AuthService } from './auth.service';
import { LocalizationService } from './localization.service';
import { NotificationService, WebPushSubscriptionRequest } from './notification.service';

export type PushNotificationEnableResult = 'subscribed' | 'already-subscribed' | 'unsupported' | 'blocked' | 'unavailable';

@Injectable({
    providedIn: 'root',
})
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
            this.isSubscribed.set(!!subscription);
            this.currentSubscriptionEndpoint.set(subscription?.endpoint ?? null);
        });

        this.swPush.pushSubscriptionChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(change => {
            void this.syncSubscriptionChange(change.oldSubscription, change.newSubscription);
        });

        this.swPush.notificationClicks.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(event => {
            const data = event.notification.data as { targetUrl?: string; url?: string } | undefined;
            const targetUrl = data?.targetUrl ?? data?.url;
            if (targetUrl) {
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
                void this.syncExistingSubscription();
            });
        });
    }

    public async ensureSubscription(): Promise<PushNotificationEnableResult> {
        if (!this.swPush.isEnabled) {
            return 'unsupported';
        }

        if (this.isBusy()) {
            return this.isSubscribed() ? 'already-subscribed' : 'unavailable';
        }

        this.isBusy.set(true);

        try {
            const current = await firstValueFrom(this.swPush.subscription.pipe(take(1)));
            if (current) {
                await firstValueFrom(this.notificationService.upsertWebPushSubscription(this.mapSubscription(current)));
                this.isSubscribed.set(true);
                this.currentSubscriptionEndpoint.set(current.endpoint);
                return 'already-subscribed';
            }

            const configuration = await firstValueFrom(this.notificationService.getWebPushConfiguration());
            if (!configuration.enabled || !configuration.publicKey) {
                return 'unavailable';
            }

            if (typeof Notification !== 'undefined' && Notification.permission === 'denied') {
                return 'blocked';
            }

            const subscription = await this.swPush.requestSubscription({
                serverPublicKey: configuration.publicKey,
            });

            await firstValueFrom(this.notificationService.upsertWebPushSubscription(this.mapSubscription(subscription)));
            this.isSubscribed.set(true);
            this.currentSubscriptionEndpoint.set(subscription.endpoint);
            return 'subscribed';
        } catch {
            if (typeof Notification !== 'undefined' && Notification.permission === 'denied') {
                return 'blocked';
            }

            return 'unavailable';
        } finally {
            this.isBusy.set(false);
        }
    }

    private async syncExistingSubscription(): Promise<void> {
        try {
            const subscription = await firstValueFrom(this.swPush.subscription.pipe(take(1)));
            this.isSubscribed.set(!!subscription);
            this.currentSubscriptionEndpoint.set(subscription?.endpoint ?? null);

            if (!subscription) {
                return;
            }

            await firstValueFrom(this.notificationService.upsertWebPushSubscription(this.mapSubscription(subscription)));
        } catch {
            this.isSubscribed.set(false);
            this.currentSubscriptionEndpoint.set(null);
        }
    }

    private async syncSubscriptionChange(
        oldSubscription: PushSubscription | null,
        newSubscription: PushSubscription | null,
    ): Promise<void> {
        try {
            if (!this.authService.isAuthenticated()) {
                this.isSubscribed.set(!!newSubscription);
                this.currentSubscriptionEndpoint.set(newSubscription?.endpoint ?? null);
                return;
            }

            if (oldSubscription) {
                await firstValueFrom(this.notificationService.removeWebPushSubscription(oldSubscription.endpoint));
            }

            if (newSubscription) {
                await firstValueFrom(this.notificationService.upsertWebPushSubscription(this.mapSubscription(newSubscription)));
            }

            this.isSubscribed.set(!!newSubscription);
            this.currentSubscriptionEndpoint.set(newSubscription?.endpoint ?? null);
        } catch {
            this.isSubscribed.set(!!newSubscription);
            this.currentSubscriptionEndpoint.set(newSubscription?.endpoint ?? null);
        }
    }

    public async removeSubscription(endpoint: string): Promise<boolean> {
        if (!endpoint || this.isBusy()) {
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
            expirationTime: subscription.expirationTime ? new Date(subscription.expirationTime).toISOString() : null,
            keys: {
                p256dh: json.keys?.['p256dh'] ?? '',
                auth: json.keys?.['auth'] ?? '',
            },
            locale: this.localizationService.getCurrentLanguage(),
            userAgent: navigator.userAgent,
        };
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
