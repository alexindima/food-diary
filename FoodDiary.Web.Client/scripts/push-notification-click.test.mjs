import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { test } from 'node:test';
import { runInNewContext } from 'node:vm';

// Exercise the installed Angular worker, replacing only its browser bootstrap.
// Backend payload tests independently protect the operation and every target URL.
const workerSource = readFileSync(new URL('../node_modules/@angular/service-worker/ngsw-worker.js', import.meta.url), 'utf8');
const bootstrap =
    /  var scope = self;\r?\n  var adapter = new Adapter\(scope.registration.scope, self.caches\);\r?\n  new Driver\(scope, adapter, new CacheDatabase\(adapter\)\);/u;
assert.match(workerSource, bootstrap, 'Review the Angular worker contract after a bootstrap change.');
const context = { URL };
runInNewContext(workerSource.replace(bootstrap, 'globalThis.WorkerDriver = Driver;'), context);

function navigationData(url) {
    return { targetUrl: url, url, onActionClick: { default: { operation: 'navigateLastFocusedOrOpen', url } } };
}

function createWindow(name, operations) {
    const messages = [];
    const client = {
        async navigate(url) {
            operations.push(['navigate', name, url]);
            return client;
        },
        async focus() {
            operations.push(['focus', name]);
            return client;
        },
        postMessage(message) {
            messages.push(message);
        },
    };
    return { client, messages };
}

async function clickNotification(data, clients = [], operations = []) {
    const driver = Object.create(context.WorkerDriver.prototype);
    let displayed;
    driver.scope = {
        registration: {
            scope: 'https://fooddiary.example/',
            async showNotification(title, options) {
                displayed = { title, ...options };
            },
        },
        clients: {
            async matchAll() {
                return clients;
            },
            async openWindow(url) {
                operations.push(['open', url]);
            },
        },
    };
    let pushCompletion;
    driver.onPush({
        data: { json: () => ({ notification: { title: 'Notification', data } }) },
        waitUntil(completion) {
            pushCompletion = completion;
        },
    });
    assert.ok(pushCompletion instanceof Promise || typeof pushCompletion?.then === 'function');
    await pushCompletion;
    assert.equal(displayed.data, data);

    let closed = false;
    let clickCompletion;
    driver.onClick({
        notification: {
            ...displayed,
            close() {
                closed = true;
            },
        },
        action: '',
        waitUntil(completion) {
            clickCompletion = completion;
        },
    });
    assert.ok(clickCompletion instanceof Promise || typeof clickCompletion?.then === 'function');
    await clickCompletion;
    assert.equal(closed, true);
    return operations;
}

for (const [target, expected] of [
    ['/fasting?intent=eating-window', 'https://fooddiary.example/fasting?intent=eating-window'],
    ['/profile?intent=set-password', 'https://fooddiary.example/profile?intent=set-password'],
    ['https://fooddiary.example/recommendations?recommendationId=123', 'https://fooddiary.example/recommendations?recommendationId=123'],
    ['/', 'https://fooddiary.example/'],
]) {
    test(`Angular worker opens ${target} when the PWA has no open windows`, async () => {
        assert.deepEqual(await clickNotification(navigationData(target)), [['open', expected]]);
    });
}

test('Angular worker navigates and focuses the most recent window without opening a duplicate', async () => {
    const operations = [];
    const recent = createWindow('recent', operations);
    const older = createWindow('older', operations);
    const data = navigationData('/fasting?intent=eating-window');

    await clickNotification(data, [recent.client, older.client], operations);

    assert.deepEqual(operations, [
        ['navigate', 'recent', 'https://fooddiary.example/fasting?intent=eating-window'],
        ['focus', 'recent'],
    ]);
    for (const window of [recent, older]) {
        assert.equal(window.messages.length, 2);
        assert.equal(window.messages[0].type, 'PUSH');
        assert.equal(window.messages[1].type, 'NOTIFICATION_CLICK');
        assert.equal(window.messages[1].data.notification.data.targetUrl, data.targetUrl);
    }
});

test('a legacy payload without a worker click operation cannot open a closed PWA', async () => {
    assert.deepEqual(await clickNotification({ targetUrl: '/fasting?intent=eating-window' }), []);
});
