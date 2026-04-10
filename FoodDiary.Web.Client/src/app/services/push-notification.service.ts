import { DestroyRef, effect, inject, Injectable, signal, untracked } from '@angular/core';
import { SwPush } from '@angular/service-worker';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { firstValueFrom, take } from 'rxjs';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';
import { LocalizationService } from './localization.service';
import { NotificationService, WebPushSubscriptionRequest } from './notification.service';

export type PushNotificationToggleResult = 'subscribed' | 'unsubscribed' | 'unsupported' | 'unavailable';

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

    public constructor() {
        if (!this.swPush.isEnabled) {
            return;
        }

        this.swPush.subscription.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(subscription => {
            this.isSubscribed.set(!!subscription);
        });

        this.swPush.pushSubscriptionChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(change => {
            void this.syncSubscriptionChange(change.oldSubscription, change.newSubscription);
        });

        this.swPush.notificationClicks.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(event => {
            const data = event.notification.data as { url?: string } | undefined;
            if (data?.url) {
                void this.router.navigateByUrl(data.url);
            }

            this.notificationService.fetchUnreadCount();
            this.notificationService.notifyNotificationsChanged();
        });

        effect(() => {
            if (!this.authService.isAuthenticated()) {
                this.isSubscribed.set(false);
                return;
            }

            untracked(() => {
                void this.syncExistingSubscription();
            });
        });
    }

    public async toggleSubscription(): Promise<PushNotificationToggleResult> {
        if (!this.swPush.isEnabled) {
            return 'unsupported';
        }

        if (this.isBusy()) {
            return this.isSubscribed() ? 'subscribed' : 'unavailable';
        }

        this.isBusy.set(true);

        try {
            const current = await firstValueFrom(this.swPush.subscription.pipe(take(1)));
            if (current) {
                await firstValueFrom(this.notificationService.removeWebPushSubscription(current.endpoint));
                await current.unsubscribe();
                this.isSubscribed.set(false);
                return 'unsubscribed';
            }

            const configuration = await firstValueFrom(this.notificationService.getWebPushConfiguration());
            if (!configuration.enabled || !configuration.publicKey) {
                return 'unavailable';
            }

            const subscription = await this.swPush.requestSubscription({
                serverPublicKey: configuration.publicKey,
            });

            await firstValueFrom(this.notificationService.upsertWebPushSubscription(this.mapSubscription(subscription)));
            this.isSubscribed.set(true);
            return 'subscribed';
        } catch {
            return 'unavailable';
        } finally {
            this.isBusy.set(false);
        }
    }

    private async syncExistingSubscription(): Promise<void> {
        try {
            const subscription = await firstValueFrom(this.swPush.subscription.pipe(take(1)));
            this.isSubscribed.set(!!subscription);

            if (!subscription) {
                return;
            }

            await firstValueFrom(this.notificationService.upsertWebPushSubscription(this.mapSubscription(subscription)));
        } catch {
            this.isSubscribed.set(false);
        }
    }

    private async syncSubscriptionChange(
        oldSubscription: PushSubscription | null,
        newSubscription: PushSubscription | null,
    ): Promise<void> {
        try {
            if (!this.authService.isAuthenticated()) {
                this.isSubscribed.set(!!newSubscription);
                return;
            }

            if (oldSubscription) {
                await firstValueFrom(this.notificationService.removeWebPushSubscription(oldSubscription.endpoint));
            }

            if (newSubscription) {
                await firstValueFrom(this.notificationService.upsertWebPushSubscription(this.mapSubscription(newSubscription)));
            }

            this.isSubscribed.set(!!newSubscription);
        } catch {
            this.isSubscribed.set(!!newSubscription);
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
}
