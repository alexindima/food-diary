import { DOCUMENT } from '@angular/common';
import { provideLocationMocks } from '@angular/common/testing';
import { ChangeDetectionStrategy, Component, PLATFORM_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { describe, expect, it } from 'vitest';

import { mobileShellStartGuard } from './mobile-shell-start.guard';

@Component({ template: 'Public landing', changeDetection: ChangeDetectionStrategy.OnPush })
class LandingPage {}

@Component({ template: 'Mobile sign-in', changeDetection: ChangeDetectionStrategy.OnPush })
class MobileSignInPage {}

type BrowserRuntime = {
    address: string;
    userAgent: string;
    bridge?: { isNativePlatform?: () => boolean; getPlatform?: () => string };
};

type ShellScenario = {
    title: string;
    runtime: BrowserRuntime | null;
    platform?: 'server' | 'browser';
    expected: '/' | '/mobile/login';
};

const HOSTED_BROWSER = { address: 'https://fooddiary.example.test/', userAgent: 'Chrome' };
const LEGACY_ANDROID_VIEW = { address: 'https://localhost/', userAgent: 'Mozilla/5.0 (Linux; Android 13; wv) Chrome Mobile Safari' };

const SCENARIOS: ShellScenario[] = [
    {
        title: 'redirects when the native bridge identifies an installed app',
        runtime: { ...HOSTED_BROWSER, bridge: { isNativePlatform: (): boolean => true } },
        expected: '/mobile/login',
    },
    {
        title: 'recognizes iOS when an older Capacitor bridge exposes only the platform name',
        runtime: { ...HOSTED_BROWSER, bridge: { getPlatform: (): string => 'ios' } },
        expected: '/mobile/login',
    },
    {
        title: 'redirects a legacy Android WebView on the local app origin',
        runtime: LEGACY_ANDROID_VIEW,
        expected: '/mobile/login',
    },
    {
        title: 'redirects a local Capacitor WebView with a web-only bridge ID',
        runtime: { ...LEGACY_ANDROID_VIEW, address: 'capacitor://localhost/', bridge: { getPlatform: (): string => 'web' } },
        expected: '/mobile/login',
    },
    {
        title: 'keeps an ordinary local Android browser on the landing page',
        runtime: { ...LEGACY_ANDROID_VIEW, userAgent: 'Mozilla/5.0 (Linux; Android 13) Chrome Mobile Safari' },
        expected: '/',
    },
    {
        title: 'keeps a WebView on the hosted site on the landing page',
        runtime: { ...LEGACY_ANDROID_VIEW, address: HOSTED_BROWSER.address },
        expected: '/',
    },
    {
        title: 'keeps a normal local desktop browser on the landing page',
        runtime: { ...HOSTED_BROWSER, address: LEGACY_ANDROID_VIEW.address },
        expected: '/',
    },
    {
        title: 'preserves the public entry point during server rendering',
        runtime: LEGACY_ANDROID_VIEW,
        platform: 'server',
        expected: '/',
    },
    {
        title: 'preserves the public entry point without an attached browser window',
        runtime: null,
        expected: '/',
    },
];

describe('mobile shell startup route', () => {
    it.each(SCENARIOS)('$title', async ({ runtime, platform = 'browser', expected }) => {
        TestBed.configureTestingModule({
            providers: [
                { provide: DOCUMENT, useValue: createShellDocument(runtime) },
                { provide: PLATFORM_ID, useValue: platform },
                provideLocationMocks(),
                provideRouter([
                    { path: '', pathMatch: 'full', component: LandingPage, canActivate: [mobileShellStartGuard] },
                    { path: 'mobile/login', component: MobileSignInPage },
                ]),
            ],
        });

        const router = TestBed.inject(Router);

        expect(await router.navigateByUrl('/')).toBe(true);
        expect(router.url).toBe(expected);
        expect(router.routerState.snapshot.root.firstChild?.component).toBe(expected === '/' ? LandingPage : MobileSignInPage);
    });
});

function createShellDocument(runtime: BrowserRuntime | null): Document {
    const shellDocument = document.implementation.createHTMLDocument();
    if (runtime !== null) {
        const view = Object.assign(
            new EventTarget(),
            { location: new URL(runtime.address), navigator: { userAgent: runtime.userAgent } },
            runtime.bridge === undefined ? {} : { Capacitor: runtime.bridge },
        );
        Object.defineProperty(shellDocument, 'defaultView', { value: view });
    }
    return shellDocument;
}
