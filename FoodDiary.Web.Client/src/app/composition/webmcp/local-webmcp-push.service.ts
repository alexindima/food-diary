import { DOCUMENT } from '@angular/common';
import { inject, Service } from '@angular/core';
import { firstValueFrom, timeout } from 'rxjs';

import { NotificationService } from '../../shared/notifications/notification.service';

const OBSERVATION_TIMEOUT_MS = 10_000;
type WorkerDiagnostics = {
    serviceWorker: { supported: boolean; controlled: boolean; registration: string; active: string | null };
    browserSubscription: { presence: string; expirationTime: number | null };
};
const unavailableWorker = (supported: boolean, registration: string, controlled = false): WorkerDiagnostics => ({
    serviceWorker: { supported, controlled, registration, active: null },
    browserSubscription: { presence: supported ? 'unknown' : 'unsupported', expirationTime: null },
});
function fulfilled<T>(result: PromiseSettledResult<T>): T | null {
    return result.status === 'fulfilled' ? result.value : null;
}
function readPermission(browser: Window | null): string {
    return browser === null || !Reflect.has(browser, 'Notification') ? 'unsupported' : Notification.permission;
}
async function readSubscriptionAsync(registration: ServiceWorkerRegistration | undefined): Promise<PushSubscription | null | undefined> {
    if (registration === undefined) {
        return null;
    }
    if (!Reflect.has(registration, 'pushManager')) {
        return undefined;
    }
    try {
        return await registration.pushManager.getSubscription();
    } catch {
        return undefined;
    }
}
function subscriptionMetadata(subscription: PushSubscription | null | undefined): WorkerDiagnostics['browserSubscription'] {
    return {
        presence: subscription === undefined ? 'unknown' : subscription === null ? 'absent' : 'present',
        expirationTime: subscription?.expirationTime ?? null,
    };
}
async function readWorkerAsync(browser: Window | null): Promise<WorkerDiagnostics> {
    if (browser === null || !Reflect.has(browser.navigator, 'serviceWorker')) {
        return unavailableWorker(false, 'unsupported');
    }
    const worker = browser.navigator.serviceWorker;
    try {
        const registration = await worker.getRegistration();
        const subscription = await readSubscriptionAsync(registration);
        return {
            serviceWorker: {
                supported: true,
                controlled: worker.controller !== null,
                registration: registration === undefined ? 'absent' : 'present',
                active: registration?.active?.state ?? null,
            },
            browserSubscription: subscriptionMetadata(subscription),
        };
    } catch {
        return unavailableWorker(true, 'unknown', worker.controller !== null);
    }
}

/** Read-only projection. Never requests permission, subscribes, sends or returns endpoints/keys. */
@Service()
export class LocalWebmcpPushService {
    private readonly document = inject(DOCUMENT);
    private readonly notifications = inject(NotificationService);

    public async readAsync(): Promise<unknown> {
        const browser = this.document.defaultView;
        const [worker, configuration, devices] = await Promise.allSettled([
            readWorkerAsync(browser),
            firstValueFrom(this.notifications.getWebPushConfiguration().pipe(timeout(OBSERVATION_TIMEOUT_MS))),
            firstValueFrom(this.notifications.getWebPushSubscriptions().pipe(timeout(OBSERVATION_TIMEOUT_MS))),
        ]);
        const serverDevices = fulfilled(devices);
        return {
            secureContext: browser?.isSecureContext ?? false,
            notificationPermission: readPermission(browser),
            ...(fulfilled(worker) ?? unavailableWorker(false, 'unknown')),
            providerAvailable: fulfilled(configuration)?.enabled ?? null,
            serverDevices:
                serverDevices === null
                    ? null
                    : { count: serverDevices.length, expirations: serverDevices.map(device => device.expirationTimeUtc) },
            delivery: 'not-observed',
        };
    }
}
