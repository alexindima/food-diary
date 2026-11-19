import { DOCUMENT } from '@angular/common';
import { Component } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { beforeEach, describe, expect, it } from 'vitest';

import { stubScrollIntoView } from '../../../../src/testing/dom-testing';
import { AppComponent } from './app';

const ADMIN_ROUTE_COUNT = 21;

@Component({ template: '<p>Section content</p>' })
class AdminSectionStubComponent {}

async function renderShellAsync(fixture: ComponentFixture<AppComponent>): Promise<void> {
    await fixture.whenStable();
    fixture.detectChanges();
}

async function createShellAsync(): Promise<ComponentFixture<AppComponent>> {
    stubScrollIntoView();
    await TestBed.configureTestingModule({
        imports: [AppComponent],
        providers: [
            provideRouter([
                { path: '', component: AdminSectionStubComponent },
                { path: 'users', component: AdminSectionStubComponent },
                { path: 'users/login-activity', component: AdminSectionStubComponent },
                { path: 'mail-inbox', component: AdminSectionStubComponent },
                { path: 'outgoing-emails', component: AdminSectionStubComponent },
                { path: 'sign-in', component: AdminSectionStubComponent },
            ]),
            provideTranslateService(),
        ],
    }).compileComponents();
    const fixture = TestBed.createComponent(AppComponent);
    await fixture.whenStable();
    return fixture;
}

describe('AppComponent', () => {
    let fixture: ComponentFixture<AppComponent>;

    beforeEach(async () => {
        fixture = await createShellAsync();
    });

    it('should create', () => {
        expect(fixture.componentInstance).toBeTruthy();
    });

    it('renders the current route as the page heading', () => {
        fixture.detectChanges();

        expect((fixture.nativeElement as HTMLElement).querySelector('h1')?.textContent).toContain('ADMIN_NAV.DASHBOARD');
    });

    it('provides every admin section through the mobile route selector', () => {
        fixture.detectChanges();
        const routeSelector = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('button#admin-mobile-route');

        expect(routeSelector?.textContent).toContain('ADMIN_NAV.DASHBOARD');
        routeSelector?.click();
        fixture.detectChanges();
        const options = TestBed.inject(DOCUMENT).querySelectorAll('[role=option]');
        expect(options).toHaveLength(ADMIN_ROUTE_COUNT);
        expect(Array.from(options).some(option => option.textContent.includes('ADMIN_FEEDBACK.TITLE'))).toBe(true);
    });
});

it('keeps the Users navigation expanded on account pages and restores its previous preference after leaving', async () => {
    const fixture = await createShellAsync();
    const router = TestBed.inject(Router);
    const host = fixture.nativeElement as HTMLElement;
    fixture.detectChanges();
    const toggle = host.querySelector<HTMLButtonElement>('fd-ui-sidebar button.fd-ui-sidebar__section-header');
    if (toggle === null) {
        throw new Error('Missing Users sidebar disclosure');
    }
    expect(toggle.getAttribute('aria-expanded')).toBe('true');
    toggle.click();
    fixture.detectChanges();
    expect(toggle.getAttribute('aria-expanded')).toBe('false');
    expect(host.querySelector('fd-ui-sidebar a[href="/users/login-activity"]')).toBeNull();

    await router.navigateByUrl('/users');
    await renderShellAsync(fixture);
    expect(toggle.getAttribute('aria-expanded')).toBe('true');
    expect(host.querySelector('fd-ui-sidebar a[href="/users/login-activity"]')).not.toBeNull();
    expect(host.querySelector('.admin-shell__page-header')).toBeNull();
    expect(host.querySelector('main')?.textContent).toContain('Section content');

    await router.navigateByUrl('/');
    await renderShellAsync(fixture);
    expect(toggle.getAttribute('aria-expanded')).toBe('false');
    expect(host.querySelector('h1')?.textContent.trim()).toBe('ADMIN_NAV.DASHBOARD');
});

describe('admin shell navigation', () => {
    it('uses the actual selected mobile route and preserves the filters in the skip link', async () => {
        const fixture = await createShellAsync();
        fixture.detectChanges();
        const element = fixture.nativeElement as HTMLElement;
        const router = TestBed.inject(Router);
        const routeSelector = element.querySelector<HTMLButtonElement>('button#admin-mobile-route');
        routeSelector?.click();
        fixture.detectChanges();
        const loginOption = Array.from(document.querySelectorAll<HTMLElement>('[role="option"]')).find(option =>
            option.textContent.includes('ADMIN_NAV.LOGINS'),
        );
        expect(loginOption).toBeDefined();
        loginOption?.click();
        await renderShellAsync(fixture);
        expect(router.url).toBe('/users/login-activity');
        expect(element.querySelector('h1')?.textContent.trim()).toBe('ADMIN_NAV.LOGINS');

        await router.navigateByUrl('/users/login-activity?search=reader#old');
        await renderShellAsync(fixture);
        expect(element.querySelector('.admin-skip-nav')?.getAttribute('href')).toBe(
            '/users/login-activity?search=reader#admin-main-content',
        );
        expect(TestBed.inject(DOCUMENT).documentElement.getAttribute('data-app-ready')).toBe('true');
    });

    it('keeps the mail tabs in sync and removes admin navigation on the sign-in screen', async () => {
        const fixture = await createShellAsync();
        const router = TestBed.inject(Router);
        const element = fixture.nativeElement as HTMLElement;
        await router.navigateByUrl('/mail-inbox');
        await renderShellAsync(fixture);
        expect(element.querySelector('h1')?.textContent.trim()).toBe('ADMIN_OUTGOING.MAIL');
        expect(element.querySelector('fd-ui-tabs [aria-current="page"]')?.getAttribute('href')).toBe('/mail-inbox');

        await router.navigateByUrl('/outgoing-emails');
        await renderShellAsync(fixture);
        expect(element.querySelector('fd-ui-tabs [aria-current="page"]')?.getAttribute('href')).toBe('/outgoing-emails');

        await router.navigateByUrl('/sign-in');
        await renderShellAsync(fixture);
        expect(element.querySelector('.admin-shell--sign-in')).not.toBeNull();
        expect(element.querySelector('fd-ui-sidebar')).toBeNull();
        expect(element.querySelector('#admin-mobile-route')).toBeNull();
        expect(element.querySelector('fd-ui-tabs')).toBeNull();
        expect(element.querySelector('main')?.textContent).toContain('Section content');
    });
});
